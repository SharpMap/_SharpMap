using System.Collections.Generic;

namespace DelftTools.Utils.IO
{
    /// <summary>
    /// Allows to open entity from an existing file or create a new one using <seealso cref="Path"/>.
    /// TODO: make name more generic (but still intuitive) since it can also be web resource, database, etc (or better define separete interface)
    /// </summary>
    public interface IFileBased
    {
        /// <summary>
        /// Gets the path of the opened file (main).
        /// </summary>
        string Path { get; set; } // TODO: try to remove setter from here

        /// <summary>
        /// Gets all files which belong to this object. In many cases this is equal to <see cref="Path"/>.
        /// </summary>
        IEnumerable<string> Paths { get; }

        /// <summary>
        /// Gets a value indicating the file is read and the 
        /// </summary>
        bool IsOpen { get; }

        /// <summary>
        /// Delete the file of the path if exists
        /// </summary>
        /// <param name="path"></param>
        void CreateNew(string path);

        void Close();

        /// <summary>
        /// Set Path and Open file
        /// </summary>
        /// <param name="path"></param>
        void Open(string path);

        /// <summary>
        /// Copies the item and related files to the specified path.
        /// </summary>
        /// <param name="newPath"></param>
        void CopyTo(string newPath);

        /// <summary>
        /// Relocate to reconnects the item to the given path. Does NOT perform copyTo.
        /// </summary>
        /// <param name="newPath"></param>
        void SwitchTo(string newPath);

        /// <summary>
        /// Deletes current file based item (including all dependent files).
        /// </summary>
        void Delete();
    }
}
