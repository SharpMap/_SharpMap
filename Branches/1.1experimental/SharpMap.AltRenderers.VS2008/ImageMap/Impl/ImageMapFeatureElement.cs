using System;
using System.Collections.Generic;
using System.Text;

namespace SharpMap.Renderers.ImageMap.Impl
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Drawing;
    using System.Drawing.Drawing2D;
    using System.Xml;
    using SharpMap.Geometries;
    using SharpMap.Styles;

    internal abstract class ImageMapFeatureElement
        : IComparable<ImageMapFeatureElement>
    {
        public abstract double Weight { get; }
        public abstract void Render(XmlDocument map);

        private readonly Dictionary<string, string> _attributes = new Dictionary<string, string>();
        public IEnumerable<KeyValuePair<string, string>> Attributes
        {
            get
            {
                foreach (KeyValuePair<string, string> pair in _attributes)
                {
                    yield return pair;
                }
            }
        }

        internal void AddAttribute(string key, string value)
        {
            if (string.IsNullOrEmpty(key)
                || string.IsNullOrEmpty(value))
                return;

            if (_attributes.ContainsKey(key))
                _attributes[key] = value;
            else
                _attributes.Add(key, value);
        }

        protected bool RenderAttributes(XmlElement el)
        {
            bool attributesRendered = false;
            foreach (KeyValuePair<string, string> pair in Attributes)
            {
                if (!string.IsNullOrEmpty(pair.Value)
                    && !string.IsNullOrEmpty(pair.Key))
                {
                    el.SetAttribute(pair.Key, pair.Value);
                    attributesRendered = true;
                }
            }
            return attributesRendered;
        }

        public ImageMapFeatureElement(Geometry geom, SharpMap.Map map)
        {

        }

        public static ImageMapFeatureElement CreateImageMapElement(SharpMap.Geometries.Geometry geom, SharpMap.Map map, ImageMapStyle mapStyle)
        {
            if (!mapStyle.Enabled)
                return null;

            if (geom as Polygon != null
                && mapStyle.Polygon.Enabled
                && map.Zoom > mapStyle.Polygon.MinVisible
                && map.Zoom < mapStyle.Polygon.MaxVisible)
            {
                return new ImageMapPolygon(geom, map);
            }
            else if (geom as LineString != null
                && mapStyle.Line.Enabled
                && map.Zoom > mapStyle.Line.MinVisible
                && map.Zoom < mapStyle.Line.MaxVisible)
            {
                return new ImageMapLine(geom, map, mapStyle);
            }
            else if (geom as SharpMap.Geometries.Point != null
                && mapStyle.Point.Enabled
                && map.Zoom > mapStyle.Point.MinVisible
                && map.Zoom < mapStyle.Point.MaxVisible)
            {
                return new ImageMapPoint(geom, map, mapStyle);
            }
            return null;
        }


        #region IComparable<ImageMapElement> Members

        public int CompareTo(ImageMapFeatureElement other)
        {
            return this.Weight < other.Weight ? -1
                : this.Weight == other.Weight ? 0 : 1;
        }

        #endregion
    }

}

