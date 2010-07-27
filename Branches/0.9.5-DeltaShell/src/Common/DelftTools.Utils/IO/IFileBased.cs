using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DelftTools.Utils.IO
{
    /// <summary>
    /// Allows to open entity from an existing file or create a new one using <seealso cref="Path"/>.
    /// TODO: make name more generic (but still intuitive) since it can also be web resource, database, etc
    /// </summary>
    public interface IFileBased
    {

        /// <summary>
        /// Just sets the Path
        /// </summary>
        string Path { get; set; }

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
        /// Open Path (if has been set)
        /// </summary>
        void Open();

        bool IsOpen { get; }

        /// <summary>
        /// Relocate to reconnects the item to the given path. Does NOT perform copyTo.
        /// </summary>
        /// <param name="newPath"></param>
        void RelocateTo(string newPath);

        /// <summary>
        /// Copies the item and related files to the specified path
        /// </summary>
        /// <param name="newPath"></param>
        void CopyTo(string newPath);

        /// <summary>
        /// Connects to a copied file without resetting the object.
        /// </summary>
        void ReConnect();
    }
}
