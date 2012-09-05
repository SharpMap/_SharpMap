using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using DelftTools.Utils.Collections;
using DelftTools.Utils.Collections.Generic;
using DelftTools.Utils.Workflow;
using log4net;

namespace DelftTools.Utils.Threading
{
    /// <summary>
    /// Class fires activities asynch and generates state changed
    /// </summary>
    public class ActivityRunner : IActivityRunner
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(ActivityRunner));

        private readonly IList<ASynchTask> runningTasks = new List<ASynchTask>();
        private readonly IList<ASynchTask> todoTasks = new List<ASynchTask>();
        private readonly IEventedList<IActivity> activities= new EventedList<IActivity>();

        /// <summary>
        /// Provides an evented summary of the current activities (running and todo). Do not add to this
        /// list but use enqueue
        /// </summary>
        public IEventedList<IActivity> Activities
        {
            get { return activities; }
        }

        private const int maxRunningTaskCount = 1;

        public event EventHandler IsRunningChanged;
        public event EventHandler ActivitiesCollectionChanged;


        private void StartTaskIfPossible()
        {
            //we can run if we are not busy and have something todo ;)
            while ((runningTasks.Count < maxRunningTaskCount) && (todoTasks.Count > 0))
            {
                //'pop' the first task (FIFO)
                var taskToRun = todoTasks[0];
                todoTasks.RemoveAt(0);

                runningTasks.Add(taskToRun);
                Debug.WriteLine(string.Format("Run activity {0}", (taskToRun.Activity.Name)));
                taskToRun.Run();
            }
        }

        void TaskTaskCompleted(object sender, EventArgs e)
        {
            var task = (ASynchTask) sender;
            Debug.WriteLine(string.Format("Finished activity {0}", task.Activity.Name));
            runningTasks.Remove(task);
            OnTaskCompleted(task);
            StartTaskIfPossible();
            
            if (runningTasks.Count == 0)
            {
                OnIsRunningChanged();
            }
            activities.Remove(task.Activity);
        }

        private void OnIsRunningChanged()
        {
            //TODO: get some logic to determine whether it really changed. (P.Changed?)
            if (IsRunningChanged != null)
            {
                IsRunningChanged(this, EventArgs.Empty);
            }
        }

        private void OnTaskCompleted(ASynchTask sender)
        {
            if (!sender.TaskCompletedSuccesFully)
            {
                if (String.IsNullOrEmpty(sender.Activity.Name))
                {
                    log.ErrorFormat("An error occured while running a background activity.");    
                }
                else
                {
                    log.ErrorFormat("An error occured while running activity {0}",sender.Activity.Name);    
                }
                
            }
                
            if (ActivityCompleted != null)
            {
                ActivityCompleted(this, new ActivityEventArgs(sender.Activity));
            }
        }



        #region IActivityRunner Members


        public bool IsRunning
        {
            get
            {
                return runningTasks.Count > 0;
            }
        }

        public bool IsRunningActivity(IActivity activity)
        {
            if (activity == null)
                return false;
            return runningTasks.Any(task => task.Activity == activity);
        }

        public void Enqueue(IActivity activity, Action<IActivity> todo)
        {
            var task = new ASynchTask(activity, todo);
            task.TaskCompleted += TaskTaskCompleted;
            
            todoTasks.Add(task);
            activities.Add(activity);
            Debug.WriteLine(string.Format("Enqueued activity {0}", activity.Name));
            StartTaskIfPossible();
            //TODO: it might already be running so running would not be changed.
            //fix and review
            OnIsRunningChanged();
        }

        public void Cancel(IActivity activity)
        {
            //find the task corresponding to the given activiy 
            var task = runningTasks.FirstOrDefault(t => t.Activity == activity);
            if (task != null)
            {
                task.Cancel();
                return;
            }
            //or remove it from todo
            task = todoTasks.FirstOrDefault(t => t.Activity == activity);
            if (task != null)
            {
                todoTasks.Remove(task);
            }
            OnIsRunningChanged();
        }

        public void CancelAll()
        {
            foreach (var task in runningTasks)
            {
                task.Cancel();
            }
            //empty the todo on a cancel
            todoTasks.Clear();
            OnIsRunningChanged();
        }

        public event EventHandler<ActivityEventArgs> ActivityCompleted;

        #endregion
    }
}