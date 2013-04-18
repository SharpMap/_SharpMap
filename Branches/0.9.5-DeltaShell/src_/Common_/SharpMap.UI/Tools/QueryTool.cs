using System;
using System.ComponentModel;
using System.Windows.Forms;
using DelftTools.Utils.PropertyBag;
using GeoAPI.Geometries;
using GisSharpBlog.NetTopologySuite.Geometries;
using SharpMap.Layers;
using SharpMap.UI.Forms;

namespace SharpMap.UI.Tools
{
    public class QueryTool : MapTool
    {
        public QueryTool(MapControl mapControl): base(mapControl)
        {
            Name = "Query";
        }

        /// <summary>
        /// Use this property to enable or disable tool. When the measure tool is deactivated, it cleans up old measurements.
        /// </summary>
        public override bool IsActive
        {
            get { return base.IsActive; }
            set
            {
                base.IsActive = value;
                if (!IsActive)
                    Clear();
            }
        }

        /// <summary>
        /// Clean up set coordinates and distances for a fresh future measurement
        /// </summary>
        private void Clear()
        {
        }

        public override void OnMouseUp(ICoordinate worldPosition, MouseEventArgs e)
        {
            if (!IsActive)
                return;

            PropertySpec ps;
            var bag = new PropertyTable();

            bag.Properties.Add(new PropertySpec("X", typeof(double), "Position"));
            bag.Properties.Add(new PropertySpec("Y", typeof(double), "Position"));
            bag["X"] = worldPosition.X;
            bag["Y"] = worldPosition.X;

            ICoordinate coordinate = new Coordinate(worldPosition.X, worldPosition.Y);
            foreach (var layer in Map.Layers)
            {
                if (layer is ICoverageLayer && layer.Envelope.Contains(coordinate))
                {

                    var value = ((ICoverageLayer) layer).Coverage.Evaluate(coordinate);

                    if(value == null)
                    {
                        bag.Properties.Add(new PropertySpec(layer.Name, typeof(string), "Values"));
                        bag[layer.Name] = "<empty>";
                    }
                    else
                    {
                        bag.Properties.Add(new PropertySpec(layer.Name, value.GetType(), "Values"));
                        bag[layer.Name] = value;
                    }
                }
            }

            if (ResultChanged != null)
            {
                var args = new QueryResultEventArgs();
                args.Result = bag;
                ResultChanged(this, args);
            }

            base.OnMouseDown(worldPosition, e);
        }

        public event EventHandler<QueryResultEventArgs> ResultChanged;

        public override void ActiveToolChanged(IMapTool newTool)
        {
            // TODO: It seems this is never called, so it is also cleared when the IsActive property is (re)set
            Clear();
            base.ActiveToolChanged(newTool);
        }
    }
}