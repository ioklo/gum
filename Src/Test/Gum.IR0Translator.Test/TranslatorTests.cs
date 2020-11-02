﻿using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using Gum.CompileTime;

using Gum.Infra;
using Gum.Misc;
using System.Linq;

using static Gum.IR0.AnalyzeErrorCode;
using S = Gum.Syntax;

namespace Gum.IR0
{
    public class TranslatorTests
    {
        class TestErrorCollector : IErrorCollector
        {
            public List<IError> Errors { get; }
            public bool HasError => Errors.Count != 0;

            bool raiseAssertionFail;

            public TestErrorCollector(bool raiseAssertionFail)
            {
                Errors = new List<IError>();
                this.raiseAssertionFail = raiseAssertionFail;
            }

            public void Add(IError error)
            {
                Errors.Add(error);
                Assert.True(!raiseAssertionFail || false);
            }
        }

        Script? Translate(S.Script syntaxScript)
        {
            var testErrorCollector = new TestErrorCollector(false);
            var translator = new Translator();

            return translator.Translate("Test", syntaxScript, Array.Empty<IModuleInfo>(), testErrorCollector);
        }

        List<IError> TranslateWithErrors(S.Script syntaxScript, bool raiseAssertionFail = false)
        {
            var testErrorCollector = new TestErrorCollector(raiseAssertionFail);
            var translator = new Translator();

            var script = translator.Translate("Test", syntaxScript, Array.Empty<IModuleInfo>(), testErrorCollector);

            return testErrorCollector.Errors;
        }

        T[] MakeArray<T>(params T[] values)
        {
            return values;
        }

        Script SimpleScript(IEnumerable<TypeDecl>? typeDecls, IEnumerable<FuncDecl>? funcDecls, IEnumerable<Stmt> topLevelStmts)
        {
            // TODO: Validator
            int i = 0;
            foreach(var funcDecl in funcDecls ?? Array.Empty<FuncDecl>())
            {
                Assert.Equal(i, funcDecl.Id.Value);
                i++;
            }

            return new Script(typeDecls ?? Array.Empty<TypeDecl>(), funcDecls ?? Array.Empty<FuncDecl>(), topLevelStmts);
        }

        void VerifyError(IEnumerable<IError> errors, AnalyzeErrorCode code, S.ISyntaxNode node)
        {
            var result = errors.OfType<AnalyzeError>()
                .Any(error => error.Code == code && error.Node == node);

            Assert.True(result, $"Errors doesn't contain (Code: {code}, Node: {node})");
        }

        S.VarDecl SimpleSVarDecl(S.TypeExp typeExp, string name, S.Exp? initExp = null)
        {
            return new S.VarDecl(typeExp, MakeArray(new S.VarDeclElement(name, initExp)));
        }

        S.VarDeclStmt SimpleSVarDeclStmt(S.TypeExp typeExp, string name, S.Exp? initExp = null)
        {
            return new S.VarDeclStmt(SimpleSVarDecl(typeExp, name, initExp));
        }

        S.Script SimpleSScript(params S.Stmt[] stmts)
        {
            return new S.Script(stmts.Select(stmt => new S.Script.StmtElement(stmt)));
        }

        S.IntLiteralExp SimpleSInt(int v) => new S.IntLiteralExp(v);

        S.StringExp SimpleSString(string s) => new S.StringExp(new S.TextStringExpElement(s));

        LocalVarDeclStmt SimpleLocalVarDeclStmt(Type typeId, string name, Exp? initExp = null)
            => new LocalVarDeclStmt(SimpleLocalVarDecl(typeId, name, initExp));

        LocalVarDecl SimpleLocalVarDecl(Type typeId, string name, Exp? initExp = null) 
            => new LocalVarDecl(MakeArray(new LocalVarDecl.Element(name, typeId, initExp)));

        IntLiteralExp SimpleInt(int v) => new IntLiteralExp(v);
        StringExp SimpleString(string v) => new StringExp(new TextStringExpElement(v));
        
        S.TypeExp VarTypeExp { get => new S.IdTypeExp("var"); }
        S.TypeExp IntTypeExp { get => new S.IdTypeExp("int"); }
        S.TypeExp VoidTypeExp { get => new S.IdTypeExp("void"); }
        S.TypeExp StringTypeExp { get => new S.IdTypeExp("string"); }


