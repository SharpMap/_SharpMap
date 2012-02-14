using System;
using System.ComponentModel;
using DelftTools.Shell.Core;
using DelftTools.Shell.Core.Workflow;

namespace DelftTools.Tests.Core.Mocks
{
    public class SimplerModel : TimeDependentModelBase
    {
        public event EventHandler Executing;

        protected override void OnInitialize()
        {
            //throw new NotImplementedException();
        }

        protected override bool OnExecute()
        {
            //throw new NotImplementedException();
            if (null != Executing)
            {
                Executing(this, null);
            }
            return true;
        }

        protected override void OnDataItemRemoved(IDataItem item)
        {

        }

        protected override void OnDataItemAdded(IDataItem item)
        {

        }

        protected override void OnInputPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            OnInputDataChangedCallCount++;

            base.OnInputPropertyChanged(sender, e);
        }

        /// <summary>
        /// Used by test.
        /// </summary>
        public int OnInputDataChangedCallCount { get; set; }
    }
}