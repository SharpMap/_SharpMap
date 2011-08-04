using System;
using System.Collections.Generic;
using System.Drawing;
using DelftTools.Functions.Filters;
using GeoAPI.Geometries;
using SharpMap.Extensions;
using SharpMap.Layers;
using SharpMap.Rendering.Thematics;
using SharpMap.Styles;
using SharpMap.UI.Forms;
using System.Linq;

namespace SharpMap.UI.Tools
{
    /// <summary>
    /// When this tool is active it displays a scalebar on the mapcontrol.
    /// </summary>
    public class LegendTool : LayoutComponentTool
    {
        #region fields

        private IList<ILayer> layers;
        private Size calculatedSize;
        private bool initScreenPosition;

        // Visual settings
        private Size padding = new Size(3, 3);
        private const int Indent = 10;
        private Font legendFont = new Font("Arial", 10);

        #endregion

        public LegendTool(MapControl mapControl)
            : base(mapControl)
        {
        }

        #region properties (getters & setters)

        public IList<ILayer> Layers
        {
            get { return layers ?? Map.Layers; }
            set { layers = value; }
        }

        /// <summary>
        /// The amount of pixels skipped between the border and legend text (both top and bottom)
        /// </summary>
        public Size Padding
        {
            get { return padding; }
            set { padding = value; }
        }

        /// <summary>
        /// The font face to use for all text painted in the legend
        /// </summary>
        public Font LegendFont
        {
            get { return legendFont; }
            set { legendFont = value; }
        }

        /// <summary>
        /// The size of the legend component (which can't be set but rather is a result of 
        /// the actual layer texts to show).
        /// </summary>
        public override Size Size
        {
            get { return calculatedSize; }
            set { } // Ignore new size
        }

        #endregion

        private DateTime? GetLayersTime()
        {
            DateTime? currentTime = null;
            foreach (ILayer layer in MapHelper.GetAllMapLayers(Map.Layers, true))
            {
                var layerGroup = layer as ILayerGroup;

                if (layerGroup == null || !layerGroup.IsVisible || !layerGroup.ShowInLegend) continue;

                if (layerGroup is ICoverageLayer)
                {
                    currentTime = GetCurrentTimeFromCoverageLayer(layerGroup as ICoverageLayer, currentTime);
                }
            }
            return currentTime;
        }

        private static DateTime? GetCurrentTimeFromCoverageLayer(ICoverageLayer coverageLayer, DateTime? currentTime)
        {
            if (coverageLayer.Coverage.Time != null)
            {
                if (coverageLayer.Coverage.Filters.Count > 0)
                {
                    var currentTimeFilter =
                        coverageLayer.Coverage.Filters.OfType<IVariableValueFilter>().FirstOrDefault();

                    if ((null != currentTimeFilter) && (currentTimeFilter.Values.Count > 0))
                    {
                        currentTime = (DateTime) currentTimeFilter.Values[0];
                    }
                }
                else
                {
                    if (coverageLayer.Coverage.Time.Values.Count > 0)
                    {
                        currentTime = coverageLayer.Coverage.Time.Values[0];
                    }
                }
            }
            return currentTime;
        }

        private static void AddLayerThemeItems(LegendToolItem parent, IEnumerable<ILayer> layers)
        {
            foreach (ILayer layer in layers)
            {
                if ((!layer.IsVisible) || (!layer.ShowInLegend)) continue;

                if (layer is ILayerGroup)
                {
                    // add a grouplayer item and then recursively call this function to add all the layers in the grouplayer
                    var newParent = parent.AddItem(Properties.Resources.legendlayergroupsymbol, layer.Name);
                    AddLayerThemeItems(newParent, ((ILayerGroup) layer).Layers);
                    continue;
                }
                
                if (layer is VectorLayer)
                {
                    AddLayerToLegend(layer as VectorLayer, parent);
                }
                else if (layer is WmsLayer)
                {
                    AddLayerToLegend(layer as WmsLayer, parent);
                }
                else if (layer is RegularGridCoverageLayer)
                {
                    AddLayerToLegend(layer as RegularGridCoverageLayer, parent);
                }
            }
        }

        private static void AddLayerToLegend(RegularGridCoverageLayer layer, LegendToolItem parent)
        {
            var title = layer.Name;
            var layerItem = parent.AddItem(title);
            if (layer.Theme != null && layer.Theme.ThemeItems != null)
            {
                AddThemeItemsAsLegendItems(layer.Theme.ThemeItems, layerItem, false);
            }
        }

        private static void AddLayerToLegend(WmsLayer layer, LegendToolItem parent)
        {
            var title = layer.RootLayer.Name;
            var layerItem = parent.AddItem(title);
            if (layer.Theme != null && layer.Theme.ThemeItems != null)
            {
                AddThemeItemsAsLegendItems(layer.Theme.ThemeItems, layerItem, false);
            }
        }