        // Trivial Cases
        [Fact]
        public void CommandStmt_TranslatesTrivially()
        {   
            var syntaxCmdStmt = new S.CommandStmt(
                new S.StringExp(
                    new S.TextStringExpElement("Hello "),
                    new S.ExpStringExpElement(new S.StringExp(new S.TextStringExpElement("World")))));

            var syntaxScript = SimpleSScript(syntaxCmdStmt);

            var script = Translate(syntaxScript);
            
            var expectedStmt = new CommandStmt(
                new StringExp(
                    new TextStringExpElement("Hello "),
                    new ExpStringExpElement(new ExpInfo(new StringExp(new TextStringExpElement("World")), Type.String))));

            var expected = new Script(Array.Empty<TypeDecl>(), Array.Empty<FuncDecl>(), new[] { expectedStmt });

            Assert.Equal(expected, script, IR0EqualityComparer.Instance);
        }
        
        [Fact]
        public void VarDeclStmt_TranslatesIntoPrivateGlobalVarDecl()
        {
            var syntaxScript = new S.Script(new S.Script.StmtElement(SimpleSVarDeclStmt(IntTypeExp, "x", SimpleSInt(1))));
            var script = Translate(syntaxScript);

            var expected = SimpleScript(null, null, MakeArray(
                new PrivateGlobalVarDeclStmt(MakeArray(new PrivateGlobalVarDeclStmt.Element("x", Type.Int, SimpleInt(1))))
            ));

            Assert.Equal(expected, script, IR0EqualityComparer.Instance);
        }

        [Fact]
        public void VarDeclStmt_TranslatesIntoLocalVarDeclInTopLevelScope()
        {
            var syntaxScript = new S.Script(new S.Script.StmtElement(
                new S.BlockStmt(
                    SimpleSVarDeclStmt(IntTypeExp, "x", SimpleSInt(1))
                )
            ));
            var script = Translate(syntaxScript);

            var expected = SimpleScript(null, null, MakeArray(
                new BlockStmt(
                    new LocalVarDeclStmt(new LocalVarDecl(MakeArray(new LocalVarDecl.Element("x", Type.Int, SimpleInt(1)))))
                )
            ));

            Assert.Equal(expected, script, IR0EqualityComparer.Instance);
        }

        [Fact]
        public void VarDeclStmt_TranslatesIntoLocalVarDeclInFuncScope()
        {
            var syntaxScript = new S.Script(
                new S.Script.FuncDeclElement(new S.FuncDecl(false, VoidTypeExp, "Func", Array.Empty<string>(), new S.FuncParamInfo(Array.Empty<S.TypeAndName>(), null),
                    new S.BlockStmt(
                        SimpleSVarDeclStmt(IntTypeExp, "x", SimpleSInt(1))
                    )
                ))
            );

            var script = Translate(syntaxScript);

            var funcDecl = new FuncDecl.Normal(new FuncDeclId(0), false, Array.Empty<string>(), Array.Empty<string>(), new BlockStmt(

                new LocalVarDeclStmt(new LocalVarDecl(MakeArray(new LocalVarDecl.Element("x", Type.Int, SimpleInt(1)))))

            ));

            var expected = SimpleScript(null, MakeArray(funcDecl), MakeArray<Stmt>());

            Assert.Equal(expected, script, IR0EqualityComparer.Instance);
        }

        [Fact]
        public void VarDeclStmt_InfersVarType()
        {
            var syntaxScript = SimpleSScript(
                new S.VarDeclStmt(new S.VarDecl(VarTypeExp, new S.VarDeclElement("x", SimpleSInt(3))))
            );

            var script = Translate(syntaxScript);

            var expected = SimpleScript(null, null, new Stmt[] {
                new PrivateGlobalVarDeclStmt(new [] { new PrivateGlobalVarDeclStmt.Element("x", Type.Int, SimpleInt(3))})
            });

            Assert.Equal(expected, script, IR0EqualityComparer.Instance);
        }

        [Fact]
        public void VarDeclStmt_ChecksLocalVarNameIsUniqueWithinScope()
        {
            S.VarDeclElement elem;

            var syntaxScript = SimpleSScript(new S.BlockStmt(
                new S.VarDeclStmt(new S.VarDecl(IntTypeExp, new S.VarDeclElement("x", null))),
                new S.VarDeclStmt(new S.VarDecl(IntTypeExp, elem = new S.VarDeclElement("x", null)))

            ));

            var errors = TranslateWithErrors(syntaxScript);
            VerifyError(errors, A0103_VarDecl_LocalVarNameShouldBeUniqueWithinScope, elem);
        }

