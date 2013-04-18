using System;
using System.Collections.Generic;

namespace DelftTools.Utils
{
    /// <summary>
    /// Object with time dependency. For example a layer or a view
    /// 
    /// *------------*-----------*----------------*-----------*
    ///             [                      ]
    /// </summary>
    public interface ITimeNavigatable
    {
        /// <summary>
        /// Used as current time or as selection start time in case of range selection.
        /// </summary>
        DateTime? TimeSelectionStart { get; set; }

        /// <summary>
        /// If set - range is selected.
        /// </summary>
        DateTime? TimeSelectionEnd { get; set; }

        /// <summary>
        /// All possible times.
        /// </summary>
        IEnumerable<DateTime> Times { get; }
    }
}