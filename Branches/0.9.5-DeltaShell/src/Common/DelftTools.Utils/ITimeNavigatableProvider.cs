using System.Collections.Generic;

namespace DelftTools.Utils
{
    public interface ITimeNavigatableProvider
    {
        IEnumerable<ITimeNavigatable> Navigatables { get; }
    }
}