using System;
using DelftTools.Shell.Core;
using DelftTools.Shell.Core.Workflow;
using DelftTools.Units.Generics;

namespace DelftTools.Tests.Core.Mocks
{
    public class TestModel : TimeDependentModelBase
    {
        public TestModel() : this("new test model")
        {
        }

        public TestModel(string name) : base(name)
        {
            // add some test data items
            base.DataItems.Add(new DataItem(new Parameter<int>("int parameter")) {Role = DataItemRole.Output});

            // time step parameter
            TimeStep =  new TimeSpan(0, 0, 5);
        }

        public override void OnCleanup()
        {
            throw new Exception("The method or operation is not implemented.");
        }

        protected override void OnInitialize()
        {
        }

        protected override bool OnExecute()
        {
            Parameter<int> p = DataItems[4].Value as Parameter<int>;
            if (p != null) p.Value = 2 + 2;

            return true;
        }

        public override void LoadDataFromDirectory(string path)
        {
        }

        public override void SaveDataToDirectory(string path)
        {
            throw new NotImplementedException("SaveState is not implemented");
        }

        public override bool IsDataItemValid(IDataItem dataItem)
        {
            return true;
        }

        public override bool IsLinkAllowed(IDataItem source, IDataItem target)
        {
            return true;
        }
    }
}















