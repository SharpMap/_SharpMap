using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DelftTools.Utils.IO;

namespace SharpMap.Data.Providers
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
