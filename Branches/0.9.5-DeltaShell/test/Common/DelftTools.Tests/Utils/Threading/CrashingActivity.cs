using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using DelftTools.Utils.Workflow;

namespace DelftTools.Tests.Utils.Threading
{
    public class CrashingActivity : IActivity
    {
        private string name;

        private ActivityStatus status;

        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        public ActivityStatus Status
        {
            get { return status; }
        }

        public void Initialize()
        {
            throw new NotImplementedException();
        }

        public void Execute()
        {
            throw new NotImplementedException("Exception!");
        }

        public void Cancel()
        {
            throw new NotImplementedException();
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}