        [Fact]
        public void VarDeclStmt_ChecksLocalVarNameIsUniqueWithinScope2()
        {
            S.VarDeclElement element;

            var syntaxScript = SimpleSScript(new S.BlockStmt(
                new S.VarDeclStmt(new S.VarDecl(IntTypeExp, new S.VarDeclElement("x", null), element = new S.VarDeclElement("x", null)))
            ));

            var errors = TranslateWithErrors(syntaxScript);
            VerifyError(errors, A0103_VarDecl_LocalVarNameShouldBeUniqueWithinScope, element);
        }

        [Fact]
        public void VarDeclStmt_ChecksGlobalVarNameIsUnique()
        {
            S.VarDeclElement elem;

            var syntaxScript = SimpleSScript(
                SimpleSVarDeclStmt(IntTypeExp, "x"),
                new S.VarDeclStmt(new S.VarDecl(IntTypeExp, elem = new S.VarDeclElement("x", null)))

            );

            var errors = TranslateWithErrors(syntaxScript);
            VerifyError(errors, A0104_VarDecl_GlobalVariableNameShouldBeUnique, elem);
        }

        [Fact]
        public void VarDeclStmt_ChecksGlobalVarNameIsUnique2()
        {
            S.VarDeclElement elem;

            var syntaxScript = SimpleSScript(
                new S.VarDeclStmt(new S.VarDecl(IntTypeExp, 
                    new S.VarDeclElement("x", null),
                    elem = new S.VarDeclElement("x", null)
                ))
            );

            var errors = TranslateWithErrors(syntaxScript);
            VerifyError(errors, A0104_VarDecl_GlobalVariableNameShouldBeUnique, elem);
        }

        [Fact]
        public void IfStmt_TranslatesTrivially()
        {
            var syntaxScript = new S.Script(new S.Script.StmtElement(

                new S.IfStmt(new S.BoolLiteralExp(false), null, S.BlankStmt.Instance, S.BlankStmt.Instance)
                
            ));

            var script = Translate(syntaxScript);

            var expected = SimpleScript(null, null, MakeArray(

                new IfStmt(new BoolLiteralExp(false), BlankStmt.Instance, BlankStmt.Instance)

            ));

            Assert.Equal(expected, script, IR0EqualityComparer.Instance);
        }

        [Fact]
        public void IfStmt_ReportsErrorWhenCondTypeIsNotBool()
        {
            S.Exp cond;

            var syntaxScript = new S.Script(new S.Script.StmtElement(

                new S.IfStmt(cond = SimpleSInt(3), null, S.BlankStmt.Instance, S.BlankStmt.Instance)

            ));

            var errors = TranslateWithErrors(syntaxScript);

            VerifyError(errors, A1004_IfStmt_ConditionShouldBeBool, cond);
        }

        [Fact]
        public void IfStmt_TranslatesIntoIfTestClassStmt()
        {
            // Prerequisite
            throw new PrerequisiteRequiredException("Class");
        }

        [Fact]
        public void IfStmt_TranslatesIntoIfTestEnumStmt()
        {
            throw new PrerequisiteRequiredException("Enum");
        }

        [Fact]
        public void ForStmt_TranslatesInitializerTrivially()
        {
            var syntaxScript = new S.Script(
                
                new S.Script.StmtElement(new S.ForStmt(
                    new S.VarDeclForStmtInitializer(SimpleSVarDecl(IntTypeExp, "x")),
                    null, null, S.BlankStmt.Instance
                )),

                new S.Script.StmtElement(SimpleSVarDeclStmt(StringTypeExp, "x")),

                new S.Script.StmtElement(new S.ForStmt(
                    new S.ExpForStmtInitializer(new S.BinaryOpExp(S.BinaryOpKind.Assign, new S.IdentifierExp("x"), SimpleSString("Hello"))),
                    null, null, S.BlankStmt.Instance
                ))
            );            

            var script = Translate(syntaxScript);

            var expected = SimpleScript(null, null, new Stmt[] { 

                new ForStmt(
                    new VarDeclForStmtInitializer(SimpleLocalVarDecl(Type.Int, "x")),
                    null, null, BlankStmt.Instance
                ),

                new PrivateGlobalVarDeclStmt(new [] { new PrivateGlobalVarDeclStmt.Element("x", Type.String, null) }),

                new ForStmt(
                    new ExpForStmtInitializer(new ExpInfo(new AssignExp(new PrivateGlobalVarExp("x"), SimpleString("Hello")), Type.String)),
                    null, null, BlankStmt.Instance
                )
            });

            Assert.Equal(expected, script, IR0EqualityComparer.Instance);
        }

