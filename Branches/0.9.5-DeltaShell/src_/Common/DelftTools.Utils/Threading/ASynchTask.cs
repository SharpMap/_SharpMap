using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using DelftTools.Utils.Workflow;

namespace DelftTools.Utils.Threading
{
    
    /// <summary>
    /// A ASynchtask is a composition of BackGroundWorker doing the job and the Activity to be done.
    /// 
    /// TODO: use ParallelFx instead.
    /// </summary>
    public class ASynchTask : IDisposable
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

        public Exception TaskException { get; set; }

        // TODO: why do we need to pass activity and action here? Isn't it expected that task will always call Execute?
        public ASynchTask(IActivity activity, Action<IActivity> action)
        {
            Activity = activity;
            this.action = action;
            backgroundWorker = new BackgroundWorker();
            backgroundWorker.DoWork += BackgroundWorkerDoWork;
            backgroundWorker.WorkerSupportsCancellation = true;
            backgroundWorker.RunWorkerCompleted += BackgroundWorkerRunWorkerCompleted;

            uiCulture = System.Threading.Thread.CurrentThread.CurrentUICulture;
        }

        private CultureInfo uiCulture;

        void BackgroundWorkerDoWork(object sender, DoWorkEventArgs e)
        {
            System.Threading.Thread.CurrentThread.CurrentUICulture = uiCulture;
            action(Activity);
        }

        private void BackgroundWorkerRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            //no error is 
            TaskCompletedSuccesFully = e.Error == null;
            TaskException = e.Error;
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

        public void Dispose()
        {
            backgroundWorker.Dispose();
        }
    }
}