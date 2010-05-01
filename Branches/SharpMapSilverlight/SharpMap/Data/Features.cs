// Copyright 2010 - Paul den Dulk (Geodan)
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

using System.Collections.Generic;
using System.Collections.ObjectModel;
using SharpMap.Geometries;
using System.Collections;

namespace SharpMap.Data
{
    public interface IFeature
    {
        IGeometry Geometry { get; set; }

        object this[string key]
        {
            get;
            set;
        }
    }

    public interface IFeatures : IEnumerable<IFeature>
    {
        //todo: This should be an enumerator directly on IFeatures
        void Add(IFeature feature);
        IFeature New();
    }

    public class Features : IFeatures
    {
        private List<IFeature> features = new List<IFeature>();

        public int Count
        {
            get { return features.Count; }
        }

        public IFeature this[int index]
        {
            get { return features[index]; }
        }

        public Features()
        {
            //Perhaps this constructor should get a dictionary parameter
            //to specify the name and type of the columns
        }

        public IFeature New()
        {
            //At this point it is possible to initialize an improved version of
            //Feature with a specifed set of columns.
            return new Feature();
        }

        public void Add(IFeature feature)
        {
            features.Add(feature);
        }

        public IEnumerator<IFeature> GetEnumerator()
        {
            return features.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return features.GetEnumerator();
        }

        private class Feature : IFeature
        {
            private IGeometry _Geometry;
            private Dictionary<string, object> dictionary;

            public Feature()
            {
                dictionary = new Dictionary<string, object>();
            }

            public IGeometry Geometry
            {
                get { return _Geometry; }
                set { _Geometry = value; }
            }

            public object this[string key]
            {
                get { return dictionary[key]; }
                set { dictionary[key] = value; }
            }
        }
    }        
}

