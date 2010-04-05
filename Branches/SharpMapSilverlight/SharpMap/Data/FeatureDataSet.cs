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
using System.Xml;
using System.Runtime.Serialization;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using SharpMap.Geometries;


namespace SharpMap.Data
{
    /// <summary>
    /// Represents one feature table of in-memory spatial data. 
    /// </summary>
    public class FeatureDataTable 
    {
        List<FeatureDataRow> rows = new List<FeatureDataRow>();

        public IList<FeatureDataRow> Rows
        {
            get { return rows; }
        }

        public FeatureDataRow NewRow()
        {
            return new FeatureDataRow();
        }
    }

    /// <summary>
    /// Represents a row of data in a FeatureDataTable.
    /// </summary>
    public class FeatureDataRow : Dictionary<string, object>
    {
        private IGeometry _Geometry;

        /// <summary>
        /// The geometry of the current feature
        /// </summary>
        public IGeometry Geometry
        {
            get { return _Geometry; }
            set { _Geometry = value; }
        }
    }

}

