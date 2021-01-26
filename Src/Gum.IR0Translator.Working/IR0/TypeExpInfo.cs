﻿using Pretune;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using M = Gum.CompileTime;

namespace Gum.IR0
{
    // X<int>.Y<T>
    abstract class TypeExpInfo
    {        
    }
    
    [ImplementIEquatable]
    partial class MTypeTypeExpInfo : TypeExpInfo
    {
        public M.Type Type { get; }
        public MTypeTypeExpInfo(M.Type type)
        {
            Type = type;
        }
    }
    
    partial class VarTypeExpInfo : TypeExpInfo
    {
        public static VarTypeExpInfo Instance { get; } = new VarTypeExpInfo();
        private VarTypeExpInfo() { }
    }
   

}