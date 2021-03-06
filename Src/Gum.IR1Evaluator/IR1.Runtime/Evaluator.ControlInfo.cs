﻿using Gum.IR1;
using System;
using System.Collections.Generic;
using System.Text;

namespace Gum.IR1.Runtime
{
    public partial class Evaluator
    {
        enum ControlFlag
        {
            None,
            Break,
            Continue,
        }

        struct ControlInfo
        {
            public ControlFlag Flag { get; }
            public ScopeId? ScopeId { get; }

            public static ControlInfo None { get; } = new ControlInfo(ControlFlag.None, null);
            public static ControlInfo Break(ScopeId scopeId) => new ControlInfo(ControlFlag.Break, scopeId);
            public static ControlInfo Continue(ScopeId scopeId) => new ControlInfo(ControlFlag.Continue, scopeId);

            private ControlInfo(ControlFlag flag, ScopeId? scopeId)
            {
                Flag = flag;
                ScopeId = scopeId;
            }
        }
    }
}
