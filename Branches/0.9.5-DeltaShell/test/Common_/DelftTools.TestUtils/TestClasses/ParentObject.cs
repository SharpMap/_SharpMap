using DelftTools.Utils.Aop.NotifyPropertyChanged;
using DelftTools.Utils.Collections.Generic;

namespace DelftTools.TestUtils.TestClasses
{
    [NotifyPropertyChanged]
    public class ParentObject
    {
        public ChildObject Child { get; set; }

        public IEventedList<ChildObject> Children { get; set; }

        public ParentObject()
        {
            Children = new EventedList<ChildObject>();
        }
    }
}