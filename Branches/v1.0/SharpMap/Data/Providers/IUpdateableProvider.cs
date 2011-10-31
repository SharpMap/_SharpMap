using System.Collections.Generic;
using System.Data;

namespace SharpMap.Data.Providers
{
    /// <summary>
    /// Interface for updateable data providers
    /// </summary>
    public interface IUpdateableProvider : IProvider
    {
        /// <summary>
        /// Updates a <see cref="FeatureDataTable"/> by calling the appropriate Insert, Update and Delete functions.
        /// </summary>
        /// <param name="table">The <see cref="FeatureDataTable"/></param>
        void Update(FeatureDataTable table);
        
        /// <summary>
        /// Inserts a single <see cref="FeatureDataRow"/> to the store
        /// </summary>
        /// <param name="featureDataRow">The <see cref="FeatureDataRow"/> to insert</param>
        void Insert(FeatureDataRow featureDataRow);

        /// <summary>
        /// Inserts a series of <see cref="FeatureDataRow"/>s to the store
        /// </summary>
        /// <param name="featureDataTable">The table of <see cref="FeatureDataRow"/>s to insert.</param>
        void Insert(DataTable featureDataTable);

        /// <summary>
        /// Updates a single <see cref="FeatureDataRow"/> in the store
        /// </summary>
        /// <param name="featureDataRow">The <see cref="FeatureDataRow"/> to update.</param>
        void Update(FeatureDataRow featureDataRow);

        /// <summary>
        /// Updates a series <see cref="FeatureDataRow"/>s in the store
        /// </summary>
        /// <param name="featureDataRows">The <see cref="DataTable"/>s to update.</param>
        void Update(DataTable featureDataRows);

        /// <summary>
        /// Deletes a <see cref="FeatureDataRow"/> from the store.
        /// </summary>
        /// <param name="featureDataRow">The <see cref="FeatureDataRow"/> to delete.</param>
        void Delete(FeatureDataRow featureDataRow);

        /// <summary>
        /// Deletes a series of <see cref="FeatureDataRow"/>s from the store
        /// </summary>
        /// <param name="featureDataRows">The <see cref="DataTable"/>s to delete.</param>
        void Delete(DataTable featureDataRows);

    }
}