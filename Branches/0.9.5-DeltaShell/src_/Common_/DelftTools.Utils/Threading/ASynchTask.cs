using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using DelftTools.Utils.Workflow;

namespace DelftTools.Utils.Threading
{
    
    /// <summary>
    /// A ASynchtask is a composition of BackGroundWorker doing the job and the Activity to be done.
    /// </summary>
    public class ASynchTask
    {
        public event EventHandler TaskCompleted;
        //breaks encapsulation.. get it private again?
        public IActivity Activity { get; private set; }
        private readonly BackgroundWorker backgroundWorker;
        private readonly Action<IActivity> action;
        
        /// <summary>
        /// A task completed succesfully if it ran without exceptions.
        /// </summary>
        public bool TaskCompletedSuccesFully { get; private set; }

        public ASynchTask(IActivity activity, Action<IActivity> action)
        {
            Activity = activity;
            this.action = action;
            backgroundWorker = new BackgroundWorker();
            backgroundWorker.DoWork += BackgroundWorkerDoWork;
            backgroundWorker.WorkerSupportsCancellation = true;
            backgroundWorker.RunWorkerCompleted += BackgroundWorkerRunWorkerCompleted;
        }

        void BackgroundWorkerDoWork(object sender, DoWorkEventArgs e)
        {
            action(Activity);
        }

        private void BackgroundWorkerRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            //no error is 
            TaskCompletedSuccesFully = e.Error == null;
            OnTaskCompleted();
        }

        private void OnTaskCompleted()
        {
            if (TaskCompleted != null)
                TaskCompleted(this, EventArgs.Empty);
        }


        public void Run()
        {
            //Hack: problems with multi thread, run in main thread
            //ToDo ReEnable DeltaShellApplicationIntegrationTest
            /*DoWorkEventArgs doWorkEventArgs = new DoWorkEventArgs(null);

            BackgroundWorkerDoWork(this, doWorkEventArgs);
            BackgroundWorkerRunWorkerCompleted(this, null);*/

            //Hack DeltaShellApplicationIntegrationTest
            backgroundWorker.RunWorkerAsync();
        }

        public void Cancel()
        {
            Activity.Cancel();
        }
    }
}