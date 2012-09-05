using System;
using System.Collections.Generic;
using System.Linq;
using DelftTools.Functions.Filters;
using DelftTools.Utils;

namespace DelftTools.Functions
{
    public class TimeArgumentNavigatable : ITimeNavigatable
    {
        private readonly VariableValueFilter<DateTime> filter;

        public TimeArgumentNavigatable(VariableValueFilter<DateTime> filter)
        {
            if (filter == null)
            {
                throw new InvalidOperationException("Filter should not be null");
            }
            this.filter = filter;
            
            filter.Variable.ValuesChanged += VariableValuesChanged;
        }

        void VariableValuesChanged(object sender, FunctionValuesChangingEventArgs e)
        {
            if (TimesChanged != null)
            {
                TimesChanged();
            }
        }

        public DateTime? TimeSelectionStart
        {
            get { return filter.Values.Min(); }
        }

        public DateTime? TimeSelectionEnd
        {
            get { return filter.Values.Max(); }
        }

        public TimeNavigatableLabelFormatProvider CustomDateTimeFormatProvider
        {
            get { return null; }
        }

        public void SetCurrentTimeSelection(DateTime? start, DateTime? end)
        {
            filter.Values[0] = start.Value;
            OnTimeSelectionChanged();
        }

        public event Action CurrentTimeSelectionChanged;

        private void OnTimeSelectionChanged()
        {
            if (TimeSelectionChanged != null)
            {
                TimeSelectionChanged(this, EventArgs.Empty);
            }
        }

        public IEnumerable<DateTime> Times
        {
            get { return (IEnumerable<DateTime>)filter.Variable.Values; }
        }

        public event Action TimesChanged;

        public TimeSelectionMode SelectionMode
        {
            get { return TimeSelectionMode.Single; }
        }

        public SnappingMode SnappingMode
        {
            get { return SnappingMode.Nearest; }
        }

        /// <summary>
        /// Occurs when the time selection changed.
        /// </summary>
        public event EventHandler TimeSelectionChanged;
    }
}