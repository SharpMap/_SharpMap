using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using log4net;
using NDepend.Helpers.FileDirectoryPath;

namespace DelftTools.Utils.IO
{
    /// <summary>
    /// File manipulations
    /// </summary>
    public static class FileUtils
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(FileUtils));

        /// <summary>
        /// Copy all files and folders in a directory to another directory
        /// </summary>
        /// <param name="sourceDirectory"></param>
        /// <param name="targetDirectory"></param>
        /// <param name="ignorePath"></param>
        public static void Copy(string sourceDirectory, string targetDirectory, string ignorePath)
        {
            var diSource = new DirectoryInfo(sourceDirectory);
            var diTarget = new DirectoryInfo(targetDirectory);

            CopyAll(diSource, diTarget, ignorePath);
        }

        /// <summary>
        /// Copy files in a direcory and its subdirectories to another directory.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <param name="ignorePath"></param>
        public static void CopyAll(DirectoryInfo source, DirectoryInfo target, string ignorePath)
        {
            foreach (var diSourceSubDir in source.GetDirectories().Where(diSourceSubDir => diSourceSubDir.Name != ignorePath))
            {
                if (Directory.Exists(target.FullName) == false)
                {
                    Directory.CreateDirectory(target.FullName);
                }
                var nextTargetSubDir = target.CreateSubdirectory(diSourceSubDir.Name);
                CopyAll(diSourceSubDir, nextTargetSubDir, ignorePath);
            }
            foreach (var fi in source.GetFiles())
            {
                Log.DebugFormat(@"Copying {0}\{1}", target.FullName, fi.Name);
                var path = Path.Combine(target.ToString(), fi.Name);
                fi.CopyTo(path, true);
            }
        }

        ///<summary>
        /// Make all files and sub-directories writable.
        ///</summary>
        ///<param name="path"></param>
        public static void MakeWritable(string path)
        {
            if (Directory.Exists(path))
            {
                File.SetAttributes(path, FileAttributes.Normal);
            }

            var di = new DirectoryInfo(path);
            foreach (DirectoryInfo di2 in di.GetDirectories())
            {
                File.SetAttributes(path, FileAttributes.Normal);
                MakeWritable(Path.Combine(path, di2.Name));
            }

            // Copy each file into it's new directory.
            foreach (FileInfo fi in di.GetFiles())
            {
                String filePath = Path.Combine(path, fi.Name);
                File.SetAttributes(filePath, FileAttributes.Normal);
            }
        }

        /// <summary>
        /// check if the search item path is a subdirectory of the rootDir
        /// </summary>
        /// <param name="rootDir"></param>
        /// <param name="searchItem"></param>
        /// <returns></returns>
        public static bool IsSubdirectory(string rootDir, string searchItem)
        {
            
            var root = new DirectoryPathAbsolute(rootDir);
            var search = new DirectoryPathAbsolute(searchItem);
            return search.IsChildDirectoryOf(root);
        }

        /// <summary>
        /// Returns if the supplied path is a directory
        /// </summary>
        /// <param name="path">Path to check</param>
        /// <returns>Path is directory</returns>
        public static bool IsDirectory(string path)
        {
            return (File.GetAttributes(path) & FileAttributes.Directory) == FileAttributes.Directory;
        }

        /// <summary>
        /// Compares two directory strings
        /// </summary>
        /// <param name="rootDir"></param>
        /// <param name="searchItem"></param>
        /// <returns></returns>
        public static bool CompareDirectories(string rootDir, string searchItem)
        {
            var root = new FilePathAbsolute(Path.GetFullPath(rootDir));
            var search = new FilePathAbsolute(Path.GetFullPath(searchItem));
            return (root == search);
        }

        /// <summary>
        /// Returns a relative path string from a full path.
        /// </summary>
        /// <returns></returns>
        public static string GetRelativePath(string rootDir, string filePath)
        {
            if (rootDir == null || filePath == null) return filePath;
            if (IsSubdirectory(rootDir, filePath))
            {
                var filePathAbsolute = new FilePathAbsolute(filePath);
                var directoryPathAbsolute = new DirectoryPathAbsolute(rootDir);
                FilePathRelative filePathRelative = filePathAbsolute.GetPathRelativeFrom(directoryPathAbsolute);
                return filePathRelative.Path;
            }
            return filePath;
        }

        public static void CreateDirectoryIfNotExists(string path)
        {
            CreateDirectoryIfNotExists(path,false);
        }
        /// <summary>
        /// Create dir if not exists
        /// </summary>
        /// <param name="path"></param>
        public static void CreateDirectoryIfNotExists(string path,bool deleteIfExists)
        {
            if (Directory.Exists(path))
            {
                //replace the directory with a one
                if (deleteIfExists)
                {
                    Directory.Delete(path,true);
                    Directory.CreateDirectory(path);
                }
            }
            else
            {
                Directory.CreateDirectory(path);
            }

        }
        
        /// <summary>
        /// Initializes filesystemwatcher
        /// </summary>
        /// <returns></returns>
        public static FileSystemWatcher CreateWatcher()
        {
            // use built-in filesystemwatcher class to monitor creation/modification/deletion of files
            // example http://www.codeguru.com/csharp/csharp/cs_network/article.php/c6043/
            var watcher = new FileSystemWatcher
                              {
                                  IncludeSubdirectories = false,
                                  NotifyFilter = NotifyFilters.FileName |
                                                 NotifyFilters.LastWrite |
                                                 NotifyFilters.Size
                              };
            return watcher;
        }

        /// <summary>
        /// Checks wether the path is a valid relative path
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static bool PathIsRelative(string path)
        {
            return !Path.IsPathRooted(path);
            string reason;
            return PathHelper.IsValidRelativePath(path, out reason);
        }

        /// <summary>
        /// Replaces text in a file.
        /// </summary>
        /// <param name="filePath">Path of the text file.</param>
        /// <param name="searchText">Text to search for.</param>
        /// <param name="replaceText">Text to replace the search text.</param>
        public static void ReplaceInFile(string filePath, string searchText, string replaceText)
        {
            var reader = new StreamReader(filePath);
            string content = reader.ReadToEnd();
            reader.Close();

            content = Regex.Replace(content, searchText, replaceText);

            var writer = new StreamWriter(filePath);
            writer.Write(content);
            writer.Close();
        }

        /// <summary>
        /// Creates a temporary directory
        /// </summary>
        /// <returns>path to temporary directory</returns>
        public static string CreateTempDirectory()
        {
            string path = Path.GetFileNameWithoutExtension(Path.GetRandomFileName());
            Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), path));
            return Path.Combine(Path.GetTempPath(), path);
        }

        /// <summary>
        /// Check if one or more files can be copied
        /// 
        /// Todo: Is this function necessary? Probably better design to just copy and handle errors
        /// </summary>
        /// <param name="files"></param>
        /// <param name="targetDir"></param>
        /// <returns></returns>
        public static bool CanCopy(IEnumerable<string> files, string targetDir)
        {
            //todo check if targetdir is readonly

            //check if targetdrive has enough space
            long spaceRequired = 0;

            foreach (string file in files)
            {
                FileInfo fi = new FileInfo(file);
                spaceRequired += fi.Length;
            }
            string driveName = targetDir.Substring(0, 1);
            DriveInfo di = new DriveInfo(driveName);
            if (spaceRequired > di.AvailableFreeSpace)
            {
                return false;
            }

            //check if files exist with the same name
            foreach (string file in files)
            {
                var info = new FileInfo(Path.Combine(targetDir, Path.GetFileName(file)));
                if (info.Exists)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// check if file can be copied to another folder
        /// 
        /// Todo: Is this function necessary? Probably better design to just copy and handle errors
        /// </summary>
        /// <param name="name"></param>
        /// <param name="targetDir"></param>
        /// <returns></returns>
        public static bool CanCopy(string name, string targetDir)
        {
            string[] files = new string[] {name};
            return CanCopy(files, targetDir);
        }

        public static bool Compare(string file1Path, string file2Path)
        {
            throw new NotImplementedException();
        }



        static Regex fileFilterRegex = new Regex(@"^(\||\;)+\s*[\*\w]+\.\w+");
        /// <summary>
        /// Checks if the extension of a file belongs to the filefilter
        /// </summary>
        /// <param name="fileFilter">"My file format1 (*.ext1)|*.ext1|My file format2 (*.ext2)|*.ext2"</param>
        /// <param name="path">Path to a file or filename including extension</param>
        /// <returns></returns>
        public static bool FileMatchesFileFilterByExtension(string fileFilter, string path)
        {

           /* if (!fileFilterRegex.Match(fileFilter).Success)
            {
                throw new ArgumentException(string.Format("Invalid filefilter: {0}", fileFilter));
            }*/
            if (String.IsNullOrEmpty(Path.GetExtension(path))) return false;
            return Regex.Match(fileFilter, String.Format(@"(\||\;)+\s*\*\{0}", Path.GetExtension(path))).Success;
        }

        /// <summary>
        /// Copies the source file to the target destination; if the file
        /// already exists, it will be overwritten
        /// </summary>
        public static void CopyTo(string sourcePath, string targetPath)
        {
            var sourceFullPath = Path.GetFullPath(sourcePath);
            var targetFullPath = Path.GetFullPath(targetPath);
            if (sourceFullPath == targetFullPath)
                return;

            if (File.Exists(targetPath))
            {
                File.Delete(targetPath);
            }
            
            File.Copy(sourcePath, targetPath);
        }

        /// <summary>
        /// Deletes the given directory if it exists (not recursively)
        /// </summary>
        public static void DeleteDirectoryIfExists(string path)
        {
            if (Directory.Exists(path))
                Directory.Delete(path,true);
        }

        /// <summary>
        /// Deletes the given directory if it exists, including all 
        /// its subdirectories
        /// </summary>
        public static void DeleteDirectoryRecursivelyIfExists(string path)
        {
            if (Directory.Exists(path))
                Directory.Delete(path, true);
        }

        /// <summary>
        /// Deletes the given file if it exists
        /// </summary>
        public static void DeleteIfExists(string fileName)
        {
            if (File.Exists(fileName))
                File.Delete(fileName);
        }

        public static string GetUniqueFileNameWithPath(string existingFileName)
        {
            var newFileName = FileUtils.GetUniqueFileName(existingFileName);
            return Path.Combine(Path.GetDirectoryName(existingFileName), newFileName);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="existingFileName">The name of the file wich may include the path</param>
        /// <returns></returns>
        public static string GetUniqueFileName(string existingFileName)
        {
            if (existingFileName == null)
                throw new ArgumentNullException("existingFileName");

            var directory = Path.GetDirectoryName(existingFileName).Replace(Path.GetFileName(existingFileName), "");
            directory = string.IsNullOrEmpty(directory) ? "." : directory;

            // Declare a function to strip a file name which leaves out the extension
            Func<string, string> getFileNameWithoutExtension = Path.GetFileNameWithoutExtension;              

            var searchString = string.Format("{0}*.*", getFileNameWithoutExtension(existingFileName));

            // get all items with the same name
            var items = Directory.GetFiles(directory, searchString);

            // make a list of INameable items where the Name property will get the name of the file
            var namedItems =
                items.Select(f => new FileName { Name = getFileNameWithoutExtension(f) });

            var newName = getFileNameWithoutExtension(existingFileName);
            if (namedItems.Any())
                newName = NamingHelper.GetUniqueName(string.Format("{0} ({{0}})", newName), namedItems, typeof (INameable));

            return newName + Path.GetExtension(existingFileName);
        }

        public static IList<string> GetDirectoriesRelative(string path)
        {
            var q = from subdir in Directory.GetDirectories(path)
                    select GetRelativePath(Path.GetFullPath(path), Path.GetFullPath(subdir));
                    
            return q.ToList();
        }

        private class FileName : INameable
        {
            public string Name { get; set; }
        }
    }
}