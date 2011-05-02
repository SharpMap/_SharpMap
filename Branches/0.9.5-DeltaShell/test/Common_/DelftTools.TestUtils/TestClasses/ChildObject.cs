using DelftTools.Utils.Aop.NotifyPropertyChanged;

namespace DelftTools.TestUtils.TestClasses
{
    [NotifyPropertyChanged]
    public class ChildObject
    {
        [NoBubbling]
        private ParentObject parent;

        public string Name { get; set; }

        [NoNotifyPropertyChanged]
        public string NameWithoutPropertyChanged { get; set; }

        [NoNotifyPropertyChanged] // skip notification bubbling
            public ParentObject Parent
        {
            get { return parent; }
            set { parent = value; }
        }
    }
}