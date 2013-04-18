using System.Reflection;
using System.Drawing;
using System.Windows.Forms;
using DelftTools.Utils;
using GeoAPI.Geometries;
using SharpMap.Layers;
using GeoAPI.Extensions.Feature;
using SharpMap.Styles;
using SharpMap.UI.Mapping;
using SharpMap.Converters.Geometries;

namespace SharpMap.UI.Editors
{
    public class PointEditor : FeatureEditor
    {

        public PointEditor(ICoordinateConverter coordinateConverter, ILayer layer, IFeature feature, VectorStyle vectorStyle, IEditableObject editableObject)
            : base(coordinateConverter, layer, feature, vectorStyle, editableObject)
        {
            Init();
        }

        private void Init()
        {
            CreateTrackers();
        }

        protected override void CreateTrackers()
        {
            IPoint point = GeometryFactory.CreatePoint((ICoordinate)SourceFeature.Geometry.Coordinates[0].Clone());
            trackers.Add(new TrackerFeature(this, point, 0, /*SelectedImageTracker*/
                                            (VectorStyle != null ) ? TrackerSymbolHelper.GenerateComposite(new Pen(Color.Blue),
                                                                                                           new SolidBrush(Color.DarkBlue),
                                                                                                           VectorStyle.Symbol.Width,
                                                                                                           VectorStyle.Symbol.Height,
                                                                                                           6,
                                                                                                           6) : null));
            trackers[0].Selected = true;
        }

        public override Cursor GetCursor(ITrackerFeature trackerFeature)
        {
            if (trackers[0] == trackerFeature)
            {
                // ReSharper disable AssignNullToNotNullAttribute
                return new Cursor(Assembly.GetExecutingAssembly().GetManifestResourceStream("SharpMap.UI.Cursors.moveTracker.cur"));
                // ReSharper restore AssignNullToNotNullAttribute
            }
            return null;
        }
        public override void Select(ITrackerFeature trackerFeature, bool select)
        {
            if (trackerFeature.Selected != select)
            {
                trackerFeature.Selected = select;
                if (null != VectorStyle)
                {
                    if (select)
                    {
                        trackerFeature.Bitmap = TrackerSymbolHelper.GenerateComposite(new Pen(Color.Blue),
                                                                                      new SolidBrush(Color.DarkBlue),
                                                                                      VectorStyle.Symbol.Width,
                                                                                      VectorStyle.Symbol.Height,
                                                                                      6,
                                                                                      6);
                    }
                    else
                    {
                        trackerFeature.Bitmap = TrackerSymbolHelper.GenerateComposite(new Pen(Color.Lime),
                                                                                      new SolidBrush(Color.Green),
                                                                                      VectorStyle.Symbol.Width,
                                                                                      VectorStyle.Symbol.Height,
                                                                                      6,
                                                                                      6);
                    }
                }
            }
        }
    }
}