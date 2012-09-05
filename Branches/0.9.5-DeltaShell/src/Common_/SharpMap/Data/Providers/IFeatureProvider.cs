// Copyright 2005, 2006 - Morten Nielsen (www.iter.dk)
//
// This file is part of SharpMap.
// SharpMap is free software; you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
// 
// SharpMap is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.

// You should have received a copy of the GNU Lesser General Public License
// along with SharpMap; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA 

using System;
using System.Collections;
using System.Collections.Generic;
using DelftTools.Utils.Data;
using GeoAPI.Extensions.Feature;
using GeoAPI.Geometries;

namespace SharpMap.Data.Providers
{
    

	/// <summary>
	/// Interface for data providers
    /// TODO: move all editing-related properties / functions into a separate interface, IFeatureEditor, IFeatureCollection?
    /// </summary>
    public interface IFeatureProvider : IUnique<long>, IDisposable
	{
        /// <summary>
        /// Type of the features provided.
        /// </summary>
        Type FeatureType { get; }

        IList Features { get; }

        /// <summary>
        /// Adds a new feature to the feature storage using geometry.
        /// </summary>
        /// <param name="geometry"></param>
	    IFeature Add(IGeometry geometry);

        Func<IFeatureProvider, IGeometry, IFeature> AddNewFeatureFromGeometryDelegate { get; set; }


        /// <summary>
		/// Gets the features within the specified <see cref="IEnvelope"/>
		/// </summary>
		/// <param name="bbox"></param>
        /// <returns>Features within the specified <see cref="IEnvelope"/></returns>
        ICollection<IGeometry> GetGeometriesInView(IEnvelope bbox, double minGeometrySize);

		/// <summary>
        /// Returns all objects whose <see cref="IEnvelope"/> intersects 'envelope'.
		/// </summary>
		/// <remarks>
		/// This method is usually much faster than the QueryFeatures method, because intersection tests
        /// are performed on objects simplifed by their <see cref="IEnvelope"/>, and using the Spatial Index
		/// </remarks>
		/// <param name="envelope">Box that objects should intersect</param>
		/// <returns></returns>
        ICollection<int> GetObjectIDsInView(IEnvelope envelope);

		/// <summary>
		/// Returns the geometry corresponding to the Object ID
		/// </summary>
		/// <param name="oid">Object ID</param>
		/// <returns>geometry</returns>
		IGeometry GetGeometryByID(int oid);

        /// <summary>
        /// Returns the data associated with all the geometries that are (partly) covered by 'geom'
        /// </summary>
        /// <param name="boundingGeometry"></param>
        /// <returns></returns>
        IList GetFeatures(IGeometry boundingGeometry);

		/// <summary>
		/// Returns the data associated with all the geometries that are intersected by 'geom'
		/// </summary>
		/// <param name="box">Geometry to intersect with</param>
        IList GetFeatures(IEnvelope box);

		/// <summary>
		/// Returns the number of features in the dataset
		/// </summary>
		/// <returns>number of features</returns>
		[Obsolete]
		int GetFeatureCount();

		/// <summary>
		/// Returns a <see cref="SharpMap.Data.FeatureDataRow"/> based on a RowID
		/// 
		/// </summary>
		/// <param name="index"></param>
		/// <returns>datarow</returns>
        [Obsolete]
        IFeature GetFeature(int index);

        /// <summary>
        /// Returns true if feature belongs to provider.
        /// </summary>
        /// <param name="feature"></param>
        /// <returns></returns>
        [Obsolete]
        bool Contains(IFeature feature);

        /// <summary>
        /// Returns the index of the feature in the internal list, or -1 if it does not contain the feature
        /// </summary>
        /// <param name="feature"></param>
        /// <returns></returns>
        [Obsolete]
        int IndexOf(IFeature feature);

		/// <summary>
        /// <see cref="IEnvelope"/> of dataset
		/// </summary>
		/// <returns>boundingbox</returns>
		IEnvelope GetExtents();

		/// <summary>
		/// The spatial reference ID (CRS)
		/// </summary>
		int SRID { get; set;}

        
	}
}
