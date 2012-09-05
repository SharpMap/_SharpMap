using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using DelftTools.Functions.Generic;
using DelftTools.Utils;
using GeoAPI.Extensions.Coverages;
using GeoAPI.Geometries;
using log4net;
using SharpMap.Data.Providers;
using SharpMap.Rendering.Thematics;
using SharpMap.Styles;

namespace SharpMap.Layers
{
    public class NetworkCoverageLayer : LayerGroup, INetworkCoverageLayer,ITimeNavigatable
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(NetworkCoverageLayer));

        private INetworkCoverage networkCoverage;

        public NetworkCoverageLayer(): this("NetworkCoverage")
        {
        }

        public NetworkCoverageLayer(string layername) : base(layername)
        {
            CreateDefaultLayers();
        }

        public virtual INetworkCoverage NetworkCoverage
        {
            get { return networkCoverage; }
            set 
            { 
                networkCoverage = value;
                networkCoverage.ValuesChanged += delegate { RenderRequired = true; };
                (networkCoverage as INotifyPropertyChanged).PropertyChanged += delegate { RenderRequired = true; };
                //renderedCoverage = null;
                
                Initialize();
                if (networkCoverage.IsTimeDependent)
                {
                    TimeSelectionStart = GetDefaultTimeFromCoverage(networkCoverage);
                }
            }
        }
        
        private static DateTime GetDefaultTimeFromCoverage(ICoverage coverage)
        {
            //if no time is specified we set a default (first or minvalue)
            return coverage.Time.AllValues.Count > 0 ? coverage.Time.AllValues[0] : DateTime.MinValue;
        }

        public virtual ICoverage Coverage
        {
            get { return NetworkCoverage; }
            set { NetworkCoverage = (INetworkCoverage)value; }
        }

        private DateTime? _timeSelectionStart;
        //MOVE this into interface ISO SetCurrentTime
        public virtual DateTime? TimeSelectionStart
        {
            get { return _timeSelectionStart; }
            set
            {
                _timeSelectionStart = value;
                //set it in the 'child' layers
                //update child datasources.
                if ((LocationLayer.DataSource as NetworkCoverageFeatureCollection) != null)
                {
                    (LocationLayer.DataSource as NetworkCoverageFeatureCollection).CurrentTime = _timeSelectionStart;
                }
                if ((SegmentLayer.DataSource as NetworkCoverageFeatureCollection) != null)
                {
                    (SegmentLayer.DataSource as NetworkCoverageFeatureCollection).CurrentTime = _timeSelectionStart;
                }
                RenderRequired = true;
            }
        }

        public virtual DateTime? TimeSelectionEnd
        {
            get { return null; }
            set { throw new NotImplementedException(); }
        }

        public virtual IEnumerable<DateTime> Times
        {
            get
            {
                if(NetworkCoverage.IsTimeDependent)
                {
                    return NetworkCoverage.Time.Values; 
                }

                return Enumerable.Empty<DateTime>();
            }
        }


        /// <summary>
        /// Late initialization, when Layers and Network is set.
        /// </summary>
        private void InitializeFeatureProviders()
        {
            if (NetworkCoverage == null || Layers.Count <= 0)
            {
                return;
            }


            //renderedCoverage = GetRenderedCoverage(NetworkCoverage);
            LocationLayer.DataSource = new NetworkCoverageFeatureCollection
                                           {
                                               NetworkCoverage = NetworkCoverage, 
                                               NetworkCoverageFeatureType = NetworkCoverageFeatureType.Locations
                                           };

            if (NetworkCoverage.SegmentGenerationMethod != SegmentGenerationMethod.None)
            {
                SegmentLayer.DataSource = new NetworkCoverageFeatureCollection
                                              {
                                                  NetworkCoverage = NetworkCoverage,
                                                  NetworkCoverageFeatureType = NetworkCoverageFeatureType.Segments
                                              };
            }
        }

        private void CreateDefaultLayers()
        {
            Layers.Clear();
            Layers.Add(new NetworkCoverageLocationLayer()
                           {
                               Name = "Locations",
                               Enabled = true,
                               Style = new VectorStyle {GeometryType = typeof (IPoint)}
                           }); //enables notification

            Layers.Add(new NetworkCoverageSegmentLayer
                           {
                               Name = "Cells",
                               Enabled = true,
                               Style = new VectorStyle
                                           {
                                               GeometryType = typeof (ILineString),
                                               Fill = new SolidBrush(Color.Tomato),
                                               Line = new Pen(Color.SteelBlue, 3)
                                           }
                           });
            /*Layers.Add(new NetworkCoverageSegmentLayer());
            Layers.Add(new NetworkCoverageSegmentLayer());*/
        }

        public virtual NetworkCoverageLocationLayer LocationLayer 
        {
            get { return (NetworkCoverageLocationLayer)Layers[0]; }
            set { Layers[0] = value; }
        }

        public virtual NetworkCoverageSegmentLayer SegmentLayer
        {
            get 
            {
                return (NetworkCoverageSegmentLayer)Layers[1];
            }
            
            set
            {
                Layers[1] = value;
            }
        }

        public override bool ReadOnly
        {
            get
            {
                if (NetworkCoverage != null)
                {
                    return !NetworkCoverage.IsEditable;
                }
                return false;
            }
        }

        private void CreateDefaultTheme()
        {
            // If there was no theme attached to the layer yet, generate a default theme
            if (Theme != null || networkCoverage == null || networkCoverage.Locations == null)
            {
                return;
            }

            //looks like min/max should suffice
            IList<double> values = GetAllValues();
            if (null == values)
            {
                return;
            }

            var minValue = networkCoverage.DefaultValue; // ???
            var maxValue = minValue;
            
            // NOTE: we should be getting al the values here!
            var featureValues = new List<double>(values);
            if (0 != featureValues.Count)
            {
                featureValues.Sort();
                minValue = featureValues[0];
                maxValue = featureValues[featureValues.Count - 1];
            }

            if (minValue == maxValue)
            {
                // Only a single value, so no gradient theme needed/wanted: create a 'blue' single feature theme
                if (networkCoverage.SegmentGenerationMethod != SegmentGenerationMethod.None)
                {
                    SegmentLayer.Theme = ThemeFactory.CreateSingleFeatureTheme(SegmentLayer.Style.GeometryType,
                                                                               Color.PaleTurquoise, 6);
                }
                LocationLayer.Theme = ThemeFactory.CreateSingleFeatureTheme(LocationLayer.Style.GeometryType, Color.Blue, 10);
            }
            else
            {
                int numberOfClasses = 12;
                if (networkCoverage.SegmentGenerationMethod != SegmentGenerationMethod.None)
                {
                    // Create flashy gradient theme
                    if (NetworkCoverage.SegmentGenerationMethod == SegmentGenerationMethod.SegmentPerLocation)
                    {
                        SegmentLayer.Theme = ThemeFactory.CreateGradientTheme(Coverage.Components[0].Name, SegmentLayer.Style,
                                                                              ColorBlend.Rainbow7,
                                                                              (float) minValue, (float) maxValue,
                                                                              10, 25, false,
                                                                              false, numberOfClasses);
                    }
                    else
                    {
                        SegmentLayer.Theme = ThemeFactory.CreateGradientTheme("Offset",SegmentLayer.Style,
                                                                              ColorBlend.Rainbow7,
                                                                              (float) minValue, (float) maxValue,
                                                                              10, 25, false,
                                                                              false, numberOfClasses);
                    }
                }

                LocationLayer.Theme = ThemeFactory.CreateGradientTheme(Coverage.Components[0].Name, LocationLayer.Style,
                                                                     ColorBlend.Rainbow7,
                                                                     (float)minValue, (float)maxValue,
                                                                      12, 25, false, false, numberOfClasses);
            }
        }

        private IMultiDimensionalArray<double> GetAllValues()
        {
            //TODO: work out for different types. Maybe introduce allvalues on IFunction
            if (networkCoverage.Components[0] is IVariable<double>)
            {
                return (networkCoverage.Components[0] as IVariable<double>).AllValues;
            }
            return null;
        }

        
        private void Initialize()
        {
            // If theme is not set generate default one.
            if (SegmentLayer.Theme == null && networkCoverage.SegmentGenerationMethod != SegmentGenerationMethod.None)
            {
                CreateDefaultTheme();
            }

            if (networkCoverage.SegmentGenerationMethod == SegmentGenerationMethod.None && SegmentLayer != null) // ugly, refactor it
            {
                Layers.Remove(SegmentLayer);
            }
            InitializeFeatureProviders();
        }

        
    }
}
