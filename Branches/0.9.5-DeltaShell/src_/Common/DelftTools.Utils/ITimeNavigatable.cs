using System;
using System.Collections.Generic;


namespace DelftTools.Utils
{
    public enum TimeSelectionMode
    {
        /// <summary>
        /// Select a timespan
        /// </summary>
        Range,
        /// <summary>
        /// Select a single timestep
        /// </summary>
        Single
    }

    public enum SnappingMode
    {
        /// <summary>
        /// No snapping needed. Navigatable can render any timestep
        /// </summary>
        None,
        /// <summary>
        /// Values send to SetCurrentTimeSelection should be defined in the Navigatable Values
        /// </summary>
        Nearest,
        /// <summary>
        /// Takes the first value to the left. Used for intervals that are defined on the first day of the period (1 jan 2001), but that apply to the whole year or whole month, etc.
        /// </summary>
        Interval,
    }
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
        DateTime? TimeSelectionStart { get; }

        /// <summary>
        /// If set - range is selected.
        /// </summary>
        DateTime? TimeSelectionEnd { get; }

        /// <summary>
        /// If set - the date time format provider to be used when rendering this navigatable in a (time) chart.
        /// </summary>
        TimeNavigatableLabelFormatProvider CustomDateTimeFormatProvider { get; }

        /// <summary>
        /// Selects range or single time (start).
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        void SetCurrentTimeSelection(DateTime? start, DateTime? end);

        event Action CurrentTimeSelectionChanged;

        /// <summary>
        /// All possible times.
        /// </summary>
        IEnumerable<DateTime> Times { get; }

        event Action TimesChanged;

        /// <summary>
        /// Can the navigatable show a range or just a single timestep
        /// </summary>
        TimeSelectionMode SelectionMode { get; }

        /// <summary>
        /// Should values send to SetCurrentTimeSelection be snapped to a defined value
        /// </summary>
        SnappingMode SnappingMode {get;}
    }
}