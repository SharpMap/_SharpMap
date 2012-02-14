using System.Collections.Generic;
using System.Linq;

namespace DelftTools.Utils.UndoRedo.Mementos
{
    public class CompoundMemento : IMemento
    {
        public string Name;

        public IMemento Restore()
        {
            var inverseMemento = new CompoundMemento { Name = Name };

            foreach (var childMemento in ChildMementos.Reverse())
            {
                inverseMemento.ChildMementos.Add(childMemento.Restore());
            }

            return inverseMemento;
        }

        public readonly IList<IMemento> ChildMementos = new List<IMemento>();

        public override string ToString()
        {
            return Name ?? "unknown edit action";
        }
    }
}