namespace DelftTools.Utils.Aop.Tests.TestClasses
{
    [NotifyPropertyChanged]
    internal class ChildObject
    {
        [NoBubbling]
        private ParentObject parent;

        public string Name { get; set; }

        [NoNotifyPropertyChanged] // skip notification bubbling
            public ParentObject Parent
        {
            get { return parent; }
            set { parent = value; }
        }
    }

    [NotifyPropertyChanged]
    internal class ParentObject
    {
        public ChildObject Child { get; set; }
    }
}