using System.Collections.Generic;
using System.Drawing;
using System.Xml;
using SharpMap.Geometries;
using SharpMap.Styles;

namespace SharpMap.Renderers.ImageMap.Impl
{
    internal class ImageMapLine : ImageMapFeatureElement
    {
        private LineStringCoordList coords = new LineStringCoordList();

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

        public ImageMapLine(SharpMap.Geometries.Geometry geom, SharpMap.Map map, ImageMapStyle mapStyle)
            : base(geom, map)
        {

            PointF[] transLine = (geom as LineString).TransformToImage(map);
            map.MapTransform.TransformPoints(transLine);

            List<PointF> tempPoly = new List<PointF>();

            for (int i = 0; i < transLine.Length; i++)
            {
                PointF p = transLine[i];
                tempPoly.Add(new System.Drawing.PointF((p.X - mapStyle.Line.BufferWidth), (p.Y + mapStyle.Line.BufferWidth)));
                tempPoly.Add(new System.Drawing.PointF((p.X + mapStyle.Line.BufferWidth), (p.Y + mapStyle.Line.BufferWidth)));
            }

            for (int i = transLine.Length - 1; i > -1; i--)
            {
                PointF p = transLine[i];
                tempPoly.Add(new System.Drawing.PointF((p.X - mapStyle.Line.BufferWidth), (p.Y - mapStyle.Line.BufferWidth)));
                tempPoly.Add(new System.Drawing.PointF((p.X + mapStyle.Line.BufferWidth), (p.Y - mapStyle.Line.BufferWidth)));
            }

            if (!tempPoly[0].Equals(tempPoly[tempPoly.Count - 1]))
            {
                tempPoly.Add(tempPoly[0]);
            }

            PointF[] tp = tempPoly.ToArray();
            tp = ImageMapUtilities.ClipPolygon(tp, map.Size.Width, map.Size.Height);
            foreach (PointF pf in tp)
            {
                coords.Add(pf);
            }



        }

    }

}
