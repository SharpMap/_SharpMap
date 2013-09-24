using System;
using System.Collections;
using System.Linq;
using DelftTools.Utils.Collections;

using log4net;

namespace DelftTools.Utils.UndoRedo.Mementos
{
    public class NotifyCollectionChangeMemento : CompoundMemento
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(NotifyCollectionChangeMemento));

        public IList List;

        public NotifyCollectionChangeAction Action;
        
        public object OldValue;
        public object NewValue;
        public int Index;
        public bool Filled;

        public override void Restore()
        {
            FireBeforeEventCall(List, false);

            base.Restore();

            SuppressNextBeforeEventCall(); 
            switch (Action)
            {
                case NotifyCollectionChangeAction.Add:
                    List.RemoveAt(Index);
                    break;
                case NotifyCollectionChangeAction.Remove:
                    List.Insert(Index, OldValue);
                    break;
                case NotifyCollectionChangeAction.Replace:
                    List[Index] = OldValue;
                    break;
            }
        }
        
        public void RememberOldValues(object sender, NotifyCollectionChangingEventArgs e)
        {
            if (List != null)
            {
                throw new InvalidOperationException("Old value is already initialized");
            }

            List = (IList)sender;

            Action = e.Action;

            Index = e.Index;

            switch(e.Action)
            {
                case NotifyCollectionChangeAction.Add:
                    break;
                case NotifyCollectionChangeAction.Remove:
                    OldValue = e.Item;
                    break;
                case NotifyCollectionChangeAction.Replace:
                    OldValue = List[e.Index]; // remember value being replaced
                    break;
            }
        }

        public void RememberNewValues(object list, NotifyCollectionChangingEventArgs e)
        {
            if (this.List != list)
            {
                throw new InvalidOperationException("Instance of the new value is not the same as for the old value");
            }

            if (NewValue != null)
            {
                throw new InvalidOperationException("New value is already initialized");
            }

            switch (e.Action)
            {
                case NotifyCollectionChangeAction.Add:
                    NewValue = e.Item;
                    break;
                case NotifyCollectionChangeAction.Remove:
                    break;
                case NotifyCollectionChangeAction.Replace:
                    if(e.Index != Index)
                    {
                        throw new NotImplementedException("Replace of element in collection with different index is not implemented yet.");
                    }

                    NewValue = e.Item;
                    break;
            }

            Filled = true;
        }

        /// <summary>
        /// Converts memento to user-readable string.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            if(log.IsDebugEnabled)
            {
                var valueTypeName = (OldValue ?? NewValue).GetType().Name;

                var listElementType = List.GetType().GetGenericArguments().FirstOrDefault();
                if (listElementType != null)
                {
                    if (valueTypeName != listElementType.Name)
                    {
                        valueTypeName = string.Format("{0}:{1}", listElementType.Name, valueTypeName);
                    }
                }

                return string.Format(
                    "{0} {1} ({2}[])",
                    Action,
                    Action == NotifyCollectionChangeAction.Remove ? OldValue : NewValue,
                    valueTypeName);
            }
            else
            {
                return string.Format(
                    "{0} {1}",
                    Action,
                    Action == NotifyCollectionChangeAction.Remove ? OldValue : NewValue);
            }
        }
    }
}