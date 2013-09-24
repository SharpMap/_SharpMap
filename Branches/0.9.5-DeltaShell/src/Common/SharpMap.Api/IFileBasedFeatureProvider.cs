using DelftTools.Utils.IO;

namespace SharpMap.Api
{
    public interface IFileBasedFeatureProvider: IFileBased, IFeatureProvider
    {
        /// <example>
        /// "My file format1 (*.ext1)|*.ext1|My file format2 (*.ext2)|*.ext2"
        /// </example>
        string FileFilter { get; }

        bool IsRelationalDataBase { get; }
    }
}
