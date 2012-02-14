namespace DelftTools.Utils
{
    /// <summary>
    /// Types implementing this interface have a name.
    /// </summary>
    public interface INameable : IName
    {
        new string Name { get; set; }
    }

    public interface IName // TODO: remove it, throw NotSupportedException in a few cases where setter is not supported
    {
        string Name { get; }
    }
}
