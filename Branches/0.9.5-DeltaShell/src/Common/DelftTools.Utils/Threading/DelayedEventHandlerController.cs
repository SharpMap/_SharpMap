using System;

namespace DelftTools.Utils.Threading
{
    public class DelayedEventHandlerController
    {
        //default to true
        private static bool fireEvents = true;

        public static bool FireEvents
        {
            get { return fireEvents; }
            set
            {
                //don't sent unnecessary changes
                if (fireEvents == value)
                {
                    return;
                }
                fireEvents = value;
                OnFireEventsChanged();
            }
        }

        private static void OnFireEventsChanged()
        {
            if (FireEventsChanged != null)
            {
                //no sender since we are in a static context here
                FireEventsChanged(null, EventArgs.Empty);
            }
        }

        public static event EventHandler FireEventsChanged;
        
    }
}