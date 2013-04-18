using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using DelftTools.Utils;
using GeoAPI.Geometries;

namespace SharpMap.UI.Tools
{
    public class ExportMapToImageMapTool : MapTool
    {
        public override bool AlwaysActive
        {
            get { return true; }
        }

        public override void OnBeforeContextMenu(ContextMenuStrip menu, ICoordinate worldPosition)
        {
            if (menu.Items.Count > 0)
            {
                menu.Items.Add(new ToolStripSeparator());
            }

            menu.Items.Add(new ToolStripMenuItem("Export map as image", null, ExportMapEventHandler));

            base.OnBeforeContextMenu(menu, worldPosition);
        }

        private void ExportMapEventHandler(object sender, EventArgs e)
        {
            Execute();
        }

        public override void Execute()
        {
            var imageFormats = new Dictionary<int, Tuple<string, System.Drawing.Imaging.ImageFormat>>();
            int i = 0;
            // we use ++i because SaveFileDialog.FilterIndex starts at 1. Easier indexing..
            imageFormats.Add(++i,
                             new Tuple<string, System.Drawing.Imaging.ImageFormat>("png",
                                                                                   System.Drawing.Imaging.ImageFormat.
                                                                                       Png));
            imageFormats.Add(++i,
                             new Tuple<string, System.Drawing.Imaging.ImageFormat>("jpg",
                                                                                   System.Drawing.Imaging.ImageFormat.
                                                                                       Jpeg));
            imageFormats.Add(++i,
                             new Tuple<string, System.Drawing.Imaging.ImageFormat>("bmp",
                                                                                   System.Drawing.Imaging.ImageFormat.
                                                                                       Bmp));

            var saveFileDialog1 = new SaveFileDialog();

            var filter = new StringBuilder();
            foreach (Tuple<string, System.Drawing.Imaging.ImageFormat> tuple in imageFormats.Values)
            {
                string imageFormatExt = tuple.First;

                filter.AppendFormat("{0} files (*.{0})|", imageFormatExt);
                filter.AppendFormat("*.{0}|", imageFormatExt);
            }
            filter.Append("All files (*.*)|*.*");

            saveFileDialog1.Filter = filter.ToString();
            saveFileDialog1.FilterIndex = 1;
            saveFileDialog1.RestoreDirectory = true;
            saveFileDialog1.Title = "Export map as image";

            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                var image = MapControl.Image;
                image.Save(saveFileDialog1.FileName, imageFormats[saveFileDialog1.FilterIndex].Second);
            }
        }
    }
}