        private static void AddLayerToLegend(VectorLayer layer, LegendToolItem parent)
        {

            // Add a legendItem for this layer
            var layerItem = parent.AddItem(layer.Name);

            // Use attribute name for layer name
            layerItem.Text += GetThemeAttributeName(layer.Theme);


            if (layer.Theme is CustomTheme)
            {
                layerItem.Symbol = ((VectorStyle) ((CustomTheme) layer.Theme).DefaultStyle).LegendSymbol;
            }
            else if (layer.Theme != null && layer.Theme.ThemeItems != null)
            {
                var isPointVectorLayer = layer.Style != null && layer.Style.GeometryType == typeof(IPoint);
                // Add vectorlayer theme items to the legend
                AddThemeItemsAsLegendItems(layer.Theme.ThemeItems, layerItem, isPointVectorLayer);
            }
            else if (layer.Style != null)
            {
                layerItem.Symbol = layer.Style.LegendSymbol;
                return;
            }
        }

        private static void AddThemeItemsAsLegendItems(IEnumerable<IThemeItem> themeItems, LegendToolItem rootItem,
                                                       bool isPointVectorLayer)
        {
            foreach (var themeItem in themeItems)
            {
                var legendItemLabel = themeItem.Label;

                if (!isPointVectorLayer)
                {
                    if (themeItem.Style is VectorStyle)
                    {
                        rootItem.AddItem((themeItem.Style as VectorStyle).LegendSymbol, legendItemLabel);
                    }
                }
                else
                {
                    rootItem.AddItem(themeItem.Symbol, themeItem.Label);
                }
            }
        }

        private static string GetThemeAttributeName(ITheme theme)
        {
            string attributeName = "";

            if (theme is CategorialTheme)
            {
                attributeName = ((CategorialTheme) theme).AttributeName;
            }

            if (theme is GradientTheme)
            {
                attributeName = ((GradientTheme) theme).ColumnName;
            }

            if (theme is QuantityTheme)
            {
                attributeName = ((QuantityTheme) theme).AttributeName;
            }

            return string.Format(" (attr {0})", attributeName);
        }

        public override void Render(Graphics graphics, Map mapBox)
        {
            if (Visible)
            {
                if (!initScreenPosition)
                {
                    initScreenPosition = SetInitialScreenLocation();
                }

                // create root item
                var root = new LegendToolItem {Padding = Padding, Font = legendFont, Graphics = graphics};
                var rootToolItem = root.AddItem("Legend", true);

                DateTime? currentTime = GetLayersTime();
                AddLayerThemeItems(root, Map.Layers);

                if (currentTime != null)
                {
                    root.InsertItem(rootToolItem, string.Format("Time {0}", currentTime));
                }

                SizeF rootSize = root.Size;

                // HACK : this compensates for the indent in DrawLegendItem function 
                rootSize.Width += Indent*3;

                Size newSize = new Size((int) rootSize.Width, (int) rootSize.Height);

                // Paint a semi-transparent background
                graphics.FillRectangle(new SolidBrush(Color.FromArgb(125, 255, 255, 255)),
                                       new Rectangle(screenLocation, newSize));

                // Paint a black border
                graphics.DrawRectangle(Pens.Black, new Rectangle(screenLocation, newSize));

                // draw root item
                DrawLegendItem(graphics, root, screenLocation.X, screenLocation.Y);

                // Store our own new size (used by the component for dragging, etc.)
                calculatedSize = newSize;
            }

            base.Render(graphics, mapBox);
        }

        private void DrawLegendItem(Graphics graphics, LegendToolItem toolItem, float x, float y)
        {
            float curX = x;
            bool hasSymbol = toolItem.Symbol != null;
            bool hasText = toolItem.Text != null;

            if (hasSymbol)
            {
                var deltaY = (toolItem.InternalSize.Height - toolItem.Symbol.Height)/2;
                graphics.DrawImage(toolItem.Symbol, curX, y + deltaY);
                curX += toolItem.Symbol.Width;
            }

            if (hasText)
            {
                curX += (hasSymbol) ? padding.Width : 0;

                var deltaY = (toolItem.InternalSize.Height - graphics.MeasureString(toolItem.Text, toolItem.Font).Height)/
                             2;
                graphics.DrawString(toolItem.Text, toolItem.Font, Brushes.Black, curX, y + deltaY);
            }

            if (hasText || hasSymbol)
            {
                y += toolItem.InternalSize.Height;
            }

            foreach (var subItem in toolItem.Items)
            {
                float deltaX = padding.Width + Indent;

                if (subItem.Centered && subItem.Symbol == null)
                {
                    float textWidth = graphics.MeasureString(subItem.Text, toolItem.Font).Width;
                    deltaX = (toolItem.Root.Size.Width - textWidth)/2;
                }

                DrawLegendItem(graphics, subItem, x + deltaX, y + padding.Height);
                y += subItem.Size.Height;
            }
        }
    }
}