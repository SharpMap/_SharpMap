using System;

namespace DelftTools.Utils.Workflow.Generics
{
    public interface IProgressingActivity:IActivity
    {
        event EventHandler ProgressChanged;
    }

    public interface IProgressingActivity<T, TStep> : IProgressingActivity where T : IComparable 
    {
        T ProgressStart { get; set; }
        T ProgressStop { get; set; }

        T ProgressCurrent { get; }

        TStep ProgressStep { get; set; }
    }
}