        // 
        [Fact]
        public void ForStmt_ChecksVarDeclInitializerScope() 
        {
            var syntaxScript = new S.Script(

                new S.Script.StmtElement(SimpleSVarDeclStmt(StringTypeExp, "x")),

                new S.Script.StmtElement(new S.ForStmt(
                    new S.VarDeclForStmtInitializer(SimpleSVarDecl(IntTypeExp, "x")), // x의 범위는 ForStmt내부에서
                    new S.BinaryOpExp(S.BinaryOpKind.Equal, new S.IdentifierExp("x"), SimpleSInt(3)),
                    null, S.BlankStmt.Instance
                )),

                new S.Script.StmtElement(new S.ExpStmt(new S.BinaryOpExp(S.BinaryOpKind.Assign, new S.IdentifierExp("x"), SimpleSString("Hello"))))
            );

            var script = Translate(syntaxScript);

            var expected = SimpleScript(null, null, new Stmt[] {

                new PrivateGlobalVarDeclStmt(new [] { new PrivateGlobalVarDeclStmt.Element("x", Type.String, null) }),

                new ForStmt(
                    new VarDeclForStmtInitializer(SimpleLocalVarDecl(Type.Int, "x")),

                    // cond
                    new CallInternalBinaryOperatorExp(
                        InternalBinaryOperator.Equal_Int_Int_Bool,
                        new ExpInfo(new LocalVarExp("x"), Type.Int),
                        new ExpInfo(SimpleInt(3), Type.Int)
                    ),
                    null, BlankStmt.Instance
                ),

                new ExpStmt(new ExpInfo(new AssignExp(new PrivateGlobalVarExp("x"), SimpleString("Hello")), Type.String)),
            });

            Assert.Equal(expected, script, IR0EqualityComparer.Instance);
        }


        [Fact]
        public void ForStmt_ChecksConditionIsBool()
        {
            S.Exp cond;

            var syntaxScript = new S.Script(
                new S.Script.StmtElement(new S.ForStmt(
                    new S.VarDeclForStmtInitializer(SimpleSVarDecl(IntTypeExp, "x")),
                    cond = SimpleSInt(3),
                    null, S.BlankStmt.Instance
                ))
            );

            var errors = TranslateWithErrors(syntaxScript);
            VerifyError(errors, A1101_ForStmt_ConditionShouldBeBool, cond);
        }


        [Fact]
        public void ForStmt_ChecksExpInitializerIsAssignOrCall()
        {
            S.Exp exp;

            var syntaxScript = new S.Script(
                new S.Script.StmtElement(new S.ForStmt(
                    new S.ExpForStmtInitializer(exp = SimpleSInt(3)), // error
                    null, null, S.BlankStmt.Instance
                ))
            );

            var errors = TranslateWithErrors(syntaxScript);

            VerifyError(errors, A1102_ForStmt_ExpInitializerShouldBeAssignOrCall, exp);
        }

        [Fact]
        public void ForStmt_ChecksContinueExpIsAssignOrCall()
        {
            S.Exp continueExp;

            var syntaxScript = new S.Script(

                new S.Script.StmtElement(new S.ForStmt(
                    null,
                    null,
                    continueExp = SimpleSInt(3), 
                    S.BlankStmt.Instance
                ))
                
            );

            var errors = TranslateWithErrors(syntaxScript);

            VerifyError(errors, A1103_ForStmt_ContinueExpShouldBeAssignOrCall, continueExp);
        }

        [Fact]
        public void ContinueStmt_TranslatesTrivially()
        {
            var syntaxScript = SimpleSScript(
                new S.ForStmt(null, null, null, S.ContinueStmt.Instance),
                new S.ForeachStmt(IntTypeExp, "x", new S.ListExp(IntTypeExp), S.ContinueStmt.Instance)
            );

            var script = Translate(syntaxScript);

            var expected = SimpleScript(null, null, new Stmt[] {
                new ForStmt(null, null, null, ContinueStmt.Instance),
                new ForeachStmt(Type.Int, "x", new ExpInfo(new ListExp(Type.Int, Array.Empty<Exp>()), Type.List(Type.Int)), ContinueStmt.Instance)
            });

            Assert.Equal(expected, script, IR0EqualityComparer.Instance);
        }

