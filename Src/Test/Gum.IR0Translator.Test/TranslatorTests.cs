﻿using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

using Gum.Infra;
using System.Linq;

using S = Gum.Syntax;
using M = Gum.CompileTime;
using R = Gum.IR0;

using static Gum.IR0Translator.AnalyzeErrorCode;
using static Gum.Infra.Misc;
using static Gum.IR0Translator.Test.TestMisc;
using static Gum.IR0Translator.Test.SyntaxFactory;
using static Gum.IR0Translator.Test.IR0Factory;

namespace Gum.IR0Translator.Test
{
    public class TranslatorTests
    {
        M.ModuleName ModuleName = "TestModule";

        R.Script? Translate(S.Script syntaxScript, bool raiseAssertFailed = true)
        {
            var testErrorCollector = new TestErrorCollector(raiseAssertFailed);
            return Translator.Translate(ModuleName, default, syntaxScript, testErrorCollector);
        }

        List<IError> TranslateWithErrors(S.Script syntaxScript, bool raiseAssertionFail = false)
        {
            var testErrorCollector = new TestErrorCollector(raiseAssertionFail);
            var script = Translator.Translate(ModuleName, default, syntaxScript, testErrorCollector);

            return testErrorCollector.Errors;
        }       

        // Trivial Cases
        [Fact]
        public void CommandStmt_TranslatesTrivially()
        {   
            var syntaxCmdStmt = SCommand(
                SString(
                    new S.TextStringExpElement("Hello "),
                    new S.ExpStringExpElement(SString("World"))));

            var syntaxScript = SScript(syntaxCmdStmt);

            var script = Translate(syntaxScript);
            
            var expectedStmt = RCommand(
                RString(
                    new R.TextStringExpElement("Hello "),
                    new R.ExpStringExpElement(RString("World"))));

            var expected = new R.Script(default, default, Arr<R.Stmt>(expectedStmt));

            Assert.Equal(expected, script, IR0EqualityComparer.Instance);
        }
        
        [Fact]
        public void VarDeclStmt_TranslatesIntoPrivateGlobalVarDecl()
        {
            var syntaxScript = SScript(new S.StmtScriptElement(SVarDeclStmt(IntTypeExp, "x", SInt(1))));
            var script = Translate(syntaxScript);

            var expected = RScript(
                RGlobalVarDeclStmt(R.Type.Int, "x", RInt(1))
            );

            Assert.Equal(expected, script, IR0EqualityComparer.Instance);
        }

        [Fact]
        public void VarDeclStmt_TranslatesIntoLocalVarDeclInTopLevelScope()
        {
            var syntaxScript = SScript(new S.StmtScriptElement(
                SBlock(
                    SVarDeclStmt(IntTypeExp, "x", SInt(1))
                )
            ));
            var script = Translate(syntaxScript);

            var expected = RScript(
                RBlock(
                    new R.LocalVarDeclStmt(new R.LocalVarDecl(Arr(new R.VarDeclElement("x", R.Type.Int, RInt(1)))))
                )
            );

            Assert.Equal(expected, script, IR0EqualityComparer.Instance);
        }

        [Fact]
        public void VarDeclStmt_TranslatesIntoLocalVarDeclInFuncScope()
        {
            var syntaxScript = SScript(
                new S.GlobalFuncDeclScriptElement(new S.GlobalFuncDecl(false, VoidTypeExp, "Func", default, new S.FuncParamInfo(default, null),
                    SBlock(
                        SVarDeclStmt(IntTypeExp, "x", SInt(1))
                    )
                ))
            );

            var script = Translate(syntaxScript);

            var funcDecl = new R.NormalFuncDecl(new R.FuncDeclId(0), false, default, default, RBlock(

                new R.LocalVarDeclStmt(new R.LocalVarDecl(Arr(new R.VarDeclElement("x", R.Type.Int, RInt(1)))))

            ));

            var expected = RScript(null, Arr(funcDecl));

            Assert.Equal(expected, script, IR0EqualityComparer.Instance);
        }

        [Fact]
        public void VarDeclStmt_InfersVarType()
        {
            var syntaxScript = SScript(
                new S.VarDeclStmt(new S.VarDecl(VarTypeExp, Arr(new S.VarDeclElement("x", SInt(3)))))
            );

            var script = Translate(syntaxScript);

            var expected = RScript(
                RGlobalVarDeclStmt(R.Type.Int, "x", RInt(3))            
            );

            Assert.Equal(expected, script, IR0EqualityComparer.Instance);
        }

        [Fact]
        public void VarDeclStmt_ChecksLocalVarNameIsUniqueWithinScope()
        {
            S.VarDeclElement elem;

            var syntaxScript = SScript(SBlock(
                new S.VarDeclStmt(SVarDecl(IntTypeExp, "x", null)),
                new S.VarDeclStmt(new S.VarDecl(IntTypeExp, Arr(elem = new S.VarDeclElement("x", null))))
            ));

            var errors = TranslateWithErrors(syntaxScript);
            VerifyError(errors, A0103_VarDecl_LocalVarNameShouldBeUniqueWithinScope, elem);
        }

        [Fact]
        public void VarDeclStmt_ChecksLocalVarNameIsUniqueWithinScope2()
        {
            S.VarDeclElement element;

            var syntaxScript = SScript(SBlock(
                new S.VarDeclStmt(new S.VarDecl(IntTypeExp, Arr(new S.VarDeclElement("x", null), element = new S.VarDeclElement("x", null))))
            ));

            var errors = TranslateWithErrors(syntaxScript);
            VerifyError(errors, A0103_VarDecl_LocalVarNameShouldBeUniqueWithinScope, element);
        }

        [Fact]
        public void VarDeclStmt_ChecksGlobalVarNameIsUnique()
        {
            S.VarDeclElement elem;

            var syntaxScript = SScript(
                SVarDeclStmt(IntTypeExp, "x"),
                new S.VarDeclStmt(new S.VarDecl(IntTypeExp, Arr(elem = new S.VarDeclElement("x", null))))

            );

            var errors = TranslateWithErrors(syntaxScript);
            VerifyError(errors, A0104_VarDecl_GlobalVariableNameShouldBeUnique, elem);
        }

        [Fact]
        public void VarDeclStmt_ChecksGlobalVarNameIsUnique2()
        {
            S.VarDeclElement elem;

            var syntaxScript = SScript(
                new S.VarDeclStmt(new S.VarDecl(IntTypeExp, Arr(
                    new S.VarDeclElement("x", null),
                    elem = new S.VarDeclElement("x", null)
                )))
            );

            var errors = TranslateWithErrors(syntaxScript);
            VerifyError(errors, A0104_VarDecl_GlobalVariableNameShouldBeUnique, elem);
        }

        [Fact]
        public void IfStmt_TranslatesTrivially()
        {
            var syntaxScript = SScript(new S.StmtScriptElement(

                new S.IfStmt(new S.BoolLiteralExp(false), null, S.BlankStmt.Instance, S.BlankStmt.Instance)
                
            ));

            var script = Translate(syntaxScript);

            var expected = RScript(

                new R.IfStmt(new R.BoolLiteralExp(false), R.BlankStmt.Instance, R.BlankStmt.Instance)

            );

            Assert.Equal(expected, script, IR0EqualityComparer.Instance);
        }

