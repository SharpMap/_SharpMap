using System;
using DelftTools.Utils;

namespace SharpMap.UI.Editors
{
    public class DummyEditableObject : IEditableObject
    {
        public bool IsEditing { get; set; }

        public bool EditWasCancelled
        {
            get { return false; }
        }

        public IEditAction CurrentEditAction
        {
            get { return null; }
        }

        
        public void BeginEdit(IEditAction action)
        {
            
        }

        public void EndEdit()
        {
        }

        public void CancelEdit()
        {
        }
    }
}