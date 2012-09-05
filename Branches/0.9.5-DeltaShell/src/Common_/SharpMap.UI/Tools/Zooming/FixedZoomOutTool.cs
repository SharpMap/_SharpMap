﻿using SharpMap.UI.Forms;

namespace SharpMap.UI.Tools.Zooming
{
    public class FixedZoomOutTool : ZoomTool
    {
        public FixedZoomOutTool(MapControl mapControl)
            : base(mapControl)
        {
            Name = "FixedZoomOut";
        }


        public override bool AlwaysActive
        {
            get { return true; }
        }

        public override void Execute()
        {
            Map.Zoom *= 1.20; // zoom out
            MapControl.Refresh();
        }
    }
}