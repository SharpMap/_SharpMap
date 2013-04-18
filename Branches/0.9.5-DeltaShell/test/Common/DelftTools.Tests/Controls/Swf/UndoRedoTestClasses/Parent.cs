using System;
using DelftTools.Utils;
using DelftTools.Utils.Aop.NotifyCollectionChange;
using DelftTools.Utils.Aop.NotifyPropertyChange;
using DelftTools.Utils.Collections.Generic;

namespace DelftTools.Tests.Controls.Swf.UndoRedoTestClasses
{
    [NotifyPropertyChange]
    [NotifyCollectionChange]
    public class Parent : IEditableObject
    {
        public Parent()
        {
            Children = new EventedList<Child>();
        }

        public IEventedList<Child> Children { get; set; }

        public Parent GrandParent { get; set; }

        public Child Child { get; set; }

        public string Name { get; set; }
        
        public bool IsEditing { get; set; }

        public bool EditWasCancelled
        {
            get { return false; }
        }

        public IEditAction CurrentEditAction { get; private set; }
        
        public void BeginEdit(IEditAction action)
        {
            CurrentEditAction = action;
            IsEditing = true;
            
        }

        public void EndEdit()
        {
            CurrentEditAction = null;
            IsEditing = false;
        }

        public void CancelEdit()
        {
            EndEdit();
        }
    }
}