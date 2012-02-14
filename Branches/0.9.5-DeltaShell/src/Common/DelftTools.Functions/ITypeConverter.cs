using System;
using DelftTools.Utils.Data;

namespace DelftTools.Functions
{
    /// <summary>
    /// Interface to convert types. 
    /// </summary>
    public interface ITypeConverter : IUnique<long>
    {
        //Types as stored in Netcdf for example
        Type[] StoreTypes { get; }
        
        //Type used in .Net
        Type ConvertedType { get; }
        
        /// <summary>
        /// Gets the names of the store types
        /// </summary>
        string[] VariableNames { get; }

        /// <summary>
        /// Returns a tuple of values in NetCdf. TODO: refactor it to convert list of object instead of a single object
        /// </summary>
        object[] ConvertToStore(object source);

        /// <summary>
        /// Creates instance using tuple of values. TODO: refactor it to convert list of object instead of a single object
        /// </summary>
        /// <param name="o"></param>
        /// <returns></returns>
        object ConvertFromStore(object o); // HACK: parameter is always used as an array [] (values of properties)
    }
}