        [Fact]
        public void IfStmt_ReportsErrorWhenCondTypeIsNotBool()
        {
            S.Exp cond;

            var syntaxScript = SScript(new S.StmtScriptElement(

                new S.IfStmt(cond = SInt(3), null, S.BlankStmt.Instance, S.BlankStmt.Instance)

            ));

            var errors = TranslateWithErrors(syntaxScript);

            VerifyError(errors, A1004_IfStmt_ConditionShouldBeBool, cond);
        }

        [Fact]
        public void IfStmt_TranslatesIntoIfTestClassStmt()
        {
            // Prerequisite
            throw new PrerequisiteRequiredException(Prerequisite.Class);
        }

        [Fact]
        public void IfStmt_TranslatesIntoIfTestEnumStmt()
        {
            throw new PrerequisiteRequiredException(Prerequisite.Enum);
        }

        [Fact]
        public void ForStmt_TranslatesInitializerTrivially()
        {
            var syntaxScript = SScript(
                
                new S.StmtScriptElement(new S.ForStmt(
                    new S.VarDeclForStmtInitializer(SVarDecl(IntTypeExp, "x")),
                    null, null, S.BlankStmt.Instance
                )),

                new S.StmtScriptElement(SVarDeclStmt(StringTypeExp, "x")),

                new S.StmtScriptElement(new S.ForStmt(
                    new S.ExpForStmtInitializer(new S.BinaryOpExp(S.BinaryOpKind.Assign, SId("x"), SString("Hello"))),
                    null, null, S.BlankStmt.Instance
                ))
            );            

            var script = Translate(syntaxScript);

            var expected = RScript(

                new R.ForStmt(
                    new R.VarDeclForStmtInitializer(RLocalVarDecl(R.Type.Int, "x")),
                    null, null, R.BlankStmt.Instance
                ),

                RGlobalVarDeclStmt(R.Type.String, "x", null),

                new R.ForStmt(
                    new R.ExpForStmtInitializer(new R.ExpInfo(new R.AssignExp(new R.GlobalVarExp("x"), RString("Hello")), R.Type.String)),
                    null, null, R.BlankStmt.Instance
                )
            );

            Assert.Equal(expected, script, IR0EqualityComparer.Instance);
        }

        // 
        [Fact]
        public void ForStmt_ChecksVarDeclInitializerScope() 
        {
            var syntaxScript = SScript(

                new S.StmtScriptElement(SVarDeclStmt(StringTypeExp, "x")),

                new S.StmtScriptElement(new S.ForStmt(
                    new S.VarDeclForStmtInitializer(SVarDecl(IntTypeExp, "x")), // x의 범위는 ForStmt내부에서
                    new S.BinaryOpExp(S.BinaryOpKind.Equal, SId("x"), SInt(3)),
                    null, S.BlankStmt.Instance
                )),

                new S.StmtScriptElement(new S.ExpStmt(new S.BinaryOpExp(S.BinaryOpKind.Assign, SId("x"), SString("Hello"))))
            );

            var script = Translate(syntaxScript);

            var expected = RScript(

                RGlobalVarDeclStmt(R.Type.String, "x", null),

                new R.ForStmt(
                    new R.VarDeclForStmtInitializer(RLocalVarDecl(R.Type.Int, "x")),

                    // cond
                    new R.CallInternalBinaryOperatorExp(
                        R.InternalBinaryOperator.Equal_Int_Int_Bool,
                        new R.ExpInfo(new R.LocalVarExp("x"), R.Type.Int),
                        new R.ExpInfo(RInt(3), R.Type.Int)
                    ),
                    null, R.BlankStmt.Instance
                ),

                new R.ExpStmt(new R.ExpInfo(new R.AssignExp(new R.GlobalVarExp("x"), RString("Hello")), R.Type.String))
            );

            Assert.Equal(expected, script, IR0EqualityComparer.Instance);
        }


