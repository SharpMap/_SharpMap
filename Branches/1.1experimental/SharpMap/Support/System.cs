using System;
using System.Collections.Generic;
using System.Text;

namespace System
{

#if !DOTNET_35
    public delegate TResult Func<TResult>();
    public delegate TResult Func<TArg0, TResult>(TArg0 arg0);
    public delegate TResult Func<TArg0, TArg1, TResult>(TArg0 arg0, TArg1 arg1);

#endif
}
