using System;
using DelftTools.Functions.Generic;
using GeoAPI.Extensions.Coverages;
using SharpMap.Data.Providers;
using SharpMap.Rendering.Thematics;

namespace SharpMap.Layers
{
    /// <summary>
    /// Base class for NetworkCoverageSegmentLayer and NetworkCoverageLocationLayer
    /// </summary>
    public abstract class NetworkCoverageBaseLayer:VectorLayer, ICoverageLayer
    {
        protected NetworkCoverageBaseLayer()
        {
            ShowAttributeTable = false;
        }

        /// <summary>
        /// Returns the Min & Max value of the coverage, taking default value into account
        /// </summary>
        /// <returns></returns>
        protected DelftTools.Utils.Tuple<double, double> GetMinMaxValue()
        {
            double min, max;
            var variable = Coverage.Components[0];

            var @default = (double)variable.DefaultValue;

            if (variable.Values.Count > 0)
            {
                min = Math.Min(@default, (double) variable.MinValue);
                max = Math.Max(@default, (double) variable.MaxValue);
            }
            else
            {
                min = @default;
                max = @default;
            }

            if (double.IsInfinity(min) || double.IsNaN(min) ||
                double.IsInfinity(max) || double.IsNaN(max))
            {
                min = @default;
                max = @default;
            }

            return new DelftTools.Utils.Tuple<double, double>(min, max);
        }

        protected override void UpdateCurrentTheme()
        {
            if (AutoUpdateThemeOnDataSourceChanged)
            {
                // sync variable & gradient attribute name
                var gradientTheme = theme as GradientTheme;
                if (gradientTheme != null && Coverage != null)
                {
                    var variable = Coverage.Components[0] as IVariable<double>;
                    if (variable != null)
                    {
                        gradientTheme.AttributeName = variable.Name;
                    }
                }
            }

            base.UpdateCurrentTheme();
        }
        
        private NetworkCoverageFeatureCollection NetworkCoverageFeatureCollection
        {
            get { return (NetworkCoverageFeatureCollection)DataSource; }
        }
        
        public ICoverage Coverage
        {
            get
            {
                if (NetworkCoverageFeatureCollection != null)
                {
                    return NetworkCoverageFeatureCollection.NetworkCoverage;
                }
                return null;
            }
            set
            {
                if (NetworkCoverageFeatureCollection != null)
                {
                    NetworkCoverageFeatureCollection.NetworkCoverage = (INetworkCoverage)value;
                }
                CreateDefaultTheme();
            }
        }

        public virtual IComparable MinValue
        {
            get { return GetMinMaxValue().First; }
        }

        public virtual IComparable MaxValue
        {
            get { return GetMinMaxValue().Second; }
        }
        
        protected abstract void CreateDefaultTheme();
    }
}