        [Fact]
        public void ForStmt_ChecksConditionIsBool()
        {
            S.Exp cond;

            var syntaxScript = SScript(
                new S.StmtScriptElement(new S.ForStmt(
                    new S.VarDeclForStmtInitializer(SVarDecl(IntTypeExp, "x")),
                    cond = SInt(3),
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

            var syntaxScript = SScript(
                new S.StmtScriptElement(new S.ForStmt(
                    new S.ExpForStmtInitializer(exp = SInt(3)), // error
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

            var syntaxScript = SScript(

                new S.StmtScriptElement(new S.ForStmt(
                    null,
                    null,
                    continueExp = SInt(3), 
                    S.BlankStmt.Instance
                ))
                
            );

            var errors = TranslateWithErrors(syntaxScript);

            VerifyError(errors, A1103_ForStmt_ContinueExpShouldBeAssignOrCall, continueExp);
        }

        [Fact]
        public void ContinueStmt_TranslatesTrivially()
        {
            var syntaxScript = SScript(
                new S.ForStmt(null, null, null, S.ContinueStmt.Instance),
                new S.ForeachStmt(IntTypeExp, "x", new S.ListExp(IntTypeExp, default), S.ContinueStmt.Instance)
            );

            var script = Translate(syntaxScript);

            var expected = RScript(
                new R.ForStmt(null, null, null, R.ContinueStmt.Instance),
                new R.ForeachStmt(R.Type.Int, "x", new R.ExpInfo(new R.ListExp(R.Type.Int, default), R.Type.List(R.Type.Int)), R.ContinueStmt.Instance)
            );

            Assert.Equal(expected, script, IR0EqualityComparer.Instance);
        }

        [Fact]
        public void ContinueStmt_ChecksUsedInLoop()
        {
            S.ContinueStmt continueStmt;
            var syntaxScript = SScript(continueStmt = S.ContinueStmt.Instance);

            var errors = TranslateWithErrors(syntaxScript);

            VerifyError(errors, A1501_ContinueStmt_ShouldUsedInLoop, continueStmt);
        }

        [Fact]
        public void BreakStmt_TranslatesTrivially()
        {
            var syntaxScript = SScript(
                new S.ForStmt(null, null, null, S.BreakStmt.Instance),
                    new S.ForeachStmt(IntTypeExp, "x", new S.ListExp(IntTypeExp, default), S.BreakStmt.Instance)
            );

            var script = Translate(syntaxScript);

            var expected = RScript(
                new R.ForStmt(null, null, null, R.BreakStmt.Instance),
                new R.ForeachStmt(R.Type.Int, "x", new R.ExpInfo(new R.ListExp(R.Type.Int, default), R.Type.List(R.Type.Int)), R.BreakStmt.Instance)
            );

            Assert.Equal(expected, script, IR0EqualityComparer.Instance);
        }

        [Fact]
        public void BreakStmt_ChecksUsedInLoop()
        {
            S.BreakStmt breakStmt;
            var syntaxScript = SScript(breakStmt = S.BreakStmt.Instance);

            var errors = TranslateWithErrors(syntaxScript);

            VerifyError(errors, A1601_BreakStmt_ShouldUsedInLoop, breakStmt);
        }
        
        [Fact]
        public void ReturnStmt_TranslatesTrivially()
        {
            var syntaxScript = SScript(new S.ReturnStmt(SInt(2)));

            var script = Translate(syntaxScript);
            var expected = RScript(new R.ReturnStmt(RInt(2)));

            Assert.Equal(expected, script, IR0EqualityComparer.Instance);
        }

        [Fact]
        public void ReturnStmt_TranslatesReturnStmtInSeqFuncTrivially()
        {
            var syntaxScript = SScript(new S.GlobalFuncDeclScriptElement(new S.GlobalFuncDecl(
                true, IntTypeExp, "Func", default, new S.FuncParamInfo(default, null),
                SBlock(
                    new S.ReturnStmt(null)
                )
            )));

            var script = Translate(syntaxScript);

            var seqFunc = new R.SequenceFuncDecl(new R.FuncDeclId(0), R.Type.Int, false, default, default, RBlock(new R.ReturnStmt(null)));

            var expected = RScript(null, new[] { seqFunc });

            Assert.Equal(expected, script, IR0EqualityComparer.Instance);
        }

        [Fact]
        public void ReturnStmt_ChecksMatchFuncRetTypeAndRetValue()
        {
            S.Exp retValue;

            var funcDecl = new S.GlobalFuncDecl(false, IntTypeExp, "Func", default, new S.FuncParamInfo(default, null), SBlock(
                new S.ReturnStmt(retValue = SString("Hello"))
            ));

            var syntaxScript = SScript(new S.GlobalFuncDeclScriptElement(funcDecl));
            var errors = TranslateWithErrors(syntaxScript);

            VerifyError(errors, A1201_ReturnStmt_MismatchBetweenReturnValueAndFuncReturnType, retValue);
        }

        [Fact]
        public void ReturnStmt_ChecksMatchVoidTypeAndReturnNothing()
        {
            S.ReturnStmt retStmt;

            var funcDecl = new S.GlobalFuncDecl(false, IntTypeExp, "Func", default, new S.FuncParamInfo(default, null), SBlock(
                retStmt = new S.ReturnStmt(null)
            ));

            var syntaxScript = SScript(new S.GlobalFuncDeclScriptElement(funcDecl));
            var errors = TranslateWithErrors(syntaxScript);

            VerifyError(errors, A1201_ReturnStmt_MismatchBetweenReturnValueAndFuncReturnType, retStmt);
        }

        [Fact]
        public void ReturnStmt_ChecksSeqFuncShouldReturnNothing()
        {
            S.ReturnStmt retStmt;

            var funcDecl = new S.GlobalFuncDecl(true, IntTypeExp, "Func", default, new S.FuncParamInfo(default, null), SBlock(
                retStmt = new S.ReturnStmt(SInt(2))
            ));

            var syntaxScript = SScript(new S.GlobalFuncDeclScriptElement(funcDecl));
            var errors = TranslateWithErrors(syntaxScript);

            VerifyError(errors, A1202_ReturnStmt_SeqFuncShouldReturnVoid, retStmt);
        }

        [Fact]
        public void ReturnStmt_ShouldReturnIntWhenUsedInTopLevelStmt()
        {
            S.Exp exp;
            var syntaxScript = SScript(new S.ReturnStmt(exp = SString("Hello")));

            var errors = TranslateWithErrors(syntaxScript);
            VerifyError(errors, A1201_ReturnStmt_MismatchBetweenReturnValueAndFuncReturnType, exp);
        }

        [Fact]
        public void ReturnStmt_UsesHintType()
        {
            throw new PrerequisiteRequiredException(Prerequisite.Enum, Prerequisite.TypeHint);
        }

        [Fact]
        public void BlockStmt_TranslatesVarDeclStmtWithinBlockStmtOfTopLevelStmtIntoLocalVarDeclStmt()
        {
            var syntaxScript = SScript(
                SBlock(
                    SVarDeclStmt(StringTypeExp, "x", SString("Hello"))
                )
            );

            var script = Translate(syntaxScript);

            var expected = RScript(
                RBlock(
                    RLocalVarDeclStmt(R.Type.String, "x", RString("Hello")) // not PrivateGlobalVarDecl
                )
            );

            Assert.Equal(expected, script, IR0EqualityComparer.Instance);
        }

        [Fact]
        public void BlockStmt_ChecksIsolatingOverridenTypesOfVariables()
        {
            throw new PrerequisiteRequiredException(Prerequisite.IfTestClassStmt, Prerequisite.IfTestEnumStmt);
        }

        [Fact]
        public void BlockStmt_ChecksLocalVariableScope()
        {
            S.Exp exp;

            var syntaxScript = SScript(
                SBlock(
                    SVarDeclStmt(StringTypeExp, "x", SString("Hello"))
                ),

                SCommand(SString(new S.ExpStringExpElement(exp = SId("x"))))
            );

            var errors = TranslateWithErrors(syntaxScript);

            VerifyError(errors, A2007_ResolveIdentifier_NotFound, exp);
        }   
        
        [Fact]
        public void ExpStmt_TranslatesTrivially()
        {
            var syntaxScript = SScript(
                SVarDeclStmt(IntTypeExp, "x"),
                new S.ExpStmt(new S.BinaryOpExp(S.BinaryOpKind.Assign, SId("x"), SInt(3)))
            );

            var script = Translate(syntaxScript);

            var expected = RScript(
                RGlobalVarDeclStmt(R.Type.Int, "x", null),
                new R.ExpStmt(new R.ExpInfo(new R.AssignExp(new R.GlobalVarExp("x"), RInt(3)), R.Type.Int))
            );

            Assert.Equal(expected, script, IR0EqualityComparer.Instance);
        }

        [Fact]
        public void ExpStmt_ChecksExpIsAssignOrCall()
        {
            S.Exp exp;
            var syntaxScript = SScript(
                new S.ExpStmt(exp = SInt(3))
            );

            var errors = TranslateWithErrors(syntaxScript);

            VerifyError(errors, A1301_ExpStmt_ExpressionShouldBeAssignOrCall, exp);
        }

        [Fact]
        public void TaskStmt_TranslatesWithGlobalVariable()
        {
            var syntaxScript = SScript(
                SVarDeclStmt(IntTypeExp, "x"),
                new S.TaskStmt(
                    new S.ExpStmt(
                        new S.BinaryOpExp(S.BinaryOpKind.Assign, SId("x"), SInt(3))
                    )
                )
            );

            var script = Translate(syntaxScript);

            var expected = RScript(
                RGlobalVarDeclStmt(R.Type.Int, "x", null),
                new R.TaskStmt(
                    new R.ExpStmt(new R.ExpInfo(new R.AssignExp(new R.GlobalVarExp("x"), RInt(3)), R.Type.Int)),
                    new R.CaptureInfo(false, default)
                )
            );

            Assert.Equal(expected, script, IR0EqualityComparer.Instance);
        }

        [Fact]
        public void TaskStmt_ChecksAssignToLocalVariableOutsideLambda()
        {
            S.Exp exp;

            var syntaxScript = SScript(
                SBlock(
                    SVarDeclStmt(IntTypeExp, "x"),
                    new S.TaskStmt(
                        new S.ExpStmt(
                            new S.BinaryOpExp(S.BinaryOpKind.Assign, exp = SId("x"), SInt(3))
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
            var syntaxScript = SScript(
                SBlock(
                    SVarDeclStmt(IntTypeExp, "x"),
                    new S.TaskStmt(
                        SVarDeclStmt(IntTypeExp, "x", SId("x"))
                    )
                )
            );

            var script = Translate(syntaxScript);

            var expected = RScript(
                RBlock(
                    RLocalVarDeclStmt(R.Type.Int, "x"),
                    new R.TaskStmt(
                        RLocalVarDeclStmt(R.Type.Int, "x", new R.LocalVarExp("x")),
                        new R.CaptureInfo(false, Arr(new R.CaptureInfo.Element(R.Type.Int, "x")))
                    )
                )
            );

            Assert.Equal(expected, script, IR0EqualityComparer.Instance);
        }

        [Fact]
        public void AwaitStmt_TranslatesTrivially()
        {
            var syntaxScript = SScript(
                new S.AwaitStmt(
                    S.BlankStmt.Instance
                )
            );

            var script = Translate(syntaxScript);

            var expected = RScript(new R.AwaitStmt(R.BlankStmt.Instance));

            Assert.Equal(expected, script, IR0EqualityComparer.Instance);
        }

        [Fact]
        public void AwaitStmt_ChecksLocalVariableScope()
        {
            S.Exp exp;

            var syntaxScript = SScript(
                new S.AwaitStmt(
                    SVarDeclStmt(StringTypeExp, "x", SString("Hello"))
                ),

                SCommand(SString(new S.ExpStringExpElement(exp = SId("x"))))
            );

            var errors = TranslateWithErrors(syntaxScript);

            VerifyError(errors, A2007_ResolveIdentifier_NotFound, exp);
        }

        [Fact]
        public void AsyncStmt_TranslatesWithGlobalVariable()
        {
            var syntaxScript = SScript(
                SVarDeclStmt(IntTypeExp, "x"),
                new S.AsyncStmt(
                    new S.ExpStmt(
                        new S.BinaryOpExp(S.BinaryOpKind.Assign, SId("x"), SInt(3))
                    )
                )
            );

            var script = Translate(syntaxScript);

            var expected = RScript(
                RGlobalVarDeclStmt(R.Type.Int, "x", null),
                new R.AsyncStmt(
                    new R.ExpStmt(new R.ExpInfo(new R.AssignExp(new R.GlobalVarExp("x"), RInt(3)), R.Type.Int)),
                    new R.CaptureInfo(false, default)
                )
            );

            Assert.Equal(expected, script, IR0EqualityComparer.Instance);
        }

        [Fact]
        public void AsyncStmt_ChecksAssignToLocalVariableOutsideLambda()
        {
            S.Exp exp;

            var syntaxScript = SScript(
                SBlock(
                    SVarDeclStmt(IntTypeExp, "x"),
                    new S.AsyncStmt(
                        new S.ExpStmt(
                            new S.BinaryOpExp(S.BinaryOpKind.Assign, exp = SId("x"), SInt(3))
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
            var syntaxScript = SScript(
                SBlock(
                    SVarDeclStmt(IntTypeExp, "x"),
                    new S.AsyncStmt(
                        SVarDeclStmt(IntTypeExp, "x", SId("x"))
                    )
                )
            );

            var script = Translate(syntaxScript);

            var expected = RScript(
                RBlock(
                    RLocalVarDeclStmt(R.Type.Int, "x"),
                    new R.AsyncStmt(
                        RLocalVarDeclStmt(R.Type.Int, "x", new R.LocalVarExp("x")),
                        new R.CaptureInfo(false, Arr(new R.CaptureInfo.Element(R.Type.Int, "x")))
                    )
                )
            );

            Assert.Equal(expected, script, IR0EqualityComparer.Instance);
        }

        [Fact]
        public void ForeachStmt_TranslatesTrivially()
        {
            var scriptSyntax = SScript(new S.ForeachStmt(IntTypeExp, "x", new S.ListExp(IntTypeExp, default), S.BlankStmt.Instance));

            var script = Translate(scriptSyntax);

            var expected = RScript(new R.ForeachStmt(R.Type.Int, "x",
                new R.ExpInfo(new R.ListExp(R.Type.Int, default),
                R.Type.List(R.Type.Int)), R.BlankStmt.Instance
            ));

            Assert.Equal(expected, script, IR0EqualityComparer.Instance);
        }

        [Fact]
        public void ForeachStmt_ChecksIteratorIsListOrEnumerable()
        {
            S.Exp iterator;
            var scriptSyntax = SScript(new S.ForeachStmt(IntTypeExp, "x", iterator = SInt(3), S.BlankStmt.Instance));

            var errors = TranslateWithErrors(scriptSyntax);

            VerifyError(errors, A1801_ForeachStmt_IteratorShouldBeListOrEnumerable, iterator);
        }

        [Fact]
        public void ForeachStmt_ChecksIteratorIsListOrEnumerable2()
        {
            // iterator type이 normal type이 아닐때.. 재현하기 쉽지 않은것 같다
            throw new PrerequisiteRequiredException(Prerequisite.Generics);
        }

        [Fact]
        public void ForeachStmt_ChecksElemTypeIsAssignableFromIteratorElemType()
        {
            S.ForeachStmt foreachStmt;
            var scriptSyntax = SScript(foreachStmt = new S.ForeachStmt(StringTypeExp, "x", new S.ListExp(IntTypeExp, default), S.BlankStmt.Instance));

            var errors = TranslateWithErrors(scriptSyntax);

            VerifyError(errors, A1802_ForeachStmt_MismatchBetweenElemTypeAndIteratorElemType, foreachStmt);
        }

        [Fact]
        public void YieldStmt_TranslatesTrivially()
        {
            var syntaxScript = SScript(
                new S.GlobalFuncDeclScriptElement(new S.GlobalFuncDecl(
                    true, IntTypeExp, "Func", default, new S.FuncParamInfo(default, null),
                    SBlock(
                        new S.YieldStmt(SInt(3))
                    )
                ))
            );

            var script = Translate(syntaxScript);

            var seqFunc = new R.SequenceFuncDecl(new R.FuncDeclId(0), R.Type.Int, false, default, default, RBlock(
                new R.YieldStmt(RInt(3))
            ));

            var expected = RScript(null, new R.FuncDecl[] { seqFunc });

            Assert.Equal(expected, script, IR0EqualityComparer.Instance);
        }

        [Fact]
        public void YieldStmt_ChecksYieldStmtUsedInSeqFunc()
        {
            S.YieldStmt yieldStmt;

            var syntaxScript = SScript(
                new S.GlobalFuncDeclScriptElement(new S.GlobalFuncDecl(
                    false, IntTypeExp, "Func", default, new S.FuncParamInfo(default, null),
                    SBlock(
                        yieldStmt = new S.YieldStmt(SInt(3))
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

            var syntaxScript = SScript(
                new S.GlobalFuncDeclScriptElement(new S.GlobalFuncDecl(
                    true, StringTypeExp, "Func", default, new S.FuncParamInfo(default, null),
                    SBlock(
                        new S.YieldStmt(yieldValue = SInt(3))
                    )
                ))
            );

            var errors = TranslateWithErrors(syntaxScript);

            VerifyError(errors, A1402_YieldStmt_MismatchBetweenYieldValueAndSeqFuncYieldType, yieldValue);
        }

        [Fact]
        public void YieldStmt_UsesHintTypeValue()
        {
            throw new PrerequisiteRequiredException(Prerequisite.Enum, Prerequisite.TypeHint);
        }

        // IdExp
        [Fact]
        public void IdExp_TranslatesIntoExternalGlobal()
        {
            throw new PrerequisiteRequiredException(Prerequisite.External);
        }

        [Fact]
        public void IdExp_TranslatesIntoGlobalVarExp()
        {
            var syntaxScript = SScript(
                SVarDeclStmt(IntTypeExp, "x", SInt(3)),
                SVarDeclStmt(IntTypeExp, "y", SId("x"))
            );

            var script = Translate(syntaxScript);
            var expected = RScript(
                RGlobalVarDeclStmt(R.Type.Int, "x", RInt(3)),
                RGlobalVarDeclStmt(R.Type.Int, "y", new R.GlobalVarExp("x"))
            );

            Assert.Equal(expected, script, IR0EqualityComparer.Instance);
        }

        [Fact]
        public void IdExp_TranslatesLocalVarOutsideLambdaIntoLocalVarExp()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public void IdExp_TranslatesIntoLocalVarExp()
        {
            var syntaxScript = SScript(SBlock(
                SVarDeclStmt(IntTypeExp, "x", SInt(3)),
                SVarDeclStmt(IntTypeExp, "y", SId("x"))
            ));

            var script = Translate(syntaxScript);
            var expected = RScript(RBlock(
                RLocalVarDeclStmt(R.Type.Int, "x", RInt(3)),
                RLocalVarDeclStmt(R.Type.Int, "y", new R.LocalVarExp("x"))
            ));

            Assert.Equal(expected, script, IR0EqualityComparer.Instance);
        }

        [Fact]
        public void IdExp_TranslatesIntoStaticMemberExp() 
        {
            throw new PrerequisiteRequiredException(Prerequisite.Static);
        }

        [Fact]
        public void IdExp_TranslatesIntoClassMemberExp()
        {
            throw new PrerequisiteRequiredException(Prerequisite.Class);
        }

        [Fact]
        public void IdExp_TranslatesIntoStructMemberExp()
        {
            throw new PrerequisiteRequiredException(Prerequisite.Struct);
        }

        [Fact]
        public void IdExp_TranslatesIntoEnumMemberExp()
        {
            throw new PrerequisiteRequiredException(Prerequisite.Enum);
        }

        [Fact]
        public void IdExp_TranslatesIntoInterfaceMemberExp()
        {
            throw new PrerequisiteRequiredException(Prerequisite.Interface);
        }

        [Fact]
        public void IdExp_TranslatesIntoNewEnumExp()
        {
            throw new PrerequisiteRequiredException(Prerequisite.Enum);
        }

        [Fact]
        public void BoolLiteralExp_TranslatesTrivially()
        {
            var syntaxScript = SScript(
                SVarDeclStmt(BoolTypeExp, "b1", new S.BoolLiteralExp(false)),
                SVarDeclStmt(BoolTypeExp, "b2", new S.BoolLiteralExp(true)));

            var script = Translate(syntaxScript);
            var expected = RScript(
                RGlobalVarDeclStmt(R.Type.Bool, "b1", new R.BoolLiteralExp(false)),
                RGlobalVarDeclStmt(R.Type.Bool, "b2", new R.BoolLiteralExp(true))                    
            );

            Assert.Equal(expected, script, IR0EqualityComparer.Instance);
        }

        [Fact]
        public void IntLiteralExp_TranslatesTrivially()
        {
            var syntaxScript = SScript(
                SVarDeclStmt(IntTypeExp, "i", new S.IntLiteralExp(34)));                

            var script = Translate(syntaxScript);
            var expected = RScript(
                RGlobalVarDeclStmt(R.Type.Int, "i", new R.IntLiteralExp(34))
            );

            Assert.Equal(expected, script, IR0EqualityComparer.Instance);
        }

        [Fact]
        public void StringExp_TranslatesTrivially()
        {
            var syntaxScript = SScript(
                SVarDeclStmt(StringTypeExp, "s", SString("Hello")));

            var script = Translate(syntaxScript);
            var expected = RScript(
                RGlobalVarDeclStmt(R.Type.String, "s", RString("Hello"))
            );

            Assert.Equal(expected, script, IR0EqualityComparer.Instance);
        }

        [Fact]
        public void StringExp_WrapsExpStringExpElementWhenExpIsBoolOrInt()
        {
            var syntaxScript = SScript(
                SVarDeclStmt(StringTypeExp, "s1", SString(new S.ExpStringExpElement(SInt(3)))),
                SVarDeclStmt(StringTypeExp, "s2", SString(new S.ExpStringExpElement(SBool(true))))
            );

            var script = Translate(syntaxScript, true);

            var expected = RScript(

                RGlobalVarDeclStmt(R.Type.String, "s1", RString(new R.ExpStringExpElement(
                    new R.CallInternalUnaryOperatorExp(R.InternalUnaryOperator.ToString_Int_String, new R.ExpInfo(RInt(3), R.Type.Int))
                ))),

                RGlobalVarDeclStmt(R.Type.String, "s2", RString(new R.ExpStringExpElement(
                    new R.CallInternalUnaryOperatorExp(R.InternalUnaryOperator.ToString_Bool_String, new R.ExpInfo(RBool(true), R.Type.Bool))
                )))
            );

            Assert.Equal(expected, script, IR0EqualityComparer.Instance);
        }

        [Fact]
        public void StringExp_ChecksStringExpElementIsStringConvertible()
        {
            S.Exp exp;
            // convertible가능하지 않은 것,, Lambda?
            var syntaxScript = SScript(
                SVarDeclStmt(StringTypeExp, "s", SString(new S.ExpStringExpElement(exp = new S.LambdaExp(
                    Arr<S.LambdaExpParam>(),
                    S.BlankStmt.Instance
                ))))
            );

            var errors = TranslateWithErrors(syntaxScript);

            VerifyError(errors, A1901_StringExp_ExpElementShouldBeBoolOrIntOrString, exp);
        }

        [Fact]
        public void UnaryOpExp_TranslatesTrivially()
        {
            var syntaxScript = SScript(
                SVarDeclStmt(VarTypeExp, "x1", new S.UnaryOpExp(S.UnaryOpKind.LogicalNot, SBool(false))),
                SVarDeclStmt(VarTypeExp, "x2", new S.UnaryOpExp(S.UnaryOpKind.Minus, SInt(3))),
                SVarDeclStmt(VarTypeExp, "x3", new S.UnaryOpExp(S.UnaryOpKind.PrefixInc, SId("x2"))),
                SVarDeclStmt(VarTypeExp, "x4", new S.UnaryOpExp(S.UnaryOpKind.PrefixDec, SId("x2"))),
                SVarDeclStmt(VarTypeExp, "x5", new S.UnaryOpExp(S.UnaryOpKind.PostfixInc, SId("x2"))),
                SVarDeclStmt(VarTypeExp, "x6", new S.UnaryOpExp(S.UnaryOpKind.PostfixDec, SId("x2")))
            );

            var script = Translate(syntaxScript);

            var expected = RScript(
                RGlobalVarDeclStmt(R.Type.Bool, "x1", new R.CallInternalUnaryOperatorExp(R.InternalUnaryOperator.LogicalNot_Bool_Bool, new R.ExpInfo(RBool(false), R.Type.Bool))),
                RGlobalVarDeclStmt(R.Type.Int, "x2", new R.CallInternalUnaryOperatorExp(R.InternalUnaryOperator.UnaryMinus_Int_Int, new R.ExpInfo(RInt(3), R.Type.Int))),
                RGlobalVarDeclStmt(R.Type.Int, "x3", new R.CallInternalUnaryAssignOperator(R.InternalUnaryAssignOperator.PrefixInc_Int_Int, new R.GlobalVarExp("x2"))),
                RGlobalVarDeclStmt(R.Type.Int, "x4", new R.CallInternalUnaryAssignOperator(R.InternalUnaryAssignOperator.PrefixDec_Int_Int, new R.GlobalVarExp("x2"))),
                RGlobalVarDeclStmt(R.Type.Int, "x5", new R.CallInternalUnaryAssignOperator(R.InternalUnaryAssignOperator.PostfixInc_Int_Int, new R.GlobalVarExp("x2"))),
                RGlobalVarDeclStmt(R.Type.Int, "x6", new R.CallInternalUnaryAssignOperator(R.InternalUnaryAssignOperator.PostfixDec_Int_Int, new R.GlobalVarExp("x2")))
            );

            Assert.Equal(expected, script, IR0EqualityComparer.Instance);
        }

        [Fact]
        public void UnaryOpExp_ChecksOperandOfUnaryAssignExpIsIntType()
        {
            S.Exp operand;
            var syntaxScript = SScript(
                SVarDeclStmt(StringTypeExp, "x", SString("Hello")),
                SVarDeclStmt(VarTypeExp, "i", new S.UnaryOpExp(S.UnaryOpKind.PrefixInc, operand = SId("x")))
            );

            var errors = TranslateWithErrors(syntaxScript);
            VerifyError(errors, A0601_UnaryAssignOp_IntTypeIsAllowedOnly, operand);
        }

        [Fact]
        public void UnaryOpExp_ChecksOperandOfUnaryAssignExpIsAssignable()
        {
            S.Exp operand;
            var syntaxScript = SScript(
                SVarDeclStmt(VarTypeExp, "i", new S.UnaryOpExp(S.UnaryOpKind.PrefixInc, operand = SInt(3)))
            );

            var errors = TranslateWithErrors(syntaxScript);
            VerifyError(errors, A0602_UnaryAssignOp_AssignableExpressionIsAllowedOnly, operand);
        }

        [Fact]
        public void UnaryOpExp_ChecksOperandOfLogicalNotIsBoolType()
        {
            S.Exp operand;
            var syntaxScript = SScript(
                SVarDeclStmt(VarTypeExp, "b", new S.UnaryOpExp(S.UnaryOpKind.LogicalNot, operand = SInt(3)))
            );

            var errors = TranslateWithErrors(syntaxScript);
            VerifyError(errors, A0701_UnaryOp_LogicalNotOperatorIsAppliedToBoolTypeOperandOnly, operand);
        }

        [Fact]
        public void UnaryOpExp_ChecksOperandOfUnaryMinusIsIntType()
        {
            S.Exp operand;
            var syntaxScript = SScript(
                SVarDeclStmt(VarTypeExp, "i", new S.UnaryOpExp(S.UnaryOpKind.Minus, operand = SBool(false)))
            );

            var errors = TranslateWithErrors(syntaxScript);
            VerifyError(errors, A0702_UnaryOp_UnaryMinusOperatorIsAppliedToIntTypeOperandOnly, operand);
        }

        [Fact]
        void BinaryOpExp_TranslatesIntoAssignExp()
        {
            var syntaxScript = SScript(
                SVarDeclStmt(IntTypeExp, "x"),
                new S.ExpStmt(new S.BinaryOpExp(S.BinaryOpKind.Assign, SId("x"), SInt(3)))
            );

            var script = Translate(syntaxScript);
            var expected = RScript(
                RGlobalVarDeclStmt(R.Type.Int, "x"),
                new R.ExpStmt(new R.ExpInfo(new R.AssignExp(new R.GlobalVarExp("x"), RInt(3)), R.Type.Int))
            );

            Assert.Equal(expected, script, IR0EqualityComparer.Instance);
        }

        [Fact]
        void BinaryOpExp_ChecksCompatibleBetweenOperandsOnAssignOperation()
        {
            S.BinaryOpExp binOpExp;
            var syntaxScript = SScript(
                SVarDeclStmt(IntTypeExp, "x"),
                new S.ExpStmt(binOpExp = new S.BinaryOpExp(S.BinaryOpKind.Assign, SId("x"), SBool(true)))
            );

            var errors = TranslateWithErrors(syntaxScript);
            VerifyError(errors, A0801_BinaryOp_LeftOperandTypeIsNotCompatibleWithRightOperandType, binOpExp);
        }

        [Fact]
        void BinaryOpExp_ChecksLeftOperandIsAssignableOnAssignOperation()
        {
            S.Exp exp;
            var syntaxScript = SScript(
                new S.ExpStmt(new S.BinaryOpExp(S.BinaryOpKind.Assign, exp = SInt(3), SInt(4)))
            );

            var errors = TranslateWithErrors(syntaxScript);
            VerifyError(errors, A0803_BinaryOp_LeftOperandIsNotAssignable, exp);
        }

        [Fact]
        void BinaryOpExp_ChecksOperatorNotFound()
        {
            S.Exp exp;
            var syntaxScript = SScript(
                SVarDeclStmt(StringTypeExp, "x", exp = new S.BinaryOpExp(S.BinaryOpKind.Multiply, SString("Hello"), SInt(4)))
            );

            var errors = TranslateWithErrors(syntaxScript);
            VerifyError(errors, A0802_BinaryOp_OperatorNotFound, exp);
        }

        static IEnumerable<object[]> Data_BinaryOpExp_TranslatesIntoCallInternalBinaryOperatorExp_Trivial()
        {
            Func<S.Exp> MakeInt = () => SInt(1);
            Func<R.ExpInfo> IntInfo = () => new R.ExpInfo(RInt(1), R.Type.Int);

            yield return new object[] { S.BinaryOpKind.Multiply, MakeInt, IntInfo, R.Type.Int, R.InternalBinaryOperator.Multiply_Int_Int_Int };
            yield return new object[] { S.BinaryOpKind.Divide, MakeInt, IntInfo, R.Type.Int, R.InternalBinaryOperator.Divide_Int_Int_Int};
            yield return new object[] { S.BinaryOpKind.Modulo, MakeInt, IntInfo, R.Type.Int, R.InternalBinaryOperator.Modulo_Int_Int_Int };
            yield return new object[] { S.BinaryOpKind.Add, MakeInt, IntInfo, R.Type.Int, R.InternalBinaryOperator.Add_Int_Int_Int };
            yield return new object[] { S.BinaryOpKind.Subtract, MakeInt, IntInfo, R.Type.Int, R.InternalBinaryOperator.Subtract_Int_Int_Int };
            yield return new object[] { S.BinaryOpKind.LessThan, MakeInt, IntInfo, R.Type.Bool, R.InternalBinaryOperator.LessThan_Int_Int_Bool };
            yield return new object[] { S.BinaryOpKind.GreaterThan, MakeInt, IntInfo, R.Type.Bool, R.InternalBinaryOperator.GreaterThan_Int_Int_Bool };
            yield return new object[] { S.BinaryOpKind.LessThanOrEqual, MakeInt, IntInfo, R.Type.Bool, R.InternalBinaryOperator.LessThanOrEqual_Int_Int_Bool };
            yield return new object[] { S.BinaryOpKind.GreaterThanOrEqual, MakeInt, IntInfo, R.Type.Bool, R.InternalBinaryOperator.GreaterThanOrEqual_Int_Int_Bool };
            yield return new object[] { S.BinaryOpKind.Equal, MakeInt, IntInfo, R.Type.Bool, R.InternalBinaryOperator.Equal_Int_Int_Bool };

            Func<S.Exp> MakeString = () => SString("Hello");
            Func<R.ExpInfo> StringInfo = () => new R.ExpInfo(RString("Hello"), R.Type.String);

            yield return new object[] { S.BinaryOpKind.Add, MakeString, StringInfo, R.Type.String, R.InternalBinaryOperator.Add_String_String_String };
            yield return new object[] { S.BinaryOpKind.LessThan, MakeString, StringInfo, R.Type.Bool, R.InternalBinaryOperator.LessThan_String_String_Bool };
            yield return new object[] { S.BinaryOpKind.GreaterThan, MakeString, StringInfo, R.Type.Bool, R.InternalBinaryOperator.GreaterThan_String_String_Bool };
            yield return new object[] { S.BinaryOpKind.LessThanOrEqual, MakeString, StringInfo, R.Type.Bool, R.InternalBinaryOperator.LessThanOrEqual_String_String_Bool };
            yield return new object[] { S.BinaryOpKind.GreaterThanOrEqual, MakeString, StringInfo, R.Type.Bool, R.InternalBinaryOperator.GreaterThanOrEqual_String_String_Bool };
            yield return new object[] { S.BinaryOpKind.Equal, MakeString, StringInfo, R.Type.Bool, R.InternalBinaryOperator.Equal_String_String_Bool };

            Func<S.Exp> MakeBool = () => SBool(true);
            Func<R.ExpInfo> BoolInfo = () => new R.ExpInfo(RBool(true), R.Type.Bool);

            yield return new object[] { S.BinaryOpKind.Equal, MakeBool, BoolInfo, R.Type.Bool, R.InternalBinaryOperator.Equal_Bool_Bool_Bool};

            // NotEqual
        }

        [Theory]
        [MemberData(nameof(Data_BinaryOpExp_TranslatesIntoCallInternalBinaryOperatorExp_Trivial))]
        public void BinaryOpExp_TranslatesIntoCallInternalBinaryOperatorExp_Trivial(
            S.BinaryOpKind syntaxOpKind, Func<S.Exp> newSOperand, Func<R.ExpInfo> newOperandInfo, R.Type resultType, R.InternalBinaryOperator ir0BinOp)
        {
            var syntaxScript = SScript(
                SVarDeclStmt(VarTypeExp, "x", new S.BinaryOpExp(syntaxOpKind, newSOperand.Invoke(), newSOperand.Invoke()))
            );

            var script = Translate(syntaxScript);
            var expected = RScript(
                RGlobalVarDeclStmt(resultType, "x", new R.CallInternalBinaryOperatorExp(ir0BinOp,
                    newOperandInfo.Invoke(),
                    newOperandInfo.Invoke()
                ))
            );

            Assert.Equal(expected, script, IR0EqualityComparer.Instance);
        }

        static IEnumerable<object[]> Data_BinaryOpExp_TranslatesIntoCallInternalBinaryOperatorExp_NotEqual()
        {
            Func<S.Exp> MakeBool = () => SBool(true);
            Func<R.ExpInfo> BoolInfo = () => new R.ExpInfo(RBool(true), R.Type.Bool);

            Func<S.Exp> MakeInt = () => SInt(1);
            Func<R.ExpInfo> IntInfo = () => new R.ExpInfo(RInt(1), R.Type.Int);

            Func<S.Exp> MakeString = () => SString("Hello");
            Func<R.ExpInfo> StringInfo = () => new R.ExpInfo(RString("Hello"), R.Type.String);

            yield return new object[] { MakeBool, BoolInfo, R.InternalBinaryOperator.Equal_Bool_Bool_Bool };
            yield return new object[] { MakeInt, IntInfo, R.InternalBinaryOperator.Equal_Int_Int_Bool };
            yield return new object[] { MakeString, StringInfo, R.InternalBinaryOperator.Equal_String_String_Bool };
        }

        [Theory]
        [MemberData(nameof(Data_BinaryOpExp_TranslatesIntoCallInternalBinaryOperatorExp_NotEqual))]
        public void BinaryOpExp_TranslatesIntoCallInternalBinaryOperatorExp_NotEqual(
            Func<S.Exp> newSOperand, Func<R.ExpInfo> newOperandInfo, R.InternalBinaryOperator ir0BinOperator)
        {
            var syntaxScript = SScript(
                SVarDeclStmt(VarTypeExp, "x", new S.BinaryOpExp(S.BinaryOpKind.NotEqual, newSOperand.Invoke(), newSOperand.Invoke()))
            );

            var script = Translate(syntaxScript);
            var expected = RScript(
                RGlobalVarDeclStmt(R.Type.Bool, "x", 
                    new R.CallInternalUnaryOperatorExp(R.InternalUnaryOperator.LogicalNot_Bool_Bool, new R.ExpInfo(
                        new R.CallInternalBinaryOperatorExp(ir0BinOperator, newOperandInfo.Invoke(), newOperandInfo.Invoke()),
                        R.Type.Bool
                    ))
                )
            );

            Assert.Equal(expected, script, IR0EqualityComparer.Instance);
        }

        [Fact]
        void CallExp_TranslatesIntoNewEnumExp()
        {
            throw new PrerequisiteRequiredException(Prerequisite.Enum);
        }

        [Fact]
        void CallExp_TranslatesIntoCallFuncExp()
        {
            var syntaxScript = SScript(
                new S.GlobalFuncDeclScriptElement(new S.GlobalFuncDecl(
                    false, IntTypeExp, "Func", Arr("T"), 
                    new S.FuncParamInfo(Arr(new S.TypeAndName(IntTypeExp, "x")), null),
                    SBlock(new S.ReturnStmt(SId("x")))
                )),

                new S.StmtScriptElement(
                    new S.ExpStmt(new S.CallExp(SId("Func", IntTypeExp), Arr<S.Exp>(SInt(3))))
                )
            );

            var script = Translate(syntaxScript);

            var expected = RScript(null,
                Arr(
                    new R.NormalFuncDecl(
                        new R.FuncDeclId(0), false, Arr("T"), Arr("x"), RBlock(new R.ReturnStmt(new R.LocalVarExp("x")))
                    )
                )
            );

            Assert.Equal(expected, script, IR0EqualityComparer.Instance);
        }

        // TODO: TypeArgument지원하면 General버전이랑 통합하기
        [Fact]
        void CallExp_TranslatesIntoCallFuncExpWithoutTypeArgument()
        {
            var syntaxScript = SScript(
                new S.GlobalFuncDeclScriptElement(new S.GlobalFuncDecl(
                    false, IntTypeExp, "Func", Arr<string>(),
                    new S.FuncParamInfo(Arr(new S.TypeAndName(IntTypeExp, "x")), null),
                    SBlock(new S.ReturnStmt(SId("x")))
                )),

                new S.StmtScriptElement(
                    new S.ExpStmt(new S.CallExp(SId("Func"), Arr<S.Exp>(SInt(3))))
                )
            );

            var script = Translate(syntaxScript);

            var expected = RScript(null,
                Arr(
                    new R.NormalFuncDecl(
                        new R.FuncDeclId(0), false, Arr<string>(), Arr("x"), RBlock(new R.ReturnStmt(new R.LocalVarExp("x")))
                    )
                ),

                new R.ExpStmt(new R.ExpInfo(new R.CallFuncExp(new R.Func(new R.FuncDeclId(0), R.TypeContext.Empty), null, Arr(new R.ExpInfo(RInt(3), R.Type.Int))), R.Type.Int))
            );

            Assert.Equal(expected, script, IR0EqualityComparer.Instance);
        }

        [Fact]
        void CallExp_TranslatesIntoCallValueExp()
        {
            throw new NotImplementedException();
        }

        // A0901
        [Fact]
        void CallExp_ChecksMultipleCandidates()
        {
            throw new NotImplementedException();
        }

        // A0902
        [Fact]
        void CallExp_ChecksCallableExpressionIsNotCallable()
        {
            throw new NotImplementedException();
        }

        // A0903
        [Fact]
        void CallExp_ChecksEnumConstructorArgumentCount()
        {
            throw new NotImplementedException();
        }

        // A0904
        [Fact]
        void CallExp_ChecksEnumConstructorArgumentType()
        {
            throw new NotImplementedException();
        }        

        [Fact]
        void LambdaExp_TranslatesTrivially()
        {
            // int x; // global
            // {
            //     int y; // local
            //     var l = (int param) => { x = 3; return param + x + y; };
            // 
            // }

            throw new NotImplementedException();
        }

        [Fact]
        void LambdaExp_ChecksAssignToLocalVaraiableOutsideLambda()
        {
            throw new NotImplementedException();
        }

        [Fact]
        void IndexerExp_TranslatesTrivially()
        {
            // var s = [1, 2, 3, 4];
            // var i = s[3];

            throw new NotImplementedException();
        }
        
        [Fact]
        void IndexerExp_ChecksInstanceIsList() // TODO: Indexable로 확장
        {
            throw new NotImplementedException();
        }

        [Fact]
        void IndexerExp_ChecksIndexIsInt() // TODO: Indexable인자 타입에 따라 달라짐
        {
            throw new NotImplementedException();
        }

        // MemberCallExp
        // 1. x.F();     // instance call (class, struct, interface)
        // 2. C.F();     // static call (class static, struct static, global)
        // 3. E.F(1, 2); // enum constructor
        // 4. x.f();     // instance lambda call (class, struct, interface)
        // 5. C.f();     // static lambda call (class static, struct static, global)

        [Fact]
        void MemberCallExp_TranslatesIntoCallFuncExp() // 1, 2
        {
            
            throw new NotImplementedException();
        }

        [Fact]
        void MemberCallExp_TranslatesIntoNewEnumExp() // 3
        {   
            throw new NotImplementedException();
        }

        [Fact]
        void MemberCallExp_TranslatesIntoCallValueExp() // 4, 5
        {
            
            throw new NotImplementedException();
        }

        [Fact]
        void MemberCallExp_ChecksCallableExpressionIsNotCallable() // 4, 5
        {
            throw new NotImplementedException();
        }

        [Fact]
        void MemberCallExp_EnumConstructorArgumentCount() // 3
        {
            throw new NotImplementedException();
        }

        [Fact]
        void MemberCallExp_EnumConstructorArgumentType() // 3
        {
            throw new NotImplementedException();
        }

        [Fact]
        void MemberCallExp_ChecksMultipleCandidates() // 1, 2, 4, 5
        {
            throw new NotImplementedException();
        }

        // MemberExp
        // 1. E.Second  // enum constructor without parameter
        // 2. e.X       // enum field (EnumMemberExp)        
        // 3. C.x       // static 
        // 4. c.x       // instance  (class, struct, interface)
        [Fact]
        void MemberExp_TranslatesIntoNewEnumExp() // 1.
        {
            throw new NotImplementedException();
        }

        [Fact]
        void MemberExp_TranslatesIntoEnumMemberExp() // 2
        {
            throw new NotImplementedException();
        }

        [Fact]
        void MemberExp_TranslatesIntoStaticMemberExp() // 3
        {
            throw new NotImplementedException();
        }

        [Fact]
        void MemberExp_TranslatesIntoStructMemberExp() // 4
        {
            throw new NotImplementedException();
        }

        [Fact]
        void MemberExp_TranslatesIntoClassMemberExp() // 4
        {
            throw new NotImplementedException();
        }

        [Fact]
        void MemberExp_ChecksMemberNotFound() // TODO: enum, class, struct, interface 각각의 경우에 해야 하지 않는가
        {
            throw new NotImplementedException();
        }

        //case S.MemberExp memberExp: return AnalyzeMemberExp(memberExp, context, out outExp, out outTypeValue);

        [Fact]
        void ListExp_TranslatesTrivially() // TODO: 타입이 적힌 빈 리스트도 포함, <int>[]
        {
            throw new NotImplementedException();
        }

        [Fact]
        void ListExp_UsesHintTypeToInferElementType() // List<int> s = []; 
        {
            throw new NotImplementedException();
        }

        [Fact]
        void ListExp_ChecksCantInferEmptyElementType() // var x = []; // ???
        {
            throw new NotImplementedException();
        }

        [Fact]
        void ListExp_ChecksCantInferElementType() // var = ["string", 1, false];
        {
            throw new NotImplementedException();
        }
    }
}
