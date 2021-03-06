﻿using Pretune;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace Gum.CompileTime
{
    [AutoConstructor]
    public partial class ModuleInfo
    {
        public ModuleName Name { get; }
        public ImmutableArray<NamespaceInfo> Namespaces { get; }
        public ImmutableArray<TypeInfo> Types { get; }
        public ImmutableArray<FuncInfo> Funcs { get; }

        public NamespaceInfo? GetNamespace(NamespacePath path)
        {
            Debug.Assert(!path.IsRoot);

            NamespaceInfo? curNamespace = null;
            foreach (var entry in path.Entries)
            {
                if (curNamespace == null)
                {
                    curNamespace = Namespaces.FirstOrDefault(info => info.Name.Equals(entry));
                    if (curNamespace == null)
                        return null;
                }
                else
                {
                    curNamespace = curNamespace.Namespaces.FirstOrDefault(info => info.Name.Equals(entry));
                    if (curNamespace == null)
                        return null;
                }
            }

            return curNamespace;
        }
    }
}
