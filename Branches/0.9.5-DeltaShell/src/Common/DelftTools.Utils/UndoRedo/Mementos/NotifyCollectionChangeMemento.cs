using System;
using System.Collections;
using System.Linq;
using DelftTools.Utils.Collections;

namespace DelftTools.Utils.UndoRedo.Mementos
{
    public class NotifyCollectionChangeMemento : IMemento
    {
        public IList List;

        public NotifyCollectionChangeAction Action;
        
        public object OldValue;
        public int OldIndex;

        public object NewValue;
        public int NewIndex;

        public IMemento Restore()
        {
            var memento = new NotifyCollectionChangeMemento
                              {
                                  List = List,
                                  Action = GetInvertedAction(Action),
                                  OldIndex = NewIndex,
                                  OldValue = NewValue,
                                  NewIndex = OldIndex,
                                  NewValue = OldValue
                              };

            switch(Action)
            {
                case NotifyCollectionChangeAction.Add:
                    List.RemoveAt(NewIndex);
                    break;
                case NotifyCollectionChangeAction.Remove:
                    List.Insert(OldIndex, OldValue);
                    break;
                case NotifyCollectionChangeAction.Replace:
                    throw new NotImplementedException();
            }

            return memento;
        }

        private NotifyCollectionChangeAction GetInvertedAction(NotifyCollectionChangeAction action)
        {
            switch (action)
            {
                case NotifyCollectionChangeAction.Add:
                    return NotifyCollectionChangeAction.Remove;
                case NotifyCollectionChangeAction.Remove:
                    return NotifyCollectionChangeAction.Add;
                case NotifyCollectionChangeAction.Replace:
                    return NotifyCollectionChangeAction.Replace;
            }
            
            throw new NotSupportedException("Unknown action");
        }

        public void RememberOldValues(object sender, NotifyCollectionChangingEventArgs e)
        {
            if (List != null)
            {
                throw new InvalidOperationException("Old value is alredy initialized");
            }

            List = (IList)sender;

            Action = e.Action;

            switch(e.Action)
            {
                case NotifyCollectionChangeAction.Add:
                    OldIndex = -1;
                    break;
                case NotifyCollectionChangeAction.Remove:
                    OldIndex = e.Index;
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
                    NewIndex = e.Index;
                    NewValue = e.Item;
                    break;
                case NotifyCollectionChangeAction.Remove:
                    NewIndex = -1;
                    break;
                case NotifyCollectionChangeAction.Replace:
                    OldValue = List[e.Index]; // remember value being replaced
                    break;
            }
        }

        /// <summary>
        /// Converts memento to user-readable string.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
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

            return string.Format("{0} {1} ({2}[])", 
                Action, 
                Action == NotifyCollectionChangeAction.Remove ? OldValue : NewValue,
                valueTypeName
            );
        }
    }
}