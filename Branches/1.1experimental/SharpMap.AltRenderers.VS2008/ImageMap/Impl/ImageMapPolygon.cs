using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using SharpMap.Styles;
using System.Drawing;
using SharpMap.Geometries;

namespace SharpMap.Renderers.ImageMap.Impl
{
    internal class ImageMapPolygon
          : ImageMapFeatureElement
    {
        private PolygonCoordList coords = new PolygonCoordList();
        public override double Weight
        {
            get { return coords.Weight; }
        }

        public override void Render(XmlDocument map)
        {
            XmlElement el = map.CreateElement("area");
            el.SetAttribute("shape", "poly");

            base.RenderAttributes(el);

            string coordString = coords.GetCoordString();
            if (coordString != "")
            {
                el.SetAttribute("coords", coords.GetCoordString());
                map.DocumentElement.AppendChild(el);
            }

        }

        public ImageMapPolygon(SharpMap.Geometries.Geometry geom, SharpMap.Map map)
            : base(geom, map)
        {
            PointF[] transRing = (geom as Polygon).ExteriorRing.TransformToImage(map);
            map.MapTransform.TransformPoints(transRing);


            for (int i = 0; i < transRing.Length; i++)
            {
                coords.Add(transRing[i]);
            }

            if (coords.Count > 0 && !coords[0].Equals(coords[coords.Count - 1]))
            {
                coords.Add(coords[0]);
            }
        }
    }
}
