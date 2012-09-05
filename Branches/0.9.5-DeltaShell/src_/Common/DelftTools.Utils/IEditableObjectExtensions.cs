namespace DelftTools.Utils
{
    public static class IEditableObjectExtensions
    {
        public static void BeginEdit(this IEditableObject obj, string actionName)
        {
            obj.BeginEdit(new DefaultEditAction(actionName));
        }
    }
}