        [Fact]
        public void ContinueStmt_ChecksUsedInLoop()
        {
            S.ContinueStmt continueStmt;
            var syntaxScript = SimpleSScript(continueStmt = S.ContinueStmt.Instance);

            var errors = TranslateWithErrors(syntaxScript);

            VerifyError(errors, A1501_ContinueStmt_ShouldUsedInLoop, continueStmt);
        }

        [Fact]
        public void BreakStmt_TranslatesTrivially()
        {
            var syntaxScript = SimpleSScript(
                new S.ForStmt(null, null, null, S.BreakStmt.Instance),
                    new S.ForeachStmt(IntTypeExp, "x", new S.ListExp(IntTypeExp), S.BreakStmt.Instance)
            );

            var script = Translate(syntaxScript);

            var expected = SimpleScript(null, null, new Stmt[] {
                new ForStmt(null, null, null, BreakStmt.Instance),
                new ForeachStmt(Type.Int, "x", new ExpInfo(new ListExp(Type.Int, Array.Empty<Exp>()), Type.List(Type.Int)), BreakStmt.Instance)
            });

            Assert.Equal(expected, script, IR0EqualityComparer.Instance);
        }

        [Fact]
        public void BreakStmt_ChecksUsedInLoop()
        {
            S.BreakStmt breakStmt;
            var syntaxScript = SimpleSScript(breakStmt = S.BreakStmt.Instance);

            var errors = TranslateWithErrors(syntaxScript);

            VerifyError(errors, A1601_BreakStmt_ShouldUsedInLoop, breakStmt);
        }
        
        [Fact]
        public void ReturnStmt_TranslatesTrivially()
        {
            var syntaxScript = SimpleSScript(new S.ReturnStmt(SimpleSInt(2)));

            var script = Translate(syntaxScript);
            var expected = SimpleScript(null, null, new Stmt[]
            {
                new ReturnStmt(SimpleInt(2))
            });

            Assert.Equal(expected, script, IR0EqualityComparer.Instance);
        }

        [Fact]
        public void ReturnStmt_TranslatesReturnStmtInSeqFuncTrivially()
        {
            var syntaxScript = new S.Script(new S.Script.FuncDeclElement(new S.FuncDecl(
                true, IntTypeExp, "Func", Array.Empty<string>(), new S.FuncParamInfo(Array.Empty<S.TypeAndName>(), null),
                new S.BlockStmt(
                    new S.ReturnStmt(null)
                )
            )));

            var script = Translate(syntaxScript);

            var seqFunc = new FuncDecl.Sequence(new FuncDeclId(0), Type.Int, false, Array.Empty<string>(), Array.Empty<string>(), new BlockStmt(new ReturnStmt(null)));

            var expected = SimpleScript(null, new[] { seqFunc }, Array.Empty<Stmt>());

            Assert.Equal(expected, script, IR0EqualityComparer.Instance);
        }

        [Fact]
        public void ReturnStmt_ChecksMatchFuncRetTypeAndRetValue()
        {
            S.Exp retValue;

            var funcDecl = new S.FuncDecl(false, IntTypeExp, "Func", Array.Empty<string>(), new S.FuncParamInfo(Array.Empty<S.TypeAndName>(), null), new S.BlockStmt(
                new S.ReturnStmt(retValue = SimpleSString("Hello"))
            ));

            var syntaxScript = new S.Script(new S.Script.FuncDeclElement(funcDecl));
            var errors = TranslateWithErrors(syntaxScript);

            VerifyError(errors, A1201_ReturnStmt_MismatchBetweenReturnValueAndFuncReturnType, retValue);
        }

        [Fact]
        public void ReturnStmt_ChecksMatchVoidTypeAndReturnNothing()
        {
            S.ReturnStmt retStmt;

            var funcDecl = new S.FuncDecl(false, IntTypeExp, "Func", Array.Empty<string>(), new S.FuncParamInfo(Array.Empty<S.TypeAndName>(), null), new S.BlockStmt(
                retStmt = new S.ReturnStmt(null)
            ));

            var syntaxScript = new S.Script(new S.Script.FuncDeclElement(funcDecl));
            var errors = TranslateWithErrors(syntaxScript);

            VerifyError(errors, A1201_ReturnStmt_MismatchBetweenReturnValueAndFuncReturnType, retStmt);
        }

