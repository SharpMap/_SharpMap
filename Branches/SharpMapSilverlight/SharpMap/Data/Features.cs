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

namespace SharpMap.Data
{
    public interface IFeatures
    {
        //todo: This should be an enumerator directly on IFeatures
        IEnumerable<IFeature> Items
        {
            get;
        }

        void Add(IFeature feature);
        IFeature New();
    }

    public interface IFeature
    {
        IGeometry Geometry { get; set; }

        object this[string key]
        {
            get;
            set;
        }
    }

    public class Features : IFeatures
    {
        Collection<IFeature> features = new Collection<IFeature>();

        public Features()
        {
            //Perhaps this constructor should get a dictionary parameter
            //to specify the columns
        }

        public IEnumerable<IFeature> Items
        {
            get { return features; }
        }

        public IFeature New()
        {
            //At this point it is possible to initialize a improved version of
            //Feature with a specifed set of Columns.
            return new Feature();
        }

        public void Add(IFeature feature)
        {
            features.Add(feature);
        }

        private class Feature : IFeature
        {
            private IGeometry _Geometry;
            Dictionary<string, object> dictionary;

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

