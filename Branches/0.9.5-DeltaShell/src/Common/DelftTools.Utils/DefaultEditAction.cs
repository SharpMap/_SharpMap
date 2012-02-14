namespace DelftTools.Utils
{
    public class DefaultEditAction : IEditAction
    {
        private readonly string name;

        public DefaultEditAction(string name)
        {
            this.name = name;
        }

        public string Name
        {
            get { return name; }
        }
    }
}