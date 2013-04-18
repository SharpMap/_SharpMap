using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using GeoAPI.Extensions.Feature;
using SharpMap.Layers;
using SharpMap.UI.Snapping;
using SharpMap.UI.Tools;
using SharpMap.UI.Tools.Zooming;
using GeoAPI.Geometries;

namespace SharpMap.UI.Forms
{
    public delegate void EditorBeforeContextMenuEventHandler(object sender, EditorContextMenuEventArgs e);

    public interface IMapControl
    {
        Map Map { get; set; }

        Image Image { get; }

        IList<IMapTool> Tools { get; }

        ZoomHistoryTool ZoomHistoryTool { get; }
        PanZoomTool PanZoomTool { get; }
        PanZoomUsingMouseWheelTool WheelPanZoomTool { get; }
        ZoomUsingRectangleTool RectangleZoomTool { get; }
        FixedZoomInTool FixedZoomInTool { get; }
        FixedZoomOutTool FixedZoomOutTool { get; }
        MoveTool MoveTool { get; }
        MoveTool LinearMoveTool { get; }
        SelectTool SelectTool { get; }
        SnapTool SnapTool { get; }
        QueryTool QueryTool { get; }
        LegendTool LegendTool { get; }

        IMapTool GetToolByName(string toolName);

        IMapTool GetToolByType(Type type);
        T GetToolByType<T>() where T : class;

        void ActivateTool(IMapTool tool);
        
        IEnumerable<IFeature> SelectedFeatures { get; set; }

        // common control methods
        Cursor Cursor { get; set; }

        /// <summary>
        /// Does a refresh when the timer ticks.
        /// </summary>
        void Refresh();
        Graphics CreateGraphics();
        Color BackColor { get; }
        Size ClientSize { get; }
        Size Size { get; }
        int Height { get; }
        int Width { get; }
        Rectangle ClientRectangle { get; }
        Point PointToScreen(Point location);
        Point PointToClient(Point p);

        void Invalidate(Rectangle rectangle);

        event EditorBeforeContextMenuEventHandler BeforeContextMenu;

        // TODO: review / remove methods and properties below
        IList<ISnapRule> SnapRules { get; }
        IList<ISnapRule> GetSnapRules(ILayer layer, IFeature feature, IGeometry geometry, int trackingIndex);

        event MouseEventHandler MouseUp;

        event KeyEventHandler KeyUp;

        event KeyEventHandler KeyDown;

        /// <summary>
        /// True if map control is busy processing something in a separate thread.
        /// </summary>
        bool IsProcessing { get; }

        event EventHandler SelectedFeaturesChanged;
    }
}
