using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using DelftTools.Utils.Aop;
using log4net;

namespace DelftTools.Utils.UndoRedo.Mementos
{
    public abstract class CompoundMemento : IMemento
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(CompoundMemento)); 
        
        private readonly IList<IMemento> childMementos = new List<IMemento>();
        
        private IMemento CurrentMemento { get; set; }
        
        public IList<IMemento> ChildMementos { get { return childMementos; } }

        public static bool CaptureStackTrace { get; set; }

        protected CompoundMemento()
        {
            if (CaptureStackTrace)
            {
                var stackTrace = new StackTrace(true);
                StackTrace = stackTrace.ToString();
            }
        }

        public virtual void Restore()
        {
            foreach (var childMemento in ChildMementos.Reverse())
            {
                CurrentMemento = childMemento;
                childMemento.Restore();
            }
            CurrentMemento = null;
        }

        /// <summary>
        /// Returns current memento taking child compound memento's into account.
        /// </summary>
        /// <returns></returns>
        public IMemento CurrentSimpleMemento
        {
            get 
            {
                if (CurrentMemento != null)
                {
                    return CurrentMemento.CurrentSimpleMemento;
                }
                return this;
            }
        }

        public string StackTrace { get; set; }

        private Action<object, bool> listener;
        private void RestoreBeforeEventCall()
        {
            EditActionAttribute.BeforeEventCall = listener;
            listener = null;
        }

        protected void SuppressNextBeforeEventCall()
        {
            if (listener != null)
            {
                throw new InvalidOperationException("Suppressing before restoring");
            }
            listener = EditActionAttribute.BeforeEventCall;
            EditActionAttribute.BeforeEventCall = (s, v) => RestoreBeforeEventCall();
        }

        protected static void FireBeforeEventCall(object sender, bool isPropertyChange)
        {
            EditActionAttribute.FireBeforeEventCall(sender, isPropertyChange);
        }
    }
}