        [Fact]
        public void ReturnStmt_ChecksSeqFuncShouldReturnNothing()
        {
            S.ReturnStmt retStmt;

            var funcDecl = new S.FuncDecl(true, IntTypeExp, "Func", Array.Empty<string>(), new S.FuncParamInfo(Array.Empty<S.TypeAndName>(), null), new S.BlockStmt(
                retStmt = new S.ReturnStmt(SimpleSInt(2))
            ));

            var syntaxScript = new S.Script(new S.Script.FuncDeclElement(funcDecl));
            var errors = TranslateWithErrors(syntaxScript);

            VerifyError(errors, A1202_ReturnStmt_SeqFuncShouldReturnVoid, retStmt);
        }

        [Fact]
        public void ReturnStmt_ShouldReturnIntWhenUsedInTopLevelStmt()
        {
            S.Exp exp;
            var syntaxScript = SimpleSScript(new S.ReturnStmt(exp = SimpleSString("Hello")));

            var errors = TranslateWithErrors(syntaxScript);
            VerifyError(errors, A1201_ReturnStmt_MismatchBetweenReturnValueAndFuncReturnType, exp);
        }

        [Fact]
        public void ReturnStmt_UsesHintType()
        {
            throw new PrerequisiteRequiredException("Enum, HintType");
        }

        [Fact]
        public void BlockStmt_TranslatesVarDeclStmtWithinBlockStmtOfTopLevelStmtIntoLocalVarDeclStmt()
        {
            var syntaxScript = SimpleSScript(
                new S.BlockStmt(
                    SimpleSVarDeclStmt(StringTypeExp, "x", SimpleSString("Hello"))
                )
            );

            var script = Translate(syntaxScript);

            var expected = SimpleScript(null, null, MakeArray(
                new BlockStmt(
                    SimpleLocalVarDeclStmt(Type.String, "x", SimpleString("Hello")) // not PrivateGlobalVarDecl
                )
            ));

            Assert.Equal(expected, script, IR0EqualityComparer.Instance);
        }

        [Fact]
        public void BlockStmt_ChecksIsolatingOverridenTypesOfVariables()
        {
            throw new PrerequisiteRequiredException("IfTestClassStmt, IfTestEnumStmt");
        }

        [Fact]
        public void BlockStmt_ChecksLocalVariableScope()
        {
            S.Exp exp;

            var syntaxScript = SimpleSScript(
                new S.BlockStmt(
                    SimpleSVarDeclStmt(StringTypeExp, "x", SimpleSString("Hello"))
                ),

                new S.CommandStmt(new S.StringExp(new S.ExpStringExpElement(exp = new S.IdentifierExp("x"))))
            );

            var errors = TranslateWithErrors(syntaxScript);

            VerifyError(errors, A0501_IdExp_VariableNotFound, exp);
        }   
        
        [Fact]
        public void ExpStmt_TranslatesTrivially()
        {
            var syntaxScript = SimpleSScript(
                SimpleSVarDeclStmt(IntTypeExp, "x"),
                new S.ExpStmt(new S.BinaryOpExp(S.BinaryOpKind.Assign, new S.IdentifierExp("x"), SimpleSInt(3)))
            );

            var script = Translate(syntaxScript);

            var expected = SimpleScript(null, null, new Stmt[] {                
                new PrivateGlobalVarDeclStmt(MakeArray(new PrivateGlobalVarDeclStmt.Element("x", Type.Int, null))),
                new ExpStmt(new ExpInfo(new AssignExp(new PrivateGlobalVarExp("x"), SimpleInt(3)), Type.Int))
            });

            Assert.Equal(expected, script, IR0EqualityComparer.Instance);
        }

        [Fact]
        public void ExpStmt_ChecksExpIsAssignOrCall()
        {
            S.Exp exp;
            var syntaxScript = SimpleSScript(
                new S.ExpStmt(exp = SimpleSInt(3))
            );

            var errors = TranslateWithErrors(syntaxScript);

            VerifyError(errors, A1301_ExpStmt_ExpressionShouldBeAssignOrCall, exp);
        }

        [Fact]
        public void TaskStmt_TranslatesWithGlobalVariable()
        {
            var syntaxScript = SimpleSScript(
                SimpleSVarDeclStmt(IntTypeExp, "x"),
                new S.TaskStmt(
                    new S.ExpStmt(
                        new S.BinaryOpExp(S.BinaryOpKind.Assign, new S.IdentifierExp("x"), SimpleSInt(3))
                    )
                )
            );

            var script = Translate(syntaxScript);

            var expected = SimpleScript(null, null, new Stmt[] {
                new PrivateGlobalVarDeclStmt(MakeArray(new PrivateGlobalVarDeclStmt.Element("x", Type.Int, null))),
                new TaskStmt(
                    new ExpStmt(new ExpInfo(new AssignExp(new PrivateGlobalVarExp("x"), SimpleInt(3)), Type.Int)),
                    new CaptureInfo(false, Array.Empty<CaptureInfo.Element>())
                )
            });

            Assert.Equal(expected, script, IR0EqualityComparer.Instance);
        }

