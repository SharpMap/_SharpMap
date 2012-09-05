using System;
using System.Collections.Generic;
using System.ComponentModel;
using DelftTools.Utils.Collections;
using DelftTools.Utils.UndoRedo.Mementos;
using log4net;

namespace DelftTools.Utils.UndoRedo
{
    public class EventBasedChangeTracker : IUndoRedoChangeTracker
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(EventBasedChangeTracker));

        private object observable;

        private readonly IDictionary<PropertyChangeKey, NotifyPropertyChangeMemento> propertyChangeMementos = new Dictionary<PropertyChangeKey, NotifyPropertyChangeMemento>();

        private readonly IDictionary<object, NotifyCollectionChangeMemento> collectionChangeMementos = new Dictionary<object, NotifyCollectionChangeMemento>();

        private Stack<CompoundMemento> editableObjectMementos = new Stack<CompoundMemento>();
        private CompoundMemento currentEditableObjectMemento;

        struct PropertyChangeKey
        {
            public object Sender;
            public string PropertyName;
        }

        public EventBasedChangeTracker(Action<IMemento> addNewMementoCallback, object observable)
        {
            TrackChanges = true;
            AddNewMementoCallback = addNewMementoCallback;
            Observable = observable;

            ExcludedTypes = new List<Type>();
        }

        public object Observable
        {
            get { return observable; }
            set
            {
                if(observable != null)
                {
                    Unsubscribe(observable);
                }
                               
                observable = value;

                Subscribe(value);
            }
        }

        public bool TrackChanges { get; set; }

        /// <summary>
        /// Subscribes to object removed from collections due to undo, 
        /// however we still have to track changes in them.
        /// </summary>
        /// <param name="memento"></param>
        public void OnBeforeUndo(IMemento memento)
        {
            var collectionChangeMemento = memento as NotifyCollectionChangeMemento;
            if (collectionChangeMemento != null)
            {
                switch (collectionChangeMemento.Action)
                {
                    case NotifyCollectionChangeAction.Add:
                        Subscribe(collectionChangeMemento.NewValue);
                        break;
                    case NotifyCollectionChangeAction.Remove:
                        Unsubscribe(collectionChangeMemento.OldValue);
                        break;
                    case NotifyCollectionChangeAction.Replace:
                        Subscribe(collectionChangeMemento.OldValue);
                        break;
                    default:
                        break;
                }
            }
            
            var propertyChangeMemento = memento as NotifyPropertyChangeMemento;
            if (propertyChangeMemento != null)
            {
                Subscribe(propertyChangeMemento.Instance);
            }
        }

        public void OnBeforeRedo(IMemento memento)
        {
            var collectionChangeMemento = memento as NotifyCollectionChangeMemento;
            if (collectionChangeMemento != null)
            {
                switch (collectionChangeMemento.Action)
                {
                    case NotifyCollectionChangeAction.Add:
                        Unsubscribe(collectionChangeMemento.NewValue);
                        break;
                    case NotifyCollectionChangeAction.Remove:
                        Subscribe(collectionChangeMemento.OldValue);
                        break;
                    case NotifyCollectionChangeAction.Replace:
                        Unsubscribe(collectionChangeMemento.OldValue);
                        break;
                    default:
                        break;
                }
            }
            var propertyChangeMemento = memento as NotifyPropertyChangeMemento;
            if (propertyChangeMemento != null)
            {
                Unsubscribe(propertyChangeMemento.Instance);
            }
        }

        public void OnBeforeRemove(IMemento memento)
        {
            var collectionChangeMemento = memento as NotifyCollectionChangeMemento;
            if (collectionChangeMemento != null)
            {
                switch (collectionChangeMemento.Action)
                {
                    case NotifyCollectionChangeAction.Add:
                        Unsubscribe(collectionChangeMemento.NewValue);
                        break;
                    case NotifyCollectionChangeAction.Replace:
                        Unsubscribe(collectionChangeMemento.OldValue);
                        break;
                    default:
                        break;
                }
            }
            var propertyChangeMemento = memento as NotifyPropertyChangeMemento;
            if (propertyChangeMemento != null)
            {
                Unsubscribe(propertyChangeMemento.Instance);
            }
        }

        public Action<IMemento> AddNewMementoCallback { get; set; }

        public IList<Type> ExcludedTypes { get; private set; }

        private void SubscribeToOldValues(IMemento memento)
        {
/*
            if(memento is NotifyPropertyChangeMemento)
            {
                var notifyPropertyChangeMemento = memento as NotifyPropertyChangeMemento;
                if (notifyPropertyChangeMemento.OldValue != null)
                {
                    Subscribe(notifyPropertyChangeMemento.OldValue);
                }
                if (notifyPropertyChangeMemento.NewValue != null) // unsubscribe from added values
                {
                    Unsubscribe(notifyPropertyChangeMemento.NewValue);
                }
            }
*/
            if (memento is NotifyCollectionChangeMemento)
            {
                var notifyCollectionChangeMemento = memento as NotifyCollectionChangeMemento;
                if (notifyCollectionChangeMemento.OldValue != null)
                {
                    Subscribe(notifyCollectionChangeMemento.OldValue);
                }
                if (notifyCollectionChangeMemento.NewValue != null) // unsubscribe from added values
                {
                    Unsubscribe(notifyCollectionChangeMemento.NewValue);
                }
            }
        }

        private void Subscribe(object item)
        {
            if(item == null)
            {
                return;
            }

            var notifyPropertyChange = item as INotifyPropertyChange;
            if (notifyPropertyChange != null)
            {
                notifyPropertyChange.PropertyChanging += notifyPropertyChanged_PropertyChanging;
                notifyPropertyChange.PropertyChanged += notifyPropertyChanged_PropertyChanged;
            }

            var notifyCollectionChange = item as INotifyCollectionChange;
            if (notifyCollectionChange != null)
            {
                notifyCollectionChange.CollectionChanging += notifyCollectionChanged_CollectionChanging;
                notifyCollectionChange.CollectionChanged += notifyCollectionChanged_CollectionChanged;
            }
        }

        private void Unsubscribe(object item)
        {
            var notifyPropertyChange = item as INotifyPropertyChange;
            if (notifyPropertyChange != null)
            {
                notifyPropertyChange.PropertyChanging -= notifyPropertyChanged_PropertyChanging;
                notifyPropertyChange.PropertyChanged -= notifyPropertyChanged_PropertyChanged;
            }

            var notifyCollectionChange = item as INotifyCollectionChange;
            if (notifyCollectionChange != null)
            {
                notifyCollectionChange.CollectionChanging -= notifyCollectionChanged_CollectionChanging;
                notifyCollectionChange.CollectionChanged -= notifyCollectionChanged_CollectionChanged;
            }
        }

        private void notifyPropertyChanged_PropertyChanging(object sender, PropertyChangingEventArgs e)
        {
            if(!TrackChanges)
            {
                return;
            }

            if (ExcludedTypes.Contains(sender.GetType()))
            {
                return;
            }

            var memento = new NotifyPropertyChangeMemento();
            memento.RememberOldValue(sender, e);

            var key = new PropertyChangeKey{Sender = sender, PropertyName = e.PropertyName};

            if(propertyChangeMementos.ContainsKey(key))
            {
                throw new InvalidOperationException(string.Format("Property {0} of {1} is already being changed",
                                                                  e.PropertyName, sender));
            }
            propertyChangeMementos[key] = memento;
        }

        private void notifyPropertyChanged_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (!TrackChanges)
            {
                return;
            }

            if(ExcludedTypes.Contains(sender.GetType()))
            {
                return;
            }

            NotifyPropertyChangeMemento memento;
            var key = new PropertyChangeKey { Sender = sender, PropertyName = e.PropertyName };
            propertyChangeMementos.TryGetValue(key, out memento);

            if(memento == null)
            {
                throw new NotSupportedException("PropertyChanged received without PropertyChanging");
            }

            memento.RememberNewValue(sender, e);
            propertyChangeMementos.Remove(key);

            // check if we need to add memento to current compound memento or to undoRedoManager
            if(e.PropertyName == "IsEditing")
            {
                var editable = (IEditableObject)memento.Instance;

                if (editable.IsEditing) // BeginEdit()
                {
                    if (currentEditableObjectMemento != null)
                    {
                        editableObjectMementos.Push(currentEditableObjectMemento);
                    }

                    currentEditableObjectMemento = new CompoundMemento { Name = editable.CurrentEditAction.Name };
                    AddMemento(memento);
                }
                else // EndEdit()
                {
                    if(currentEditableObjectMemento == null)
                    {
                        throw new InvalidOperationException("Unexpected end edit call before begin edit");
                    }

                    AddMemento(memento);

                    if (editableObjectMementos.Count == 0)
                    {
                        log.DebugFormat("saving undo for edit action {0}", currentEditableObjectMemento.Name);
                        AddNewMementoCallback(currentEditableObjectMemento);

                        currentEditableObjectMemento = null;
                    }
                    else 
                    {
                        // pull previous editable object memento and continue with it as current compound memento
                        var previousEditableObjectMemento = editableObjectMementos.Pop();
                        previousEditableObjectMemento.ChildMementos.Add(currentEditableObjectMemento);
                        currentEditableObjectMemento = previousEditableObjectMemento;
                    }
                }
            }
            else
            {
                if (currentEditableObjectMemento != null)
                {
                    log.DebugFormat("adding property cange to edit action {0}.{1}: {2} -> {3}", sender.GetType().Name,
                                    e.PropertyName, memento.OldValue ?? "null", memento.NewValue ?? "null");
                }
                else
                {
                    log.DebugFormat("saving undo for property cange {0}.{1}: {2} -> {3}", sender.GetType().Name,
                                    e.PropertyName, memento.OldValue ?? "null", memento.NewValue ?? "null");
                }

                AddMemento(memento);
            }
        }

        void notifyCollectionChanged_CollectionChanging(object sender, NotifyCollectionChangingEventArgs e)
        {
            if (!TrackChanges)
            {
                return;
            }

            if (ExcludedTypes.Contains(sender.GetType()))
            {
                return;
            }
            
            if (e.Cancel)
            {
                log.DebugFormat("CollectionChanging event was cancelled, skipping undo");
            }

            if (collectionChangeMementos.ContainsKey(sender))
            {
                throw new NotSupportedException("Unsuported operation, received two CollectionChanging events in a row");
            }

            if (e.Item != null) // unsubscribe from added values
            {
                Unsubscribe(e.Item);
            }

            var memento = new NotifyCollectionChangeMemento();
            memento.RememberOldValues(sender, e);

            collectionChangeMementos[sender] = memento;
        }

        void notifyCollectionChanged_CollectionChanged(object sender, NotifyCollectionChangingEventArgs e)
        {
            if (!TrackChanges)
            {
                return;
            }

            if (ExcludedTypes.Contains(sender.GetType()))
            {
                return;
            }

            NotifyCollectionChangeMemento memento;
            collectionChangeMementos.TryGetValue(sender, out memento);

            if (memento == null)
            {
                throw new NotSupportedException("CollectionChanged received without CollectionChanging");
            }

            memento.RememberNewValues(sender, e);
            collectionChangeMementos.Remove(sender);

            if (currentEditableObjectMemento != null)
            {
                if (e.Action == NotifyCollectionChangeAction.Replace)
                {
                    log.DebugFormat("adding collection change to edit action {0}: {1}:{2} -> {3}:{4}", memento.Action,
                                    memento.OldValue ?? "null", memento.OldIndex, memento.NewValue ?? "null",
                                    memento.NewIndex);
                }
                else
                {
                    log.DebugFormat("adding collection change to edit action {0}: {1}:{2}", memento.Action,
                                    memento.NewValue ?? "null", memento.NewIndex);
                }
            }
            else
            {
                if (e.Action == NotifyCollectionChangeAction.Replace)
                {
                    log.DebugFormat("saving undo for collection change {0}: {1}:{2} -> {3}:{4}", memento.Action,
                                    memento.OldValue ?? "null", memento.OldIndex, memento.NewValue ?? "null",
                                    memento.NewIndex);
                }
                else
                {
                    log.DebugFormat("saving undo for collection change {0}: {1}:{2}", memento.Action,
                                    memento.NewValue ?? "null", memento.NewIndex);
                }
            }

            AddMemento(memento);
        }

        private void AddMemento(IMemento memento)
        {
            SubscribeToOldValues(memento);

            if (currentEditableObjectMemento != null)
            {
                currentEditableObjectMemento.ChildMementos.Add(memento); // remember as a part of current compound memento
            }
            else
            {
                AddNewMementoCallback(memento); // remember as a single memento
            }
        }
    }
}