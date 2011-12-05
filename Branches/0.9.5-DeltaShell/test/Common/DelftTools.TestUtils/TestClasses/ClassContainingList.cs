using DelftTools.Utils.Aop;
using DelftTools.Utils.Aop.NotifyPropertyChanged;
using DelftTools.Utils.Collections.Generic;

namespace DelftTools.TestUtils.TestClasses
{
    [NotifyCollectionChanged]
    public class CollectionChangedAspectTestClass
    {
        public CollectionChangedAspectTestClass()
        {
            ListContainers = new EventedList<CollectionChangedAspectTestClass>();
            Integers = new EventedList<int>();
            NoBubblingIntegers = new EventedList<int>();
            Lists = new EventedList<IEventedList<int>>();
        }

        public string Name { get; set; }

        public IEventedList<int> Integers { get; set; }

        public IEventedList<CollectionChangedAspectTestClass> ListContainers { get; private set; }

        public IEventedList<IEventedList<int>> Lists { get; private set; }

        [NoBubbling]
        public IEventedList<int> NoBubblingIntegers { get; private set; }
    }
}