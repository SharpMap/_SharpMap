using System;
using System.ComponentModel;

namespace DelftTools.Functions.Binding
{
    public interface IFunctionBindingList : IBindingList, ITypedList
    {
        IFunction Function { get; }
        
        string[] ColumnNames { get; }

        string[] DisplayNames { get; }
        
        /// <summary>
        /// Used to allow thread-safe behaviour (e.g. in Windows.Forms).
        /// </summary>
        ISynchronizeInvoke SynchronizeInvoke { get; set; }

        /// <summary>
        /// In most cases it it equal to Application.DoEvents
        /// </summary>
        Action SynchronizeWaitMethod { get; set; }

        event EventHandler<FunctionValuesChangingEventArgs> FunctionValuesChanged;

        IVariable GetVariableForColumnIndex(int absoluteIndex);
    }
}