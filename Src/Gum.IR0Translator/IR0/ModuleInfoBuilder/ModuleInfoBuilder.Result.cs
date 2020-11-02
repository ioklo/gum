﻿using Gum.CompileTime;
using System.Collections.Immutable;
using S = Gum.Syntax;

namespace Gum.IR0
{
    partial class ModuleInfoBuilder
    {
        public class Result
        {
            public ScriptModuleInfo ModuleInfo { get; }
            public TypeExpTypeValueService TypeExpTypeValueService { get; }
            public ImmutableDictionary<S.FuncDecl, FuncInfo> FuncInfosByDecl { get; }
            public ImmutableDictionary<S.EnumDecl, EnumInfo> EnumInfosByDecl{ get; }

            public Result(
                ScriptModuleInfo moduleInfo,
                TypeExpTypeValueService typeExpTypeValueService,
                ImmutableDictionary<S.FuncDecl, FuncInfo> funcInfosbyDecl,
                ImmutableDictionary<S.EnumDecl, EnumInfo> enumInfosByDecl)
            {
                ModuleInfo = moduleInfo;
                TypeExpTypeValueService = typeExpTypeValueService;
                FuncInfosByDecl = funcInfosbyDecl;
                EnumInfosByDecl = enumInfosByDecl;
            }
        }
    }
}