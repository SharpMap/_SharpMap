using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using DelftTools.Utils.Collections;
using DelftTools.Utils.IO;
using DelftTools.Utils.Reflection;
using DelftTools.Utils.UndoRedo.DataTable;
using DelftTools.Utils.UndoRedo.Mementos;
using PostSharp.Aspects;
using log4net;
using IEditableObject = DelftTools.Utils.Editing.IEditableObject;

namespace DelftTools.Utils.UndoRedo
{
    using System.Diagnostics;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using Aop;

    public class EventBasedChangeTracker : IUndoRedoChangeTracker
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(EventBasedChangeTracker));

        private object observable;

        /// <summary>
        /// Sometimes events come double to disconnected and connected objects. Remeber memento when this happen.
        /// </summary>
        private readonly IDictionary<PropertyChangeKey, NotifyPropertyChangeMemento> propertyChangeMementosDisconnected = new Dictionary<PropertyChangeKey, NotifyPropertyChangeMemento>();
        private readonly IDictionary<PropertyChangeKey, NotifyPropertyChangeMemento> propertyChangeMementos = new Dictionary<PropertyChangeKey, NotifyPropertyChangeMemento>();
        private readonly IDictionary<CollectionChangeKey, NotifyCollectionChangeMemento> collectionChangeMementos = new Dictionary<CollectionChangeKey, NotifyCollectionChangeMemento>();
        
        private readonly List<string> virtualPropertyCalls = new List<string>();

        private readonly HashSet<int> disconnectedObjectHashes = new HashSet<int>();
        
        private readonly Stack<IMemento> mementoStack = new Stack<IMemento>();

        private IMemento restoringMemento;

        private bool handlingDisconnected;

        private object pendingIncrementEventCallCountSender;

        private class PropertyChangeKey
        {
            public PropertyChangeKey(object sender, string propertyName)
            {
                this.sender = sender;
                this.propertyName = propertyName;
            }

            private readonly object sender;
            private readonly string propertyName;
            
            /// <summary>
            /// Overwriten to make sure the hashcode of this key is not affected by any changes in the hashcode of the Sender 
            /// object (over which we have no control). A dictionary caches these keys, so if the hashcode of a key changes 
            /// later, the key is no longer found. We use RuntimeHelpers.GetHashCode instead of normal GetHashCode to get a 
            /// reference-based hashcode, so regardless of changes in Sender, the hashcode will always remain the same for a 
            /// specific instance.
            /// </summary>
            /// <returns></returns>
            public override int GetHashCode()
            {
                unchecked
                {
                    return RuntimeHelpers.GetHashCode(sender) + propertyName.GetHashCode();
                }
            }

            public override bool Equals(object obj)
            {
                if (obj is PropertyChangeKey)
                    return GetHashCode() == obj.GetHashCode();
                return false;
            }
        }

        private class CollectionChangeKey
        {
            public CollectionChangeKey(object sender, object item, NotifyCollectionChangeAction action)
            {
                this.sender = sender;
                this.item = item;
                this.action = action;
            }

            private readonly object sender;
            private readonly object item;
            private readonly NotifyCollectionChangeAction action;

            protected bool Equals(CollectionChangeKey other)
            {
                return sender.Equals(other.sender) && item.Equals(other.item) && action == other.action;
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((CollectionChangeKey) obj);
            }

            /// <summary>
            /// Overwriten to make sure the hashcode of this key is not affected by any changes in the hashcode of the Sender 
            /// object (over which we have no control). A dictionary caches these keys, so if the hashcode of a key changes 
            /// later, the key is no longer found. We use RuntimeHelpers.GetHashCode instead of normal GetHashCode to get a 
            /// reference-based hashcode, so regardless of changes in Sender, the hashcode will always remain the same for a 
            /// specific instance.
            /// </summary>
            /// <returns></returns>
            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = RuntimeHelpers.GetHashCode(sender);
                    hashCode = (hashCode * 397) ^ RuntimeHelpers.GetHashCode(item);
                    hashCode = (hashCode*397) ^ (int) action;
                    return hashCode;
                }
            }

            public static bool operator ==(CollectionChangeKey left, CollectionChangeKey right)
            {
                return Equals(left, right);
            }

            public static bool operator !=(CollectionChangeKey left, CollectionChangeKey right)
            {
                return !Equals(left, right);
            }
        }

        public EventBasedChangeTracker(Action<IMemento> addNewMementoCallback, object observable)
        {
            if (EditActionAttribute.BeforeEventCall != null && instance != null)
            {
                var message = "Did you forget to use using(var gui = new DeltaShellGui()) in the test? EditActionAttribute.BeforeEventCall is already set, most probably last UndoRedoManager was not disposed correctly, last call stack trace: " + lastInitializationStackTrace;
                instance.Dispose();
                throw new InvalidOperationException(message);
            }

            lastInitializationStackTrace = new StackTrace(true).ToString(); // for tracability purposes

            TrackChanges = true;
            AddNewMementoCallback = addNewMementoCallback;
            Observable = observable;
            ExcludedTypes = new List<Type>();

            EditActionAttribute.BeforeEdit += EditActionAttributeOnBeforeEdit;
            EditActionAttribute.AfterEdit += EditActionAttributeOnAfterEdit;

            EditActionAttribute.BeforeEventCall = BeforeEventCall;
            EditActionAttribute.AfterEventCall = AfterEventCall;

            threadId = Thread.CurrentThread.ManagedThreadId;

            instance = this;
        }

        private static EventBasedChangeTracker instance;
        private static string lastInitializationStackTrace;

        private readonly int threadId;

        private int eventCascadeCallLevel;

        private int editActionCallCount;

        private bool isDisposed;
        public void Dispose()
        {
            if (isDisposed)
            {
                return;
            }
            instance = null;
            isDisposed = true;
            Observable = null;
            TrackChanges = false;
            pendingIncrementEventCallCountSender = null;

            EditActionAttribute.BeforeEdit -= EditActionAttributeOnBeforeEdit;
            EditActionAttribute.AfterEdit -= EditActionAttributeOnAfterEdit;

            //do we still 'own' the BeforeEventCall action
            if (EditActionAttribute.BeforeEventCall == BeforeEventCall) 
            {
                EditActionAttribute.BeforeEventCall = null;
                EditActionAttribute.AfterEventCall = null;
            }

            lastInitializationStackTrace = null;
        }

        ~EventBasedChangeTracker()
        {
            Dispose();
        }

        private void EditActionAttributeOnBeforeEdit(MethodInterceptionArgs e)
        {
            if(!TrackChanges || observable == null)
            {
                return;
            }

            if (Thread.CurrentThread.ManagedThreadId == threadId)
            {
                editActionCallCount++;
                expectedEventCascadeCallLevel = eventCascadeCallLevel + 1;
            }
        }

        private void EditActionAttributeOnAfterEdit(MethodInterceptionArgs e)
        {
            if (!TrackChanges || observable == null)
            {
                return;
            }

            if (Thread.CurrentThread.ManagedThreadId == threadId)
            {
                editActionCallCount--;
            }
        }
        
        private void BeforeEventCall(object sender, bool isPropertyChange)
        {
            if (TrackChanges && Thread.CurrentThread.ManagedThreadId == threadId)
            {
                var memento = isPropertyChange
                                  ? (IMemento) new NotifyPropertyChangeMemento()
                                  : (IMemento) new NotifyCollectionChangeMemento();
                mementoStack.Push(memento);

                eventCascadeCallLevel++;

                if (pendingIncrementEventCallCountSender != null)
                {
                    // external event!                               
                    expectedEventCascadeCallLevel = eventCascadeCallLevel;
                }

                pendingIncrementEventCallCountSender = sender;
            }
        }

        private void AfterEventCall(object sender, bool isPropertyChange, bool isCancelled)
        {
            if (TrackChanges && Thread.CurrentThread.ManagedThreadId == threadId)
            {
                ThrowIfMementoStackEmpty();

                if (HasMementoEnded(mementoStack.Peek()))
                {
                    var lastMemento = mementoStack.Pop();
                    ProcessMemento(lastMemento, isCancelled);
                }
                eventCascadeCallLevel--;
            }

            if(!TrackChanges)
            {
                pendingIncrementEventCallCountSender = null; // clean-up, required because of static event handlers
            }
        }

        private void ThrowIfMementoStackEmpty()
        {
            if (mementoStack.Count == 0)
            {
                throw new InvalidOperationException("Memento stack empty: check for side-effects during memento restore, subscriber stacktrace: " + lastInitializationStackTrace);
            }
        }

        private void ProcessMemento(IMemento memento, bool isCancelled)
        {
            if (!isCancelled && IsMementoFilled(memento))
            {
                if (mementoStack.Count == 0) //end of nesting
                {
                    ThrowIfInternalStateInconsistent();
                    AddNewMementoCallback(memento);
                }
                else
                {
                    mementoStack.Peek().ChildMementos.Add(memento);
                }
            }
            else //not filled, but process any children
            {
                foreach (var childMemento in memento.ChildMementos)
                {
                    ProcessMemento(childMemento, false);
                }

                // unsubscribe from cancelled property change events
                var propertyChangeMemento = memento as NotifyPropertyChangeMemento;
                if (isCancelled && propertyChangeMemento != null)
                {
                    var key = propertyChangeMementos.Where(kv => kv.Value == memento).Select(kv => kv.Key).FirstOrDefault();
                    
                    if(key != null)
                    {
                        UnsubscribeDisconnected(propertyChangeMemento.OldValue);
                        propertyChangeMementos.Remove(key);
                    }
                }
            }

            // cleanup references
            if (lastPropertyChangeMemento == memento)
            {
                lastPropertyChangeMemento = null;
            }
            if (lastCollectionChangeMemento == memento)
            {
                lastCollectionChangeMemento = null;
            }
        }

        private static bool HasMementoEnded(IMemento memento)
        {
            if (memento is EditableObjectMemento)
            {
                return ((EditableObjectMemento) memento).Done;
            }
            return true;
        }
        
        /// <summary>
        /// The static 'BeforeEventCall' and 'AfterEventCall' are called for any change, however this tracker should only be tracking
        /// changes that happen in the observable's object graph. So the Observable*Changing / Changed actually fill the mementos
        /// with old value / new value etc. If at the end of an event call no Observable*Changed has been received, this indicates
        /// the change was not triggered by something in the observable object graph, which is detected by checking if the memento
        /// was modified. If it is not filled, it is typically thrown away later (and any child mementos it captured are processed 
        /// seperately)
        /// </summary>
        /// <param name="memento"></param>
        /// <returns></returns>
        private static bool IsMementoFilled(IMemento memento)
        {
            var propertyChangeMemento = memento as NotifyPropertyChangeMemento;
            if (propertyChangeMemento != null)
            {
                return propertyChangeMemento.Instance != null && !IsDataBindingBogusMemento(propertyChangeMemento);
            }

            var notifyCollectionChangeMemento = memento as NotifyCollectionChangeMemento;
            if (notifyCollectionChangeMemento != null)
            {
                return notifyCollectionChangeMemento.Filled;
            }

            var editableObjectMemento = memento as EditableObjectMemento;
            if (editableObjectMemento != null)
            {
                return !editableObjectMemento.Cancelled;
            }
            throw new NotImplementedException("unknown memento type: add implementation");
        }

        /// <summary>
        /// Hack in place to filter out useless (but corrupting) property changes caused by data binding refreshes
        /// TOOLS-7093
        /// </summary>
        /// <param name="memento"></param>
        /// <returns></returns>
        private static bool IsDataBindingBogusMemento(NotifyPropertyChangeMemento memento)
        {
            var isSameValue = false;
            if (memento.OldValue is bool)
            {
                isSameValue = (bool)memento.OldValue == (bool)memento.NewValue;
            }
            if (memento.OldValue is Enum)
            {
                isSameValue = (int)memento.OldValue == (int)memento.NewValue;
            }
            return isSameValue && (new StackTrace(false).ToString().Contains("Binding.PullData"));
        }

        private int expectedEventCascadeCallLevel;

        private bool trackChanges;
        public bool TrackChanges
        {
            get { return trackChanges; }
            set
            {
                trackChanges = value;
                EditActionSettings.SupportEditableObject = trackChanges;
                DataTableObserver.TrackChanges = trackChanges;
            }
        }

        public Action<IMemento> AddNewMementoCallback { get; set; }

        public IList<Type> ExcludedTypes { get; private set; }

        public object Observable
        {
            get { return observable; }
            set
            {
                if(observable != null)
                {
                    Unsubscribe(observable);
                }

                pendingIncrementEventCallCountSender = null;
                               
                observable = value;

                if(value == null)
                {
                    TrackChanges = false; // makes sure that we ignore static event handlers
                    return;
                }

                Subscribe(value);
            }
        }

        private void Subscribe(object item)
        {
            if (item == null)
            {
                return;
            }

            var notifyPropertyChange = item as INotifyPropertyChange;
            var notifyCollectionChange = item as INotifyCollectionChange;

            // does not support events
            if (notifyCollectionChange == null && notifyPropertyChange == null)
            {
                return;
            }

            if (notifyPropertyChange != null)
            {
                notifyPropertyChange.PropertyChanging += ObservablePropertyChanging;
                notifyPropertyChange.PropertyChanged += ObservablePropertyChanged;
            }
                
            if (notifyCollectionChange != null)
            {
                notifyCollectionChange.CollectionChanging += ObservableCollectionChanging;
                notifyCollectionChange.CollectionChanged += ObservableCollectionChanged;
            }
        }

        private void Unsubscribe(object item)
        {
            var notifyPropertyChange = item as INotifyPropertyChange;
            var notifyCollectionChange = item as INotifyCollectionChange;

            if(notifyPropertyChange == null && notifyCollectionChange == null)
            {
                return;
            }

            if (notifyPropertyChange != null)
            {
                notifyPropertyChange.PropertyChanging -= ObservablePropertyChanging;
                notifyPropertyChange.PropertyChanged -= ObservablePropertyChanged;
            }

            if (notifyCollectionChange != null)
            {
                notifyCollectionChange.CollectionChanging -= ObservableCollectionChanging;
                notifyCollectionChange.CollectionChanged -= ObservableCollectionChanged;
            }
        }
        
        private void ObservablePropertyChanging(object sender, PropertyChangingEventArgs e)
        {
            if (!TrackChanges)
            {
                return;
            }
            
            pendingIncrementEventCallCountSender = null;

            ThrowIfFromDifferentThread();

            if (ExcludedTypes.Contains(sender.GetType()))
            {
                return;
            }

            if (!handlingDisconnected)
            {
                //we got an event through the normal path, so we're sure this object is still connected
                UnsubscribeDisconnected(sender);
            }

            FixCancelledCollectionChangingEvents();

            var key = new PropertyChangeKey(sender, e.PropertyName);

            NotifyPropertyChangeMemento existingMemento;
            propertyChangeMementos.TryGetValue(key, out existingMemento);

            if (existingMemento != null)
            {
                // check if property is virtual, if so - show tip how to fix it
                if (CheckVirtualPropertyNames(sender, e.PropertyName))
                {
                    // ignore and add property to the virtual property list so that it can be handled in changed handler
                    virtualPropertyCalls.Add(e.PropertyName);
                    return;
                }
            }

            if (restoringMemento != null)
            {
                if (!IsPropertyChangeEventPartOfRestore(sender, e.PropertyName))
                {
                    if (e.PropertyName != "IsEditing")
                    {
                        throw new InvalidOperationException("Side-effect code detected (code which changes object tree in setters or in event handlers) which is not marked with [EditAction]");
                    }
                }
            }

            if (eventCascadeCallLevel > 1 && !handlingDisconnected)
            {
                if (eventCascadeCallLevel < expectedEventCascadeCallLevel)
                {
                    expectedEventCascadeCallLevel = eventCascadeCallLevel;
                }

                // we must be in a side-effect
                if (expectedEventCascadeCallLevel != eventCascadeCallLevel)
                {
                    LogCascadeLevels();
                    throw new InvalidOperationException("Side-effect code detected (code which changes object tree in setters or in event handlers) which is not marked with [EditAction]");
                }
            }
            
            if (existingMemento != null)
            {
                if(existingMemento.LastEventSenderIsDisconnected || handlingDisconnected)
                {
                    propertyChangeMementosDisconnected[key] = existingMemento;
                    return; // ignore
                }
                
                var oldMemento = propertyChangeMementos[key];

                LogMementoSenders(oldMemento);

                throw new InvalidOperationException(string.Format("Property {0} of {1} is already being changed", e.PropertyName, sender));
            }

            var memento = (NotifyPropertyChangeMemento)mementoStack.Peek();
            memento.RememberOldValue(sender, e);

            propertyChangeMementos[key] = memento;
            
            // debugging
            memento.LastEventSender = EventSettings.LastEventBubbler;
            memento.LastEventSenderIsDisconnected = handlingDisconnected;

            if (!handlingDisconnected && !TypeUtils.IsAggregationProperty(sender, e.PropertyName))
            {
                if (restoringMemento == null)
                {
                    HandleIfTransactional(memento.OldValue, true);
                }

                SubscribeDisconnected(memento.OldValue); // listen to changes in disconnected objects
            }
        }

        private void ObservablePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (!TrackChanges)
            {
                return;
            }

            ThrowIfFromDifferentThread();

            if (ExcludedTypes.Contains(sender.GetType()))
            {
                return;
            }

            if (!handlingDisconnected)
            {
                //we got an event through the normal path, so we're sure this object is still connected
                UnsubscribeDisconnected(sender);
            }

            // if virtual property was added - try to handle it
            if(virtualPropertyCalls.Contains(e.PropertyName) && CheckVirtualPropertyNames(sender, e.PropertyName))
            {
                virtualPropertyCalls.Remove(e.PropertyName);
                return; // will be handled in second call (in derived class setter)
            }

            NotifyPropertyChangeMemento memento;
            var key = new PropertyChangeKey(sender, e.PropertyName);
            propertyChangeMementos.TryGetValue(key, out memento);

            if (memento == null)
            {
                // check if it is not something from disconnected objects
                if (propertyChangeMementosDisconnected.ContainsKey(key))
                {
                    propertyChangeMementosDisconnected.Remove(key);
                    return;
                }
                throw new NotSupportedException("PropertyChanged received without PropertyChanging");
            }

            memento.RememberNewValue(sender, e);
            propertyChangeMementos.Remove(key);
            if (!handlingDisconnected && propertyChangeMementosDisconnected.ContainsKey(key))
            {
                propertyChangeMementosDisconnected.Remove(key);
            }

            // exceptional situation, same object!
            lastPropertyChangeMemento = memento;

            if (!TypeUtils.IsAggregationProperty(sender, e.PropertyName))
            {
                if (memento.NewValue != null && ReferenceEquals(memento.OldValue, memento.NewValue))
                {
                    UnsubscribeDisconnected(memento.OldValue);
                }
                UnsubscribeDisconnected(memento.NewValue);
            }

            // check if we need to add memento to current compound memento or to undoRedoManager
            if (e.PropertyName == "IsEditing")
            {
                EditableObjectIsEditingChanged(memento);
            }
            else
            {
                LogPropertyChanged(sender, e, memento);
            }
        }

        private bool InEditableObjectAction()
        {
            return mementoStack.OfType<EditableObjectMemento>().Any();
        }

        private bool InEditAction()
        {
            return InEditableObjectAction() || editActionCallCount > 0;
        }

        private void EditableObjectIsEditingChanged(NotifyPropertyChangeMemento memento)
        {
            var editable = (IEditableObject)memento.Instance;
            EditableObjectMemento editableObjectMemento = null;

            var orphanedMementos = RemoveIsEditingMemento();
            
            if (editable.IsEditing) // BeginEdit()
            {
                editableObjectMemento = new EditableObjectMemento(editable, editable.CurrentEditAction);
                mementoStack.Push(editableObjectMemento); //inject EditableObject memento
            }
            else // EndEdit()
            {
                if (editable.CurrentEditAction == null)
                {
                    throw new ArgumentException(
                        "CurrentEditAction is null while IsEditing is set to false, two potential causes:\n" +
                        "1. Check the order in EndEdit (first IsEditing false, then CurrentEditAction null)\n" +
                        "2. Check edit actions are not being called recursive (a new edit action starts while another is busy)\n"+
                        "     If the latter is the case, consider using a stack to store nested actions, see for example DataItem\n");
                }

                editableObjectMemento = (EditableObjectMemento)mementoStack.Peek();
                if (!ReferenceEquals(editableObjectMemento.Editable, memento.Instance))
                {
                    throw new ArgumentException("IsEditing events out of sync: did anyone call BeginEdit in response to another BeginEdit (instead of EndEdit)");
                }
                editableObjectMemento.Done = true; //mark done
                editableObjectMemento.Cancelled = editable.EditWasCancelled;

            }
            // add orphaned mementos to edit action
            orphanedMementos.ForEach(editableObjectMemento.ChildMementos.Add);

            LogIsEditingChanged(editable);
        }
        
        private IEnumerable<IMemento> RemoveIsEditingMemento()
        {
            var isEditingMemento = (NotifyPropertyChangeMemento)mementoStack.Pop(); //remove IsEditing property memento
            return isEditingMemento.ChildMementos;
        }
        
        /// <summary>
        /// Checks if we're done with all pending events and if there are no hanging cancelled events waiting
        /// </summary>
        private void FixCancelledCollectionChangingEvents()
        {
            // if edit object is active - skip it
            if (InEditableObjectAction())
            {
                return;
            }

            // fix only when we get event for main observable, not for disconnected objects!
            if(handlingDisconnected)
            {
                return;
            }

            if (eventCascadeCallLevel == 1) // first change is being fired
            {
                foreach (var m in collectionChangeMementos.Values)
                {
                    log.DebugFormat("Collection cancel event detected, clearing {0}: index:{1} {2}", m.Action, m.Index, m.NewValue ?? "null");
                    UnsubscribeDisconnected(m.OldValue);
                }
                collectionChangeMementos.Clear();
            }

        }

        void ObservableCollectionChanging(object sender, NotifyCollectionChangingEventArgs e)
        {
            if (!TrackChanges)
            {
                return;
            }
            ThrowIfFromDifferentThread();

            if (ExcludedTypes.Contains(sender.GetType()))
            {
                return;
            }

            if (!handlingDisconnected)
            {
                //we got an event through the normal path, so we're sure this object is still connected
                UnsubscribeDisconnected(sender);
            }

            // normally this will not happen unless event handlers are subscribed before undo/redo, check nevertheless
            if (e.Cancel)
            {
                log.DebugFormat("CollectionChanging event was cancelled, skipping undo");
                return;
            }

            FixCancelledCollectionChangingEvents();
            var key = new CollectionChangeKey(sender, e.Item, e.Action);
            if (collectionChangeMementos.ContainsKey(key))
            {
                if (handlingDisconnected)
                {
                    return; //already received
                }

                throw new NotSupportedException("The same CollectionChanging event fired twice (probably two different event paths)");
            }

            if (restoringMemento != null)
            {
                if (!IsCollectionChangeEventPartOfRestore(sender, e.Index))
                {
                    throw new InvalidOperationException("Side-effect code detected (code which changes object tree in setters or in event handlers) which is not marked with [EditAction]");
                }
            }

            if (eventCascadeCallLevel > 1 && !handlingDisconnected)
            {
                if (eventCascadeCallLevel < expectedEventCascadeCallLevel)
                {
                    expectedEventCascadeCallLevel = eventCascadeCallLevel;
                }

                // we must be in a side-effect
                if (expectedEventCascadeCallLevel != eventCascadeCallLevel)
                {
                    log.DebugFormat("CascadeEventLevel: {0}, ExpectedCascadeEventLevel: {1}", eventCascadeCallLevel, expectedEventCascadeCallLevel); 
                    throw new InvalidOperationException("Side-effect code detected (code which changes object tree in setters or in event handlers) which is not marked with [EditAction]");
                }
            }

            var memento = (NotifyCollectionChangeMemento)mementoStack.Peek();
            memento.RememberOldValues(sender, e);

            collectionChangeMementos[key] = memento;

            if (!handlingDisconnected && !TypeUtils.IsAggregationList(sender))
            {
                if (restoringMemento == null)
                {
                    HandleIfTransactional(memento.OldValue, true);
                }
                SubscribeDisconnected(memento.OldValue); // listen to the changes in disconnected objects
            }
        }

        private readonly IList<object> handledTransactionals = new List<object>();
        private void HandleIfTransactional(object value, bool beginChange)
        {
            var transactional = value as ITransactionalChangeAccess;
            if (transactional != null)
            {
                if (beginChange)
                {
                    if (!transactional.IsChanging)
                    {
                        transactional.BeginChanges();
                        handledTransactionals.Add(transactional);
                    }
                }
                else
                {
                    if (handledTransactionals.Contains(transactional))
                    {
                        handledTransactionals.Remove(transactional);
                        if (transactional.IsChanging)
                        {
                            transactional.CommitChanges();
                        }
                    }
                }
            }

            // recursive
            var itemContainer = value as IItemContainer;
            if (itemContainer != null)
            {
                foreach (var transactional2 in itemContainer.GetAllItemsRecursive().OfType<ITransactionalChangeAccess>())
                {
                    HandleIfTransactional(transactional2, beginChange);
                }
            }
        }
        private void RollbackIfTransactional(object item)
        {
            var transactional = item as ITransactionalChangeAccess;
            if (transactional != null)
            {
                if (handledTransactionals.Contains(transactional))
                {
                    transactional.RollbackChanges();
                    handledTransactionals.Remove(transactional);
                }
            }
            //recursive
            var itemContainer = item as IItemContainer;
            if (itemContainer != null)
            {
                foreach (var transactional2 in itemContainer.GetAllItemsRecursive().OfType<ITransactionalChangeAccess>())
                {
                    RollbackIfTransactional(transactional2);
                }
            }
        }

        private static object GetOldValueIfComposition(IMemento memento)
        {
            object oldValue = null;

            var propertyChangeMemento = memento as NotifyPropertyChangeMemento;
            var collectionChangeMemento = memento as NotifyCollectionChangeMemento;

            if (propertyChangeMemento != null)
            {
                if (!TypeUtils.IsAggregationProperty(propertyChangeMemento.Instance, propertyChangeMemento.PropertyName))
                {
                    oldValue = propertyChangeMemento.OldValue;
                }
            }
            else if (collectionChangeMemento != null)
            {
                if (!TypeUtils.IsAggregationList(collectionChangeMemento.List))
                {
                    oldValue = collectionChangeMemento.OldValue;
                }
            }
            return oldValue;
        }

        private static object GetNewValueIfComposition(IMemento memento)
        {
            object newValue = null;

            var propertyChangeMemento = memento as NotifyPropertyChangeMemento;
            var collectionChangeMemento = memento as NotifyCollectionChangeMemento;

            if (propertyChangeMemento != null)
            {
                if (!TypeUtils.IsAggregationProperty(propertyChangeMemento.Instance, propertyChangeMemento.PropertyName))
                {
                    newValue = propertyChangeMemento.NewValue;
                }
            }
            else if (collectionChangeMemento != null)
            {
                if (!TypeUtils.IsAggregationList(collectionChangeMemento.List))
                {
                    newValue = collectionChangeMemento.NewValue;
                }
            }
            return newValue;
        }

        void ObservableCollectionChanged(object sender, NotifyCollectionChangingEventArgs e)
        {
            if (!TrackChanges)
            {
                return;
            }

            ThrowIfFromDifferentThread();

            if (ExcludedTypes.Contains(sender.GetType()))
            {
                return;
            }

            if (!handlingDisconnected)
            {
                //we got an event through the normal path, so we're sure this object is still connected
                UnsubscribeDisconnected(sender);
            }

            NotifyCollectionChangeMemento memento;
            var key = new CollectionChangeKey(sender, e.Item, e.Action);
            collectionChangeMementos.TryGetValue(key, out memento);

            if (memento == null)
            {
                throw new NotSupportedException("CollectionChanged received without CollectionChanging");
            }

            memento.RememberNewValues(sender, e);
            collectionChangeMementos.Remove(key);

            lastCollectionChangeMemento = memento;

            // exceptional situation, same object!
            if (!TypeUtils.IsAggregationList(sender))
            {
                if (ReferenceEquals(memento.OldValue, memento.NewValue))
                {
                    UnsubscribeDisconnected(memento.OldValue);
                }
                UnsubscribeDisconnected(memento.NewValue); // newly added value
            }

            LogCollectionChanged(memento, e);
        }

        private void ThrowIfFromDifferentThread()
        {
            if (Thread.CurrentThread.ManagedThreadId != threadId)
            {
                throw new NotSupportedException("Changing objects which are under undo/redo from 2 threads is not supported, make sure that 1) undo/redo is disabled during such edits 2) undo/redo is cleared after change");
            }
        }

        private static bool CheckVirtualPropertyNames(object sender, string propertyName)
        {
            var baseTypes = GetAllBaseTypes(sender.GetType());

            var typesWithProperty = new List<Type>();
            var propertyInfos = new List<PropertyInfo>();

            foreach (var type in baseTypes)
            {
                var propertyInfo = type.GetProperty(propertyName, BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public);
                
                if (propertyInfo == null)
                {
                    continue;
                }
                
                propertyInfos.Add(propertyInfo);
                typesWithProperty.Add(type);
            }

            if(typesWithProperty.Count > 1)
            {
                log.DebugFormat("More than one property with the same name found in base classes, check that only one property fires PropertyChange events: " + propertyName);
                foreach (var type in typesWithProperty)
                {
                    log.DebugFormat("   type: " + type);    
                }

                return true;
            }

            return false;
        }

        private static IEnumerable<Type> GetAllBaseTypes(Type type)
        {
            yield return type;

            if (type.BaseType != typeof(object))
            {
                foreach (var baseType in GetAllBaseTypes(type.BaseType))
                {
                    yield return baseType;
                }
            }
        }

        /// <summary>
        /// Subscribes to object removed from collections due to undo, 
        /// however we still have to track changes in them.
        /// </summary>
        /// <param name="memento">Memento containing objects after undo</param>
        public void OnBeforeUndo(IMemento memento)
        {
            OnBeforeUndoRedoTransactional(memento);

            log.DebugFormat("==== On Before Undo ====");
            ThrowIfInternalStateInconsistent();
            restoringMemento = memento;
        }

        public void OnAfterUndo(IMemento memento)
        {
            log.DebugFormat("==== On After Undo ====");
            restoringMemento = null;
            ThrowIfInternalStateInconsistent();
        }

        public void OnBeforeRedo(IMemento memento)
        {
            OnBeforeUndoRedoTransactional(memento);

            log.DebugFormat("==== On Before Redo ====");
            ThrowIfInternalStateInconsistent();
            restoringMemento = memento;
        }

        public void OnAfterRedo(IMemento memento)
        {
            log.DebugFormat("==== On After Redo ====");
            restoringMemento = null;
            ThrowIfInternalStateInconsistent();
        }

        private void OnBeforeUndoRedoTransactional(IMemento memento)
        {
            HandleIfTransactional(GetNewValueIfComposition(memento), true);
            RollbackIfTransactional(GetOldValueIfComposition(memento));

            foreach (var childMemento in memento.ChildMementos)
            {
                OnBeforeUndoRedoTransactional(childMemento);
            }
        }

        public void OnBeforeRemoveUndo(IMemento memento)
        {
            if (memento is NotifyPropertyChangeMemento)
            {
                var notifyPropertyChangeMemento = memento as NotifyPropertyChangeMemento;
                UnsubscribeDisconnected(notifyPropertyChangeMemento.OldValue);
                UnsubscribeDisconnected(notifyPropertyChangeMemento.NewValue);
            }
            if (memento is NotifyCollectionChangeMemento)
            {
                var notifyCollectionChangeMemento = memento as NotifyCollectionChangeMemento;
                UnsubscribeDisconnected(notifyCollectionChangeMemento.OldValue);
                UnsubscribeDisconnected(notifyCollectionChangeMemento.NewValue);
            }

            HandleIfTransactional(GetOldValueIfComposition(memento), false);

            foreach (var childMemento in memento.ChildMementos)
            {
                OnBeforeRemoveUndo(childMemento);
            }
        }

        public void OnBeforeRemoveRedo(IMemento memento)
        {
            if (memento is NotifyPropertyChangeMemento)
            {
                var notifyPropertyChangeMemento = memento as NotifyPropertyChangeMemento;
                UnsubscribeDisconnected(notifyPropertyChangeMemento.OldValue);
                UnsubscribeDisconnected(notifyPropertyChangeMemento.NewValue);
            }
            if (memento is NotifyCollectionChangeMemento)
            {
                var notifyCollectionChangeMemento = memento as NotifyCollectionChangeMemento;
                UnsubscribeDisconnected(notifyCollectionChangeMemento.OldValue);
                UnsubscribeDisconnected(notifyCollectionChangeMemento.NewValue);
            }

            HandleIfTransactional(GetOldValueIfComposition(memento), false);

            foreach (var childMemento in memento.ChildMementos)
            {
                OnBeforeRemoveRedo(childMemento);
            }
        }
        
        private void SubscribeDisconnected(object item)
        {
            if (item == null || item.GetType().IsValueType)
            {
                return;
            }

            var newItem = disconnectedObjectHashes.Add(RuntimeHelpers.GetHashCode(item));
            var notifyPropertyChange = item as INotifyPropertyChange;
            var notifyCollectionChange = item as INotifyCollectionChange;

            LogSubscriptions(item, notifyCollectionChange, notifyPropertyChange, newItem);

            if (!newItem)
            {
                return;
            }
            
            if (notifyPropertyChange != null)
            {
                notifyPropertyChange.PropertyChanging += DisconnectedPropertyChanging;
                notifyPropertyChange.PropertyChanged += DisconnectedPropertyChanged;
            }

            if (notifyCollectionChange != null)
            {
                notifyCollectionChange.CollectionChanging += DisconnectedCollectionChanging;
                notifyCollectionChange.CollectionChanged += DisconnectedCollectionChanged;
            }
        }

        private void UnsubscribeDisconnected(object item)
        {
            if (item == null || item.GetType().IsValueType)
            {
                return;
            }

            disconnectedObjectHashes.Remove(RuntimeHelpers.GetHashCode(item));

            var notifyPropertyChange = item as INotifyPropertyChange;
            var notifyCollectionChange = item as INotifyCollectionChange;

            LogUnsubscribtions(item, notifyCollectionChange, notifyPropertyChange);

            if (notifyPropertyChange != null)
            {
                notifyPropertyChange.PropertyChanging -= DisconnectedPropertyChanging;
                notifyPropertyChange.PropertyChanged -= DisconnectedPropertyChanged;
            }

            if (notifyCollectionChange != null)
            {
                notifyCollectionChange.CollectionChanging -= DisconnectedCollectionChanging;
                notifyCollectionChange.CollectionChanged -= DisconnectedCollectionChanged;
            }
        }

        private bool IsCollectionChangeEventPartOfRestore(object sender, int index)
        {
            if (restoringMemento != null)
            {
                var undoingCollectionChangeMemento = GetCurrentCollectionChangeMemento();
                if (undoingCollectionChangeMemento != null && 
                    Equals(undoingCollectionChangeMemento.List, sender) &&
                    undoingCollectionChangeMemento.Index == index)
                {
                    return true;
                }
            }
            return false;
        }

        private bool IsPropertyChangeEventPartOfRestore(object sender, string propertyName)
        {
            if (restoringMemento != null)
            {
                var undoingPropertyChangeMemento = GetCurrentPropertyChangeMemento();
                if (undoingPropertyChangeMemento != null &&
                    (Equals(undoingPropertyChangeMemento.Instance, sender) &&
                     undoingPropertyChangeMemento.PropertyName == propertyName))
                {
                    return true;
                }
                var editableObjectMemento = restoringMemento.CurrentSimpleMemento as EditableObjectMemento;
                if (propertyName == "IsEditing" && editableObjectMemento != null && Equals(editableObjectMemento.Editable, sender))
                {
                    return true;
                }
            }
            return false;
        }

        private void DisconnectedCollectionChanging(object sender, NotifyCollectionChangingEventArgs e)
        {
            if (!disconnectedObjectHashes.Contains(RuntimeHelpers.GetHashCode(EventSettings.LastEventBubbler)))
            {
                //see comments in DisconnectedPropertyChanging
                UnsubscribeDisconnected(sender);
                return; 
            }

            if (restoringMemento != null)
            {
                if (IsCollectionChangeEventPartOfRestore(sender, e.Index))
                {
                    handlingDisconnected = true;
                    // track actual changes in disconnected objects
                    ObservableCollectionChanging(sender, e);
                    handlingDisconnected = false;
                    return;
                }
            }
            else
            {
                if (InEditAction())
                {
                    if (collectionChangeMementos.ContainsKey(new CollectionChangeKey(sender, e.Item, e.Action)))
                    {
                        UnsubscribeDisconnected(sender);
                    }
                    else
                    {
                        handlingDisconnected = true;
                        // track actual changes in disconnected objects
                        ObservableCollectionChanging(sender, e);
                        handlingDisconnected = false;
                    }

                    return;
                }

                if (editActionCallCount > 0)
                {
                    return; // changes occuring in side effects (isolated)
                }
            }
            if (ExcludedTypes.Contains(sender.GetType()))
            {
                return;
            }

            throw new InvalidOperationException("Disconnected object which is removed from the main container object but remains in undo/redo stack is being changed, check if side-effect method is marked [EditAction]");
        }

        private void DisconnectedCollectionChanged(object sender, NotifyCollectionChangingEventArgs e)
        {
            if (InEditAction())
            {
                if (lastCollectionChangeMemento == null ||
                    !(Equals(lastCollectionChangeMemento.List, sender) && lastCollectionChangeMemento.Index == e.Index))
                {
                    // track actual changes in disconnected objects
                    handlingDisconnected = true;
                    ObservableCollectionChanged(sender, e);
                    handlingDisconnected = false;
                }
                else
                {
                    // skip already handled event
                    lastCollectionChangeMemento = null;
                }
            }
        }
        
        private void DisconnectedPropertyChanging(object sender, PropertyChangingEventArgs e)
        {
            if (!disconnectedObjectHashes.Contains(RuntimeHelpers.GetHashCode(EventSettings.LastEventBubbler)))
            { 
                //1: bubbler was re-connected (but event still received because of multi-cast event), or:
                //2: bubbler was not removed from one and then added to another (disconnect, reconnect), but was
                //   added to another and then removed from the first (inverse order: reconnect, disconnect), causing 
                //   the event subscription to be the wrong way around. For this case we unsubscribe here again just 
                //   to be sure:
                UnsubscribeDisconnected(sender);
                return;
            }

            if (restoringMemento != null)
            {
                if (IsPropertyChangeEventPartOfRestore(sender, e.PropertyName))
                {
                    handlingDisconnected = true;
                    // track actual changes in disconnected objects
                    ObservablePropertyChanging(sender, e);
                    handlingDisconnected = false;
                    return;
                }
            }
            else
            {
                if (InEditAction())
                {
                    // object is already being changed - disconnect
                    if(propertyChangeMementos.ContainsKey(new PropertyChangeKey(sender,e.PropertyName)))
                    {
                        throw new InvalidOperationException(
                            string.Format(
                                "Event already received on non-disconnected path, or overriden properties both firing: {0} for type {1}",
                                e.PropertyName,
                                sender.GetType()));
                    }
                    else
                    {
                        handlingDisconnected = true;
                        // track actual changes in disconnected objects
                        ObservablePropertyChanging(sender, e);
                        handlingDisconnected = false;
                    }

                    return;
                }

                if (editActionCallCount > 0)
                {
                    return; // changes occuring in side effects (isolated)
                }
            }

            if (ExcludedTypes.Contains(sender.GetType()))
            {
                return;
            }

            throw new InvalidOperationException("Disconnected object which is removed from the main container container but remains in undo/redo stack is being changed, check if side-effect method is marked [EditAction]");
        }

        private NotifyCollectionChangeMemento lastCollectionChangeMemento;
        private NotifyPropertyChangeMemento lastPropertyChangeMemento;

        private void DisconnectedPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (restoringMemento != null)
            {
                if (IsPropertyChangeEventPartOfRestore(sender, e.PropertyName))
                {
                    handlingDisconnected = true;
                    //track actual changes in disconnected objects
                    ObservablePropertyChanged(sender, e);
                    handlingDisconnected = false;
                    return;
                }
            }
            else
            {
                if (InEditAction())
                {
                    if (!AlreadySeenEvent(sender, e))
                    {
                        handlingDisconnected = true;
                        ObservablePropertyChanged(sender, e); // track actual changes in disconnected objects
                        handlingDisconnected = false;
                    }
                    else
                    {
                        // skip already handled event
                        lastPropertyChangeMemento = null;
                    }
                }
            }
        }

        private bool AlreadySeenEvent(object sender, PropertyChangedEventArgs e)
        {
            if (lastPropertyChangeMemento == null) 
                return false;
            return (lastPropertyChangeMemento.Instance == sender &&
                    lastPropertyChangeMemento.PropertyName == e.PropertyName &&
                    Equals(lastPropertyChangeMemento.NewValue, TypeUtils.GetPropertyValue(sender, e.PropertyName)));
        }

        private NotifyCollectionChangeMemento GetCurrentCollectionChangeMemento()
        {
            return restoringMemento.CurrentSimpleMemento as NotifyCollectionChangeMemento;
        }

        private NotifyPropertyChangeMemento GetCurrentPropertyChangeMemento()
        {
            return restoringMemento.CurrentSimpleMemento as NotifyPropertyChangeMemento;
        }

        #region Logging
        
        private static void LogIsEditingChanged(IEditableObject editable)
        {
            if (!EventSettings.EnableLogging)
                return;
            
            if (editable.IsEditing)
            {
                log.DebugFormat(
                    "starting compound edit action {0}.{1} ({2})",
                    editable,
                    editable.CurrentEditAction.Name,
                    editable.GetType().Name);
            }
            else
            {
                log.DebugFormat(
                    "ended compound edit action {0}.{1} ({2})",
                    editable,
                    editable.CurrentEditAction.Name,
                    editable.GetType().Name);
            }
        }

        private void LogPropertyChanged(object sender, PropertyChangedEventArgs e, NotifyPropertyChangeMemento memento)
        {
            if (!EventSettings.EnableLogging)
                return;
            
            log.DebugFormat(
                "{0} {1}.{2}: {3} -> {4} ({5})",
                InEditableObjectAction()
                    ? "adding property change to edit action"
                    : "saving undo for property change",
                sender.GetType().Name,
                e.PropertyName,
                memento.OldValue ?? "null",
                memento.NewValue ?? "null",
                memento.LastEventSender);
        }

        private void LogCollectionChanged(NotifyCollectionChangeMemento memento, NotifyCollectionChangingEventArgs e)
        {
            if (!EventSettings.EnableLogging)
                return;
            
            if (InEditableObjectAction())
            {
                if (e.Action == NotifyCollectionChangeAction.Replace)
                {
                    log.DebugFormat("adding collection change to edit action {0}: index:{1} {2} -> {3}", memento.Action,
                                    memento.Index, memento.OldValue ?? "null", memento.NewValue ?? "null");
                }
                else
                {
                    log.DebugFormat("adding collection change to edit action {0}: index:{1} {2}", memento.Action,
                                    memento.Index, memento.NewValue ?? "null");
                }
            }
            else
            {
                if (e.Action == NotifyCollectionChangeAction.Replace)
                {
                    log.DebugFormat("saving undo for collection change {0}: index:{1} {2} -> {3}", memento.Action,
                                    memento.Index, memento.OldValue ?? "null", memento.NewValue ?? "null");
                }
                else
                {
                    log.DebugFormat("saving undo for collection change {0}: index: {1} {2}", memento.Action,
                                    memento.Index, memento.NewValue ?? "null");
                }
            }
        }
        
        private void LogCascadeLevels()
        {
            if (!EventSettings.EnableLogging)
                return;

            log.DebugFormat("CascadeEventLevel: {0}, ExpectedCascadeEventLevel: {1}", eventCascadeCallLevel,
                            expectedEventCascadeCallLevel);
        }

        private static void LogMementoSenders(NotifyPropertyChangeMemento oldMemento)
        {
            if (!EventSettings.EnableLogging)
                return;

            log.DebugFormat("Old event sender: {0} ({1}), current event sender: {2} ({3})", oldMemento.LastEventSender,
                            oldMemento.LastEventSender.GetType(), EventSettings.LastEventBubbler,
                            EventSettings.LastEventBubbler.GetType());
        }

        private static void LogSubscriptions(object item, INotifyCollectionChange notifyCollectionChange,
                                             INotifyPropertyChange notifyPropertyChange, bool newItem)
        {
            if (!EventSettings.EnableLogging)
                return;

            if (!newItem)
            {
                log.DebugFormat("Tried to subscribe disconnected, but was already subscribed as such: {0} ({1})", item, item.GetType().Name);
                return;
            }
            if (notifyPropertyChange != null)
            {
                log.DebugFormat("Subscribed Disconnected to property changes of: {0} ({1})", item, item.GetType().Name);
            }
            if (notifyCollectionChange != null)
            {
                log.DebugFormat("Subscribed Disconnected to collection changes of: {0} ({1})", item, item.GetType().Name);
            }
        }

        private static void LogUnsubscribtions(object item, INotifyCollectionChange notifyCollectionChange, INotifyPropertyChange notifyPropertyChange)
        {
            if (!EventSettings.EnableLogging)
                return;

            if (notifyPropertyChange != null)
                log.DebugFormat("Unsubscribed Disconnected from property changes of: {0} ({1})", item, item.GetType().Name);
            if (notifyCollectionChange != null)
                log.DebugFormat("Unsubscribed Disconnected from collection changes of: {0} ({1})", item, item.GetType().Name);
        }

        private void ThrowIfInternalStateInconsistent()
        {
            string msg = null;

            if (propertyChangeMementos.Count != 0)
            {
                msg = "Property change mementos dictionary not empty before/after action!";
            }
            else if (propertyChangeMementosDisconnected.Count != 0)
            {
                msg = "Property change disconnected mementos dictionary not empty before/after action!";
            }
            else if (collectionChangeMementos.Count != 0)
            {
                msg = "Collection change mementos dictionary not empty before/after action!";
            }

            if (msg == null)
                return;

            throw new InvalidOperationException(msg);
        }

        #endregion
    }
}
