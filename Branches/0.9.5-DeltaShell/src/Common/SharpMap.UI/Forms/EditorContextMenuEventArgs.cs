using System;
using System.Windows.Forms;
using GeoAPI.Extensions.Feature;

namespace SharpMap.UI.Forms
{
    public class EditorContextMenuEventArgs : EventArgs
    {
        private ContextMenuStrip contextMenuStrip;
        private IFeature feature;

        public EditorContextMenuEventArgs(IFeature feature)
        {
            Feature = feature;
        }

        public ContextMenuStrip ContextMenuStrip
        {
            get { return contextMenuStrip;} 
            set{ contextMenuStrip = value;}
        }

        public IFeature Feature
        {
            get { return feature;} 
            set { feature = value;}
        }
    }
}