using System;
using DelftTools.Utils.Workflow;

namespace DelftTools.Utils.Threading
{
    public class ActivityEventArgs:EventArgs
    {
        public IActivity Activity { get;private set; }
        public ActivityEventArgs(IActivity activity)
        {
            Activity = activity;
        }
    }
}