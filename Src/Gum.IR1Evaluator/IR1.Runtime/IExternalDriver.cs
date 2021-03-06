﻿using Gum.CompileTime;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Gum.IR1.Runtime
{
    public delegate ValueTask ExternalFuncDelegate(Evaluator.Value? retValue, params Evaluator.Value[] values);

    public interface IExternalDriver
    {
        ExternalFuncDelegate GetDelegate(ExternalDriverFuncId id);
    }
}
