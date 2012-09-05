using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace DelftTools.Utils.Workflow
{
    /// <summary>
    /// Defines entities which may be linked with each other. 
    /// Interface may be used to define different types of the associations between entities.
    /// It is up to object implementating ILinkable to decide how to behave when it is linked to source object.
    /// </summary>
    public interface ILinkable<T> where T : ILinkable<T>
    {
        /// <summary>
        /// Establishes a link with a source object. 
        /// Sets property LikedTo equal to a source object.
        /// Fires Linking event and Linked event after link has been established.
        /// If one of subscribers of Linking event sets Cancel property to true - this method will return false and will not establish a link.
        /// </summary>
        /// <param name="source">Object which will be used as a source for the current object.</param>
        /// <returns>True is object was linked successfully.</returns>
        bool LinkTo(T source);

        /// <summary>
        /// Unlinks current object from it's source.
        /// </summary>
        void Unlink();

        /// <summary>
        /// Gets object used as a source for a current object.
        /// </summary>
        bool IsLinked { get; }

        /// <summary>
        /// Gets object used as a source for a current object.
        /// </summary>
        T LinkedTo { get; }

        /// <summary>
        /// Gets array of the objects linked to the current object.
        /// </summary>
        IList<T> LinkedBy { get; }

        /// <summary>
        /// Fired before current object is linked to the target object.
        /// Event should be also fided in the target object so that it can react and cancel it if it is not possible to establish a link.
        /// </summary>
        event EventHandler<LinkingUnlinkingEventArgs<T>> Linking;

        /// <summary>
        /// Fired after object is linked to another object. Called in the otarged objects.
        /// </summary>
        event EventHandler<LinkedUnlinkedEventArgs<T>> Linked;

        event EventHandler<LinkingUnlinkingEventArgs<T>> Unlinking;

        event EventHandler<LinkedUnlinkedEventArgs<T>> Unlinked;
    }

    public class LinkingUnlinkingEventArgs<T> : CancelEventArgs where T : ILinkable<T>
    {
        private T source;
        private T target;

        public LinkingUnlinkingEventArgs(T source, T target)
        {
            this.source = source;
            this.target = target;
        }

        public T Source { get { return source; } }
        public T Target { get { return target; } }
    }

    public class LinkedUnlinkedEventArgs<T> : EventArgs where T : ILinkable<T>
    {
        private T source;
        private T target;

        public LinkedUnlinkedEventArgs(T source, T target,object previousValue)
        {
            this.source = source;
            this.target = target;
            this.PreviousValue = previousValue;
        }

        public T Source { get { return source; } }
        public T Target { get { return target; } }

        public object PreviousValue { get; private set; }
    }
}