        [Fact]
        public void TaskStmt_ChecksAssignToLocalVariableOutsideLambda()
        {
            S.Exp exp;

            var syntaxScript = SimpleSScript(
                new S.BlockStmt(
                    SimpleSVarDeclStmt(IntTypeExp, "x"),
                    new S.TaskStmt(
                        new S.ExpStmt(
                            new S.BinaryOpExp(S.BinaryOpKind.Assign, exp = new S.IdentifierExp("x"), SimpleSInt(3))
                        )
                    )
                )
            );

            var errors = TranslateWithErrors(syntaxScript);
            VerifyError(errors, A0803_BinaryOp_LeftOperandIsNotAssignable, exp);
        }

        [Fact]
        public void TaskStmt_TranslatesWithLocalVariable()
        {
            var syntaxScript = SimpleSScript(
                new S.BlockStmt(
                    SimpleSVarDeclStmt(IntTypeExp, "x"),
                    new S.TaskStmt(
                        SimpleSVarDeclStmt(IntTypeExp, "x", new S.IdentifierExp("x"))
                    )
                )
            );

            var script = Translate(syntaxScript);

            var expected = SimpleScript(null, null, new Stmt[] {
                new BlockStmt(
                    SimpleLocalVarDeclStmt(Type.Int, "x"),
                    new TaskStmt(
                        SimpleLocalVarDeclStmt(Type.Int, "x", new LocalVarExp("x")),
                        new CaptureInfo(false, new [] { new CaptureInfo.Element(Type.Int, "x") })
                    )
                )
            });

            Assert.Equal(expected, script, IR0EqualityComparer.Instance);
        }

        [Fact]
        public void AwaitStmt_TranslatesTrivially()
        {
            var syntaxScript = SimpleSScript(
                new S.AwaitStmt(
                    S.BlankStmt.Instance
                )
            );

            var script = Translate(syntaxScript);

            var expected = SimpleScript(null, null, new Stmt[] { new AwaitStmt(BlankStmt.Instance) });

            Assert.Equal(expected, script, IR0EqualityComparer.Instance);
        }

        [Fact]
        public void AwaitStmt_ChecksLocalVariableScope()
        {
            S.Exp exp;

            var syntaxScript = SimpleSScript(
                new S.AwaitStmt(
                    SimpleSVarDeclStmt(StringTypeExp, "x", SimpleSString("Hello"))
                ),

                new S.CommandStmt(new S.StringExp(new S.ExpStringExpElement(exp = new S.IdentifierExp("x"))))
            );

            var errors = TranslateWithErrors(syntaxScript);

            VerifyError(errors, A0501_IdExp_VariableNotFound, exp);
        }

        [Fact]
        public void AsyncStmt_TranslatesWithGlobalVariable()
        {
            var syntaxScript = SimpleSScript(
                SimpleSVarDeclStmt(IntTypeExp, "x"),
                new S.AsyncStmt(
                    new S.ExpStmt(
                        new S.BinaryOpExp(S.BinaryOpKind.Assign, new S.IdentifierExp("x"), SimpleSInt(3))
                    )
                )
            );

            var script = Translate(syntaxScript);

            var expected = SimpleScript(null, null, new Stmt[] {
                new PrivateGlobalVarDeclStmt(MakeArray(new PrivateGlobalVarDeclStmt.Element("x", Type.Int, null))),
                new AsyncStmt(
                    new ExpStmt(new ExpInfo(new AssignExp(new PrivateGlobalVarExp("x"), SimpleInt(3)), Type.Int)),
                    new CaptureInfo(false, Array.Empty<CaptureInfo.Element>())
                )
            });

            Assert.Equal(expected, script, IR0EqualityComparer.Instance);
        }

        [Fact]
        public void AsyncStmt_ChecksAssignToLocalVariableOutsideLambda()
        {
            S.Exp exp;

            var syntaxScript = SimpleSScript(
                new S.BlockStmt(
                    SimpleSVarDeclStmt(IntTypeExp, "x"),
                    new S.AsyncStmt(
                        new S.ExpStmt(
                            new S.BinaryOpExp(S.BinaryOpKind.Assign, exp = new S.IdentifierExp("x"), SimpleSInt(3))
                        )
                    )
                )
            );

            var errors = TranslateWithErrors(syntaxScript);
            VerifyError(errors, A0803_BinaryOp_LeftOperandIsNotAssignable, exp);
        }

