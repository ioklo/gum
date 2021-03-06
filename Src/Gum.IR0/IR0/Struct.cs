﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Gum.Infra;

namespace Gum.IR0
{
    public partial class Struct
    {
        public AccessModifier AccessModifier { get; }
        public string Name { get; }
        public ImmutableArray<string> TypeParams { get; }
        public ImmutableArray<Type> BaseTypes { get; }
        // public ImmutableArray<Element> Elems { get; }

        public Struct(
            AccessModifier accessModifier,
            string name,
            ImmutableArray<string> typeParams,
            ImmutableArray<Type> baseTypes
            // IEnumerable<Element> elems
            )
        {
            AccessModifier = accessModifier;
            Name = name;
            TypeParams = typeParams;
            BaseTypes = baseTypes;
        }
    }
}
