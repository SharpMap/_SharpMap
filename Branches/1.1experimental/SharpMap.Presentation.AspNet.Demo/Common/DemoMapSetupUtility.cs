/*
 *  The attached / following is part of SharpMap.Presentation.AspNet
 *  SharpMap.Presentation.AspNet is free software © 2008 Newgrove Consultants Limited, 
 *  www.newgrove.com; you can redistribute it and/or modify it under the terms 
 *  of the current GNU Lesser General Public License (LGPL) as published by and 
 *  available from the Free Software Foundation, Inc., 
 *  59 Temple Place, Suite 330, Boston, MA 02111-1307 USA: http://fsf.org/    
 *  This program is distributed without any warranty; 
 *  without even the implied warranty of merchantability or fitness for purpose.  
 *  See the GNU Lesser General Public License for the full details. 
 *  
 *  Author: John Diss 2008
 * 
 */
using System.Configuration;
using System.Drawing;
using System.Web;
using SharpMap.Data;
using SharpMap.Data.Providers;
using SharpMap.Layers;
using SharpMap.Rendering.Thematics;
using SharpMap.Styles;
using Point=SharpMap.Geometries.Point;

namespace SharpMap.Presentation.AspNet.Demo
{
    public static class DemoMapSetupUtility
    {
        /// <summary>
        /// little util wich just adds one vector layer to the map and assigns it a random theme.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="m"></param>
        public static void SetupMap(HttpContext context, Map m)
        {
            var l = new VectorLayer(
                "Countries",
                new ShapeFile(context.Server.MapPath(ConfigurationManager.AppSettings["shpfilePath"])));

            l.Style = RandomStyle.RandomVectorStyleNoSymbols();
            l.Theme = new CustomTheme<IVectorStyle>(
                delegate { return RandomStyle.RandomVectorStyleNoSymbols(); });
            m.Layers.Add(l);

            FeatureDataTable labelData = new FeatureDataTable();
            labelData.Columns.Add("Name", typeof (string));
            FeatureDataRow r = labelData.NewRow();
            r["Name"] = "My Lair";
            r.Geometry = new Point(5, 5);
            labelData.AddRow(r);

            LabelLayer labelLayer = new LabelLayer("labelLayer")
                            {
                                DataSource = new GeometryFeatureProvider(labelData),
                                Enabled = true,
                                LabelColumn = "Name",
                                Style = new LabelStyle
                                            {
                                                BackColor = new SolidBrush(Color.Black),
                                                ForeColor = Color.White,
                                                Halo = new Pen(Color.Yellow, 0.1F),
                                                CollisionDetection = false,
                                                Font = new Font("Arial", 10, GraphicsUnit.Point)
                                            }
                            };

            m.Layers.Add(labelLayer);
        }
    }
}