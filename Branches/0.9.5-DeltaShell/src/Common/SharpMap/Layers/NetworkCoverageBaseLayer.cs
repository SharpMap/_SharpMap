using System;
using System.Collections.Generic;
using DelftTools.Functions;
using DelftTools.Functions.Generic;
using DelftTools.Utils;
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
        /// <summary>
        /// Returns the Min & Max value of the coverage, taking default value into account
        /// </summary>
        /// <returns></returns>
        protected Tuple<double, double> GetMinMaxValue()
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

            return new Tuple<double, double>(min, max);
        }
        
        protected override void UpdateCurrentTheme()
        {
            base.UpdateCurrentTheme();

            if (!AutoUpdateThemeOnDataSourceChanged)
                return;//we don't have to update

            var gradientTheme = theme as GradientTheme;
            
            if (gradientTheme != null && NetworkCoverageFeatureCollection != null)
            {
                var variable = NetworkCoverageFeatureCollection.NetworkCoverage.Components[0] as IVariable<double>;
                if (variable != null)
                {
                    var minMaxTuple = GetMinMaxValue();

                    gradientTheme.Min = minMaxTuple.First;
                    gradientTheme.Max = minMaxTuple.Second;
                    gradientTheme.UpdateThemeItems();
                }
            }
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
        protected override void OnLayerPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            //if the component name changes we change the attribute name
            if ((sender is IVariable) && (Coverage != null) && (Coverage.Components.Contains((IVariable)sender)) && (e.PropertyName == "Name"))
            {
                if (Theme as GradientTheme != null) 
                {
                    ((GradientTheme)Theme).AttributeName = string.Format("{0}", Coverage.Components[0].Name);
                }
            }

            base.OnLayerPropertyChanged(sender, e);
        }
        
        
        protected abstract void CreateDefaultTheme();
    }
}