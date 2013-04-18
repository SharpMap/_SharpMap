using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using DelftTools.Utils.Collections;
using DelftTools.Utils.Collections.Generic;
using DelftTools.Utils.Workflow;
using log4net;

namespace DelftTools.Utils.Threading
{
    using System.Runtime.InteropServices;

    /// <summary>
    /// Class fires activities asynch and generates state changed
    /// </summary>
    //TODO: increase coverage and simplity this class. It is on the verge of unmanagable
    public class ActivityRunner : IActivityRunner
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(ActivityRunner));

        private readonly IList<ASynchTask> runningTasks = new List<ASynchTask>();
        private readonly IList<ASynchTask> todoTasks = new List<ASynchTask>();
        private readonly IEventedList<IActivity> activities;

        /// <summary>
        /// Provides an evented summary of the current activities (running and todo). 
        /// DO NOT ADD TO THIS LIST useEnqueue instead
        /// </summary>
        public IEventedList<IActivity> Activities
        {
            get { return activities; }
        }


        public ActivityRunner()
        {
            activities = new EventedList<IActivity>();
            activities.CollectionChanged += HandleActivitiesCollectionChanged;
        }

        void HandleActivitiesCollectionChanged(object sender, NotifyCollectionChangingEventArgs e)
        {
            //only local changes...get this in the EventedList...
            if (sender != activities)
                return;

            if (e.Action == NotifyCollectionChangeAction.Add)
            {
                ((IActivity)e.Item).StatusChanged += HandleActivityStatusChanged;
            }
            else if (e.Action == NotifyCollectionChangeAction.Remove)
            {
                ((IActivity)e.Item).StatusChanged -= HandleActivityStatusChanged;
            }
        }

        void HandleActivityStatusChanged(object sender, ActivityStatusChangedEventArgs e)
        {
            if (ActivityStatusChanged != null)
            {
                //bubble the activity status change..
                ActivityStatusChanged(sender, e);
            }

            if (e.NewStatus == ActivityStatus.Cancelled)
            {
                //done cancelling,
                activities.Remove(sender as IActivity);
            }
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

            CleanUp(task);
        }

        private void CleanUp(ASynchTask task)
        {
            task.TaskCompleted -= TaskTaskCompleted;
            
            task.Dispose();
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
                    log.Error("An error occured while running a background activity: ", sender.TaskException);
                }
                else
                {
                    log.Error(String.Format("An error occured while running activity {0}: ", sender.Activity.Name), sender.TaskException);    
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
            get { return runningTasks.Count > 0; }
        }

        public bool IsRunningActivity(IActivity activity)
        {
            if (activity == null)
                return false;
            return runningTasks.Any(task => task.Activity == activity);
        }

        public void Enqueue(IActivity activity)
        {
            var task = new ASynchTask(activity, this.RunActivity);
            task.TaskCompleted += TaskTaskCompleted;
            
            todoTasks.Add(task);
            activities.Add(activity);
            Debug.WriteLine(string.Format("Enqueued activity {0}", activity.Name));
            StartTaskIfPossible();
            //TODO: it might already be running so running would not be changed.
            //fix and review
            OnIsRunningChanged();
        }

        private void RunActivity(IActivity activity)
        {
            try
            {
                if (activity.Status != ActivityStatus.Initialized)
                {
                    activity.Initialize();
                }

                if (activity.Status != ActivityStatus.Initialized)
                {
                    throw new InvalidOperationException("Can't initialize activity: " + activity);
                }

                while (activity.Status != ActivityStatus.Finished)
                {
                    activity.Execute();

                    if (activity.Status != ActivityStatus.Executed)
                    {
                        return; // finished
                    }
                }
            }
            catch (Exception e)
            {
                var message = e.Message;

                if (e is SEHException)
                {
                    message = String.Format("Exception during call '{0}' in external component. Activity failed.", e.TargetSite != null ? e.TargetSite.Name : "<unknown>");
                }

                log.Error(message, e);
                throw;
            }

        }

        public void Cancel(IActivity activity)
        {
            var task = runningTasks.FirstOrDefault(t => t.Activity == activity);

            if (task != null)
            {
                //TODO: let the task cancel and complete.cleanup should be in TaskTaskCompleted
                task.Cancel();

                runningTasks.Remove(task);
                
                CleanUp(task);

                return;
            }

            //or remove it from todo
            task = todoTasks.FirstOrDefault(t => t.Activity == activity);
            if (task != null)
            {
                todoTasks.Remove(task);
                CleanUp(task);
            }

            OnIsRunningChanged();
        }
        //TODO: make cancelAll use cancel for each activity.
        public void CancelAll()
        {
            foreach (var task in runningTasks)
            {
                task.Cancel();
                CleanUp(task);
            }

            //empty the todo on a cancel
            foreach (var task in todoTasks)
            {
                CleanUp(task);
            }

            todoTasks.Clear();
            runningTasks.Clear();

            OnIsRunningChanged();
        }

        public event EventHandler<ActivityEventArgs> ActivityCompleted;

        public event EventHandler<ActivityStatusChangedEventArgs> ActivityStatusChanged;

        #endregion
    }
}