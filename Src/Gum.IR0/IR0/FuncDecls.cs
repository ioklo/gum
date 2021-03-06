﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace Gum.IR0
{
    public abstract class FuncDecl
    {
        public FuncDeclId Id { get; }
        public bool IsThisCall { get; }
        public ImmutableArray<string> TypeParams { get; }
        public ImmutableArray<string> ParamNames { get; }
        public Stmt Body { get; }

        internal FuncDecl(FuncDeclId id,
                bool bThisCall,
                ImmutableArray<string> typeParams,
                ImmutableArray<string> paramNames,
                Stmt body)
        {
            Id = id;
            IsThisCall = bThisCall;

            TypeParams = typeParams;
            ParamNames = paramNames;
            Body = body;
        }       
        
    }

    public class NormalFuncDecl : FuncDecl
    {
        public NormalFuncDecl(
            FuncDeclId id,
            bool bThisCall,
            ImmutableArray<string> typeParams,
            ImmutableArray<string> paramNames,
            Stmt body)
            : base(id, bThisCall, typeParams, paramNames, body)
        {
        }
    }

    public class SequenceFuncDecl : FuncDecl
    {
        public Type ElemType { get; }

        public SequenceFuncDecl(FuncDeclId id, Type elemType, bool bThisCall, ImmutableArray<string> typeParams, ImmutableArray<string> paramNames, Stmt body)
            : base(id, bThisCall, typeParams, paramNames, body)
        {
            ElemType = elemType;
        }
    }
}
