using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using SharpMap.Styles;
using System.Drawing;

namespace SharpMap.Renderers.ImageMap.Impl
{
    internal class ImageMapPoint : ImageMapFeatureElement
    {
        public override double Weight
        {
            get { return 1; }
        }

        System.Drawing.Point center;
        int Radius = 5;



        public override void Render(XmlDocument map)
        {
            XmlElement el = map.CreateElement("area");
            el.SetAttribute("shape", "circle");


            base.RenderAttributes(el);

            el.SetAttribute("coords", center.X.ToString() + "," + center.Y.ToString() + "," + Radius.ToString());
            map.DocumentElement.AppendChild(el);
        }

        public ImageMapPoint(SharpMap.Geometries.Geometry geom, SharpMap.Map map, ImageMapStyle mapStyle)
            : base(geom, map)
        {
            this.Radius = mapStyle.Point.Radius;

            SharpMap.Geometries.Point P = geom as SharpMap.Geometries.Point;
            PointF pf = map.WorldToImage(P);
            map.MapTransform.TransformPoints(new PointF[] { pf });
            center = new System.Drawing.Point((int)pf.X, (int)pf.Y);

        }
    }

}
