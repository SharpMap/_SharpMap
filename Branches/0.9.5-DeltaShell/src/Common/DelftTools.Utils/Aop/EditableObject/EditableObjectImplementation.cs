using System;
using System.ComponentModel;
using PostSharp.Laos;

namespace DelftTools.Utils.Aop.EditableObject
{
    /// <summary>
    /// Implementation of the <see cref="IEditableObject"/> interface.
    /// </summary>
    /// <remarks>
    /// This implementation object also contains two dictionaries for the working copy and the backup copy
    /// of field values.
    /// </remarks>
    public class EditableObjectImplementation : IEditableObject
    {
        private readonly InstanceCredentials credentials;
        private readonly IProtectedInterface<IEditableObject> parentObject;
        private bool editable;
        private bool isEditing;

        public EditableObjectImplementation(IProtectedInterface<IEditableObject> parentObject,
                                            InstanceCredentials credentials)
        {
            this.credentials = credentials;
            this.parentObject = parentObject;
        }

        #region IEditableObject Members

        public virtual bool IsEditing
        {
            get { return isEditing; }
            set
            {
/*
                if(PropertyChanging != null)
                {
                    PropertyChanging(this, new PropertyChangingEventArgs("IsEditing"));
                }
*/

                isEditing = value;

/*
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("IsEditing"));
                }
*/
            }
        }

        public bool EditWasCancelled
        {
            get { return false; }
        }

        public IEditAction CurrentEditAction
        {
            get { throw new NotImplementedException(); }
        }

        public string CurrentEditActionName { get; private set; }

        public void BeginEdit(IEditAction action)
        {
            IsEditing = true;
        }

        
        public void EndEdit()
        {
            IsEditing = false;
        }

        public void CancelEdit()
        {
            IsEditing = false;
        }

        #endregion

        // public event PropertyChangedEventHandler PropertyChanged;
        // public event PropertyChangingEventHandler PropertyChanging;
    }
}