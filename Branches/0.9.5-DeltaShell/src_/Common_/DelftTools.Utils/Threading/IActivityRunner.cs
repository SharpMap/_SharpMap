using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DelftTools.Utils.Collections;
using DelftTools.Utils.Collections.Generic;
using DelftTools.Utils.Workflow;

namespace DelftTools.Utils.Threading
{
    public interface IActivityRunner
    {
        /// <summary>
        /// All activities (todo and running)
        /// </summary>
        IEventedList<IActivity> Activities { get; }

        /// <summary>
        /// Determines whether some activity is running
        /// </summary>
        bool IsRunning { get; }

        /// <summary>
        /// Call to find out if a specific activity is being run
        /// </summary>
        /// <param name="activity"></param>
        /// <returns></returns>
        bool IsRunningActivity(IActivity activity);

        /// <summary>
        /// Adds the activity to the todo and runs it when possible
        /// </summary>
        /// <param name="activity">Activity to process</param>
        /// <param name="todo">Action to do on the activity</param>
        void Enqueue(IActivity activity, Action<IActivity> todo);
        
        /// <summary>
        /// Cancels the specified activity
        /// </summary>
        /// <param name="activity"></param>
        void Cancel(IActivity activity);
        
        /// <summary>
        /// Stops all activities and empties the TODO-list
        /// </summary>
        void CancelAll();
            
        /// <summary>
        /// Fired when an activity completes
        /// </summary>
        event EventHandler<ActivityEventArgs> ActivityCompleted;
        
        /// <summary>
        /// Occurs when the queuestate changes
        /// </summary>
        event EventHandler IsRunningChanged;

        /// <summary>
        /// Occurs when an activity is enqueued or finished
        /// </summary>
        event EventHandler ActivitiesCollectionChanged;
    }
}
