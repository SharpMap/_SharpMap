using System;
using System.Drawing;
using System.Windows.Forms;
using GeoAPI.Extensions.Feature;
using GeoAPI.Geometries;
using SharpMap.Layers;
using SharpMap.UI.Forms;
using SharpMap.UI.Snapping;
using System.ComponentModel;
using DelftTools.Utils.Collections;

namespace SharpMap.UI.Tools
{
    public interface IMapTool
    {
        IMapControl MapControl { get; set; }

        void OnMouseDown(ICoordinate worldPosition, MouseEventArgs e);
        
        void OnMouseMove(ICoordinate worldPosition, MouseEventArgs e);
        
        void OnMouseUp(ICoordinate worldPosition, MouseEventArgs e);

        void OnMouseWheel(ICoordinate worldPosition, MouseEventArgs e);
        
        void OnMouseDoubleClick(object sender, MouseEventArgs e);

        void OnMouseHover(ICoordinate worldPosition, EventArgs e);

        void OnKeyDown(KeyEventArgs e);

        void OnKeyUp(KeyEventArgs e);
        
        void OnPaint(PaintEventArgs e);

        void Render(Graphics graphics, Map mapBox);

        void OnMapLayerRendered(Graphics g, ILayer layer);

        void OnMapPropertyChanged(object sender, PropertyChangedEventArgs e);

        void OnMapCollectionChanged(object sender, NotifyCollectionChangedEventArgs e);

        void OnBeforeContextMenu(ContextMenuStrip menu, ICoordinate worldPosition);

        void OnDragEnter(DragEventArgs e);

        void OnDragDrop(DragEventArgs e);

        /// <summary>
        /// Returns true if tool is currently busy (working).
        /// </summary>
        bool IsBusy { get; set; }

        /// <summary>
        /// True when tool is currently active (can be used).
        /// </summary>
        bool IsActive { get; set; }

        /// <summary>
        /// True when tool is currently enabled.
        /// </summary>
        bool Enabled { get; }

        /// <summary>
        /// Returns true if tool is always active, e.g. mouse wheel zoom,. fixed zoom in/out, zoom to map extent ...
        /// 
        /// <remarks>If tool is AlwaysActive - Execute() method should be used and not ActivateTool().</remarks>
        /// </summary>
        bool AlwaysActive { get; }

        /// <summary>
        /// Used for AlwaysActive tools.
        /// </summary>
        void Execute();

        /// <summary>
        /// Cancels the current operation. Map should revert to state before start tool
        /// </summary>
        void Cancel();
        
        /// <summary>
        /// User readable name of tool.
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// Tool may be used only for 1 layer, otherwise null.
        /// </summary>
        ILayer Layer { get; set; }

        void ActiveToolChanged(IMapTool newTool); // TODO: remove, why tool should know about changes of active tool

        //ILayer GetLayerByGeometry(IGeometry geometry);
        ILayer GetLayerByFeature(IFeature feature);
    }
}