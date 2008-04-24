using SharpMap.Data.Providers;

namespace SharpMap.Layers
{
    public interface IDataLayer : ILayer
    {
        IProvider DataSource { get; set; }
    }
}
