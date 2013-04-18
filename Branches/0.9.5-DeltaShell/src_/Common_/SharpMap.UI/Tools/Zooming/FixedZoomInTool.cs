using SharpMap.UI.Forms;

namespace SharpMap.UI.Tools.Zooming
{
    public class FixedZoomInTool : ZoomTool
    {
        public FixedZoomInTool(MapControl mapControl)
            : base(mapControl)
        {
            Name = "FixedZoomIn";
        }


        public override bool AlwaysActive
        {
            get { return true; }
        }

        public override void Execute()
        {
            Map.Zoom *= 0.80; // zoom in
            MapControl.Refresh();
        }
    }
}