        [Fact]
        public void AsyncStmt_TranslatesWithLocalVariable()
        {
            var syntaxScript = SimpleSScript(
                new S.BlockStmt(
                    SimpleSVarDeclStmt(IntTypeExp, "x"),
                    new S.AsyncStmt(
                        SimpleSVarDeclStmt(IntTypeExp, "x", new S.IdentifierExp("x"))
                    )
                )
            );

            var script = Translate(syntaxScript);

            var expected = SimpleScript(null, null, new Stmt[] {
                new BlockStmt(
                    SimpleLocalVarDeclStmt(Type.Int, "x"),
                    new AsyncStmt(
                        SimpleLocalVarDeclStmt(Type.Int, "x", new LocalVarExp("x")),
                        new CaptureInfo(false, new [] { new CaptureInfo.Element(Type.Int, "x") })
                    )
                )
            });

            Assert.Equal(expected, script, IR0EqualityComparer.Instance);
        }

        [Fact]
        public void ForeachStmt_TranslateTrivially()
        {
            throw new PrerequisiteRequiredException("Implementation");
        }

        [Fact]
        public void YieldStmt_TranslateTrivially()
        {
            var syntaxScript = new S.Script(
                new S.Script.FuncDeclElement(new S.FuncDecl(
                    true, IntTypeExp, "Func", Array.Empty<string>(), new S.FuncParamInfo(Array.Empty<S.TypeAndName>(), null),
                    new S.BlockStmt(
                        new S.YieldStmt(SimpleSInt(3))
                    )
                ))
            );

            var script = Translate(syntaxScript);

            var seqFunc = new FuncDecl.Sequence(new FuncDeclId(0), Type.Int, false, Array.Empty<string>(), Array.Empty<string>(), new BlockStmt(
                new YieldStmt(SimpleInt(3))
            ));

            var expected = SimpleScript(null, new[] { seqFunc }, Array.Empty<Stmt>());

            Assert.Equal(expected, script, IR0EqualityComparer.Instance);
        }

        [Fact]
        public void YieldStmt_ChecksYieldStmtUsedInSeqFunc()
        {
            S.YieldStmt yieldStmt;

            var syntaxScript = new S.Script(
                new S.Script.FuncDeclElement(new S.FuncDecl(
                    false, IntTypeExp, "Func", Array.Empty<string>(), new S.FuncParamInfo(Array.Empty<S.TypeAndName>(), null),
                    new S.BlockStmt(
                        yieldStmt = new S.YieldStmt(SimpleSInt(3))
                    )
                ))
            );

            var errors = TranslateWithErrors(syntaxScript);

            VerifyError(errors, A1401_YieldStmt_YieldShouldBeInSeqFunc, yieldStmt);
        }

        [Fact]
        public void YieldStmt_ChecksMatchingYieldType()
        {
            S.Exp yieldValue;

            var syntaxScript = new S.Script(
                new S.Script.FuncDeclElement(new S.FuncDecl(
                    true, StringTypeExp, "Func", Array.Empty<string>(), new S.FuncParamInfo(Array.Empty<S.TypeAndName>(), null),
                    new S.BlockStmt(
                        new S.YieldStmt(yieldValue = SimpleSInt(3))
                    )
                ))
            );

            var errors = TranslateWithErrors(syntaxScript);

            VerifyError(errors, A1402_YieldStmt_MismatchBetweenYieldValueAndSeqFuncYieldType, yieldValue);
        }

        [Fact]
        public void YieldStmt_UsesHintTypeValue()
        {
            throw new PrerequisiteRequiredException("Enum HintType");
        }

        // StringExp
        [Fact]
        public void StringExp_ChecksStringExpElementIsNotStringType()
        {   
            var syntaxCmdStmt = new S.ExpStmt(
                new S.StringExp(
                    new S.ExpStringExpElement(SimpleSInt(3))));

            var syntaxScript = new S.Script(new S.Script.StmtElement(syntaxCmdStmt));

            var errors = TranslateWithErrors(syntaxScript);

            // Assert.True(errors.Exists(error => error.Code == A1004_IfStmt_ConditionShouldBeBool && error.Node == (S.ISyntaxNode)cond));

            throw new PrerequisiteRequiredException("AnalyzeStringExpElement");
        }

    }
}