using System;
using DelftTools.Shell.Core;
using DelftTools.Shell.Core.Workflow;

namespace DelftTools.Tests.Core.Mocks
{
    public class SimplerModel : TimeDependentModelBase
    {
        public event EventHandler Executing;

        public override void OnInitialize()
        {
            //throw new NotImplementedException();
        }

        public override bool OnExecute()
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

        protected override void OnInputDataItemValuePropertyChanged(IDataItem sender)
        {
            OnInputDataChangedCallCount++;
        }

        /// <summary>
        /// Used by test.
        /// </summary>
        public int OnInputDataChangedCallCount { get; set; }
    }
}