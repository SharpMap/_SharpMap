using DelftTools.Utils.Aop;
using DelftTools.Utils.Editing;

namespace DelftTools.Utils.UndoRedo.Mementos
{
    public class EditableObjectMemento : CompoundMemento
    {
        private const string ReverseActionPrefix = "[Reverse] ";

        public IEditableObject Editable { get; private set; }
        private IEditAction EditAction { get; set; }

        public EditableObjectMemento(IEditableObject editable, IEditAction editAction)
        {
            Editable = editable;
            EditAction = editAction;
        }

        public string Name { get { return EditAction.Name; } }

        public override string ToString()
        {
            return Name ?? "unknown edit action";
        }

        public override void Restore()
        {
            Editable.BeginEdit(GetReverseName(EditAction));

            if (!EditAction.SuppressEventBasedRestore)
            {
                base.Restore();
            }

            if (EditAction.HandlesRestore)
            {
                EditActionSettings.AllowRestoreActions = true; //todo: don't mix EditAction & SideEffect ?!?!?
                EditAction.Restore();
                EditActionSettings.AllowRestoreActions = false;
            }

            Editable.EndEdit();
        }

        private static string GetReverseName(IEditAction editAction)
        {
            var name = editAction.Name ?? "";

            if (name.StartsWith(ReverseActionPrefix))
            {
                return name.Substring(ReverseActionPrefix.Length);
            }
            return ReverseActionPrefix + name;
        }

        public bool Done { get; set; }
        public bool Cancelled { get; set; }
    }
}