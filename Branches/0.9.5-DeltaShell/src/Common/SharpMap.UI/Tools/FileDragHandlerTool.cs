using System.Collections.Generic;
using System.Windows.Forms;
using log4net;
using SharpMap.Layers;

namespace SharpMap.UI.Tools
{
    public class FileDragHandlerTool : MapTool
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(FileDragHandlerTool));

        public static ILayerFactory LayerFactory { get; set; }

        public override void OnDragDrop(DragEventArgs e)
        {
            if(LayerFactory == null)
            {
                log.Error("LayerFactory is undefined, skipping drag & drop operation.");
                return;
            }

            if (Map == null) return;
            if (e.Data.GetDataPresent(DataFormats.FileDrop, false))
            {
                
                foreach(string path in (string[]) e.Data.GetData(DataFormats.FileDrop))
                {
                    foreach ( ILayer layer in LayerFactory.CreateLayersFromFile(path))
                    {
                        Map.Layers.Insert(0, layer);
                    }
                }
            }
        }



        public override void OnDragEnter(DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop, false))
            {
                e.Effect = DragDropEffects.Copy & e.AllowedEffect;
            }
        }

        public override bool AlwaysActive
        {
            get
            {
                return true;
            }
        }
    }

    public interface ILayerFactory
    {
        IEnumerable<ILayer> CreateLayersFromFile(string path);
    }
}