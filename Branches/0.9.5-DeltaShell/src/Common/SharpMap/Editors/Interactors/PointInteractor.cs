using System.Reflection;
using System.Drawing;
using System.Windows.Forms;
using DelftTools.Utils.Editing;
using GeoAPI.Geometries;
using SharpMap.Api;
using SharpMap.Api.Editors;
using SharpMap.Layers;
using GeoAPI.Extensions.Feature;
using SharpMap.Styles;
using SharpMap.Converters.Geometries;

namespace SharpMap.Editors.Interactors
{
    public class PointInteractor : FeatureInteractor
    {

        public PointInteractor(ILayer layer, IFeature feature, VectorStyle vectorStyle, IEditableObject editableObject)
            : base(layer, feature, vectorStyle, editableObject)
        {
        }

        protected override void CreateTrackers()
        {
            IPoint point = GeometryFactory.CreatePoint((ICoordinate) SourceFeature.Geometry.Coordinates[0].Clone());
            Trackers.Add(new TrackerFeature(this, point, 0, /*SelectedImageTracker*/
                                            (VectorStyle != null)
                                                ? TrackerSymbolHelper.GenerateComposite(new Pen(Color.Blue),
                                                                                        new SolidBrush(Color.DarkBlue),
                                                                                        VectorStyle.Symbol.Width,
                                                                                        VectorStyle.Symbol.Height,
                                                                                        6,
                                                                                        6)
                                                : null));
            Trackers[0].Selected = true;
        }

        public override Cursor GetCursor(TrackerFeature trackerFeature)
        {
            if (Trackers[0] == trackerFeature)
            {
                // ReSharper disable AssignNullToNotNullAttribute
                return new Cursor(Assembly.GetExecutingAssembly().GetManifestResourceStream("SharpMap.Editors.Cursors.Move.cur"));
                // ReSharper restore AssignNullToNotNullAttribute
            }
            return null;
        }

        public override void SetTrackerSelection(TrackerFeature trackerFeature, bool select)
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