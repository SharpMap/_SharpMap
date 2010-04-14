using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using SharpMap.Geometries;

namespace SharpMap.Data.Providers
{
    public interface IProvider
    {
        /// <summary>
        /// The spatial reference ID (CRS)
        /// </summary>
        int SRID { get; set; }

        /// <summary>
        /// <see cref="SharpMap.Geometries.BoundingBox"/> of dataset
        /// </summary>
        /// <returns>boundingbox</returns>
        BoundingBox GetExtents();
    }
}
