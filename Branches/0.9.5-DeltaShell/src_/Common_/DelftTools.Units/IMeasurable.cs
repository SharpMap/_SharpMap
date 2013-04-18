using System;
using DelftTools.Utils;
using DelftTools.Utils.Data;

namespace DelftTools.Units
{
    public interface IMeasurable: INameable, IUnique<long>, ICloneable
    {
        IUnit Unit { get; set; }

        /// <summary>
        /// Value type contained in the current variable.
        /// </summary>
        Type ValueType { get; set; }

        /// <summary>
        /// Default value of the variable. Used when number of values in dependent variable changes and default values need to be added.
        /// </summary>
        object DefaultValue { get; set; }

        ///<summary>
        /// Name to be used in displaying the variable (name [unit])
        /// TODO: MIGRATE IT TO FunctionBindingList (UI level), based on ShowUnits user option
        ///</summary>
        string DisplayName { get; }

        new IMeasurable Clone();

        // TODO: add Substance
    }
}