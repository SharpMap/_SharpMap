using System.Windows.Forms;
using GeoAPI.Extensions.Feature;
using GeoAPI.Geometries;
using log4net;
using SharpMap.Layers;
using ToolTip=System.Windows.Forms.ToolTip;

namespace SharpMap.UI.Tools
{
    public class ToolTipTool: MapTool
    {
        private static readonly ILog log = LogManager.GetLogger(typeof (ToolTipTool));

        ToolTip toolTip;
        private string toolTipText;

        private Layer layer;
        private IFeature feature;

        public ToolTipTool()
        {
            Name = "ToolTipTool";
            toolTip = new ToolTip();
            toolTip.ReshowDelay = 500;
            toolTip.InitialDelay = 500;
            toolTip.AutoPopDelay = 2500;
        }

        public override bool IsActive
        {
            get { return true; } // always active
            set { }
        }

        public override void OnMouseMove(ICoordinate worldPosition, MouseEventArgs e)
        {
/*
            feature = null;

            feature = FindNearestFeature(worldPosition, 5, out layer);

            if (feature == null || layer == null)
            {
                toolTip.Active = false;
                return;
            }

            string message = "Feature: " + feature + "\nLayer: " + layer.Name;

            if (toolTipText != message)
            {
                toolTip.SetToolTip((Control) MapControl, toolTipText);
                toolTip.Active = false;
                toolTip.Active = true;
            }
*/
        }
    }
}
