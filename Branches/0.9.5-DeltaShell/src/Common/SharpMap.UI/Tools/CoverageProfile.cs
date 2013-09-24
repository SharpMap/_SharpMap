using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using DelftTools.Functions;
using DelftTools.Functions.Generic;
using DelftTools.Utils;
using DelftTools.Utils.Reflection;

using GeoAPI.Extensions.Coverages;
using GeoAPI.Extensions.Feature;
using GeoAPI.Geometries;

using GisSharpBlog.NetTopologySuite.LinearReferencing;

using SharpMap.Layers;

namespace SharpMap.UI.Tools
{
    /// <summary>
    /// Feature implementation to support a profile on a grid coverage
    /// </summary>
    public class CoverageProfile : UserControl, IFeature
    {
        public long Id { get; set; }

        public Type GetEntityType()
        {
            return GetEntityType();
        }

        public IFeatureAttributeCollection Attributes { get; set; }

        private ICoverageLayer coverageLayer;

        public ICoverageLayer CoverageLayer
        {
            get { return coverageLayer; } 
            set
            {
                coverageLayer = value;
                
                if(coverageLayer is ITimeNavigatable)
                {
                    ((ITimeNavigatable) coverageLayer).CurrentTimeSelectionChanged += () => Geometry = geometry; // updates grid profile using current layer time
                }
            }
        }
 
        private IGeometry geometry;
        public IGeometry Geometry 
        { 
            get { return geometry; } 
            set 
            { 
                if (!(value is ILineString))
                {
                    throw new ArgumentException("CoverageProfile only supports LineString geometries.");
                }
                geometry = value;
                if (null != profileData)
                {
                    var time = GetCurrentTime();
                    UpdateProfileFunctionValues(profileData, CoverageLayer.Coverage, geometry as ILineString, time);
                }
            }
 
        }

        private Function profileData;
        public Function ProfileData 
        {
            get
            {
                if (null == CoverageLayer)
                {
                    return null;
                }
                if (null == Geometry)
                {
                    return null;
                }

                DateTime? time = GetCurrentTime();
                if (null == profileData)
                {
                    profileData = GetCoverageValues(CoverageLayer.Coverage, geometry as ILineString, time);
                    
                }
                else
                {
                    UpdateProfileFunctionValues(profileData, CoverageLayer.Coverage, geometry as ILineString, time);
                }
                return profileData; 
            }
            set 
            { 
                profileData = value; 
            }
        }

        /// <summary>
        /// Defines the gridProfile function
        /// </summary>
        /// <param name="grid"></param>
        /// <param name="polyline"></param>
        /// <param name="resolution">Defines the sample resolution along <paramref name="polyline"/> (each resolution step a sample).
        /// Null will cause 101 samples to be take along the line uniformly.</param>
        /// <exception cref="ArgumentException">When <paramref name="resolution"/> is 0.0 or less.</exception>
        public static Function GetCoverageValues(ICoverage grid, ILineString polyline, DateTime? time, double? resolution = null)
        {
            var function = new Function { Name = grid.Name };

            function.Arguments.Add(new Variable<double>("offset"));
            function.Components.Add((IVariable)TypeUtils.CreateGeneric(typeof(Variable<>), grid.Components[0].ValueType, new object[] { "value" }));
            function.Components[0].NoDataValues = grid.Components[0].NoDataValues;

            if (null == polyline)
            {
                return function;
            }

            UpdateProfileFunctionValues(function, grid, polyline, time, resolution);

            return function;
        }

        /// <summary>
        /// Fills gridvalues function with profiledata based on profileline over the grid
        /// </summary>
        /// <param name="function"></param>
        /// <param name="coverage"></param>
        /// <param name="polyline"></param>
        /// <param name="resolution">Defines the sample resolution along <paramref name="polyline"/> (each resolution step a sample).
        /// Null will cause 101 samples to be take along the line uniformly.</param>
        /// <exception cref="ArgumentException">When <paramref name="resolution"/> is 0.0 or less.</exception> 
        public static void UpdateProfileFunctionValues(Function function, ICoverage coverage, ILineString polyline, DateTime? time, double? resolution = null)
        {
            // when coverage is empty (has no times), we cannot call Evaluate below...
            if (time != null && time.Equals(default(DateTime))) return;

            function.Clear();
            double offset = 0;
            double step = resolution ?? polyline.Length / 100;
            var gridProfileCoordinates = GetGridProfileCoordinates(polyline, step).ToArray();
            foreach (ICoordinate coordinate in gridProfileCoordinates)
            {
                var value = time != null ? coverage.Evaluate(coordinate, time.Value) : coverage.Evaluate(coordinate);
                if (value == null)
                {
                    offset += step;
                    continue;
                }
                function[offset] = value;
                offset += step;
            }
        }

        /// <summary>
        /// return the coordinates along the gridProfile at stepSize intervals.
        /// </summary>
        /// <param name="gridProfile"></param>
        /// <param name="stepSize"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException">When <paramref name="stepSize"/> is 0.0 or less.</exception>
        public static IEnumerable<ICoordinate> GetGridProfileCoordinates(ILineString gridProfile, double stepSize)
        {
            var lengthIndexedLine = new LengthIndexedLine(gridProfile);
            if (stepSize <= 0) throw new ArgumentException("Stepsize too small", "stepSize");

            var count = (int)((gridProfile.Length / stepSize) + 1);
            for (int i = 0; i < count; i++)
            {
                yield return (ICoordinate)lengthIndexedLine.ExtractPoint(i * stepSize).Clone();
            }
        }

        private DateTime? GetCurrentTime()
        {
            var coverage = CoverageLayer.Coverage;
            if (coverage.IsTimeDependent && CoverageLayer is ITimeNavigatable)
            {
                var timeNavigatable = CoverageLayer as ITimeNavigatable;
                return timeNavigatable.TimeSelectionStart;
            }
            
            return null;
        }

        public object Clone()
        {
            throw new NotImplementedException();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // GridProfile
            // 
            this.Name = "CoverageProfile";
            this.Size = new System.Drawing.Size(266, 150);
            this.ResumeLayout(false);

        }
    }
}