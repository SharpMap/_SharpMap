using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using SharpMap.Geometries;

namespace SilverlightRendering
{
    class Buffer
    {
        Canvas canvas = new Canvas();
        BoundingBox extent = new BoundingBox(0, 0, 0, 0);

        public Canvas Canvas
        {
            get { return canvas; }
            set { canvas = value; }
        }

        public BoundingBox Extent
        {
            get { return extent; }
            set { extent = value; }
        }
    }
}
