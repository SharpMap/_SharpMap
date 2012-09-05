using System.Collections.Generic;
using System.IO;
using ICSharpCode.SharpZipLib.Zip;

namespace DelftTools.Utils.IO
{
    public static class ZipFileUtils
    {
        //todo add callback function to report when extraction is finished. use separate thread?
        public static void Extract(string zipFile, string destinationPath)
        {
            var fastZip=new FastZip();
            fastZip.ExtractZip(zipFile,destinationPath,"");
        }

        /// <summary>
        /// Creates a zip file containing the provided filePaths. (File will be overwritten when it already exits)
        /// </summary>
        /// <param name="fileName">Name and path of the zipfile to create</param>
        /// <param name="filePaths">List with files to add to the zip file</param>
        /// <exception cref="IOException">Throws IOExecptions</exception>
        public static void Create(string fileName, List<string> filePaths)
        {
            Create(fileName, filePaths, true);
        }

        /// <summary>
        /// Creates a zip file containing the provided filePaths.
        /// </summary>
        /// <param name="fileName">Name and path of the zipfile to create</param>
        /// <param name="filePaths">List with files to add to the zip file</param>
        /// <param name="overwriteIfExits">Determents if the file will be overwritten if it already exists</param>
        /// <exception cref="IOException">Throws IOExecptions</exception>
        public static void Create(string fileName, List<string> filePaths, bool overwriteIfExits)
        {
            if (!overwriteIfExits && File.Exists(fileName))
            {
                throw new IOException("File already exists.");
            }

            var zipFile = ZipFile.Create(fileName);

            try
            {
                zipFile.BeginUpdate();

                if (filePaths != null)
                {
                    foreach (var file in filePaths)
                    {
                        zipFile.Add(file);
                    }
                }

                zipFile.CommitUpdate();
            }
            finally
            {
                if (zipFile != null)
                {
                    zipFile.Close();
                }
            }
        }
    }
}
