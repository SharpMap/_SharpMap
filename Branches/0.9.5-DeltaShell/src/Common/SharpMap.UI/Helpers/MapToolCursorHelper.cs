using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using SharpMap.UI.Tools;

namespace SharpMap.UI.Helpers
{
    public static class MapToolCursorHelper
    {
        private static IDictionary<MapTool, Cursor> cursorsPerTool = new Dictionary<MapTool, Cursor>();

        public static void InitializeCursor(MapTool mapTool, Bitmap icon)
        {
            cursorsPerTool.Add(mapTool, icon != null ? CreateAddFeatureCursor(icon) : null);
        }

        public static void OnMouseMove(MapTool mapTool, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.None)
                return;

            RefreshCursor(mapTool);
        }

        public static void OnIsActiveChanged(MapTool mapTool)
        {
            RefreshCursor(mapTool);
        }

        private static void RefreshCursor(MapTool mapTool)
        {
            if (mapTool.IsActive)
            {
                var targetLayer = mapTool.Layers.FirstOrDefault();
                if (targetLayer != null)
                {
                    var cursor = cursorsPerTool[mapTool];
                    if (cursor != null)
                    {
                        SetCursor(mapTool, cursor);
                        return;
                    }
                }
            }
            SetCursor(mapTool, Cursors.Default);
        }

        private static void SetCursor(MapTool mapTool, Cursor cursor)
        {
            if (!ReferenceEquals(mapTool.MapControl.Cursor, cursor))
                mapTool.MapControl.Cursor = cursor;
        }

        private static Cursor CreateAddFeatureCursor(Bitmap icon)
        {
            var baseImage = MapCursors.AddFeatureTemplateBitmap;
            var featureImage = icon;

            using (var compositeImage = new Bitmap(32, 32))
            using (var g = Graphics.FromImage(compositeImage))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.DrawImageUnscaled(baseImage, 0, 0);
                g.DrawImage(featureImage, 10f, 0f, 10f, 10f);
                return MapCursors.CreateCursor(compositeImage, 0, 0);
            }
        }
    }
}