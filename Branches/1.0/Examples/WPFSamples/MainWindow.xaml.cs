﻿using System;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using BruTile.Predefined;
using BruTile.Web;
using GeoAPI.Geometries;
using Application = System.Windows.Application;
using MenuItem = System.Windows.Controls.MenuItem;

namespace WPFSamples
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();

        }

        private void MenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void BgOSM_OnClick(object sender, RoutedEventArgs e)
        {
            WpfMap.BackgroundLayer = new SharpMap.Layers.TileAsyncLayer(
                new BruTile.Web.OsmTileSource(), "OSM");

            foreach (var menuItem in Menu.Items.OfType<MenuItem>())
            {
                menuItem.IsChecked = false;
            }
            BgOsm.IsChecked = true;

            WpfMap.ZoomToExtents();
            e.Handled = true;
        }

        private void BgMapQuest_Click(object sender, RoutedEventArgs e)
        {
            WpfMap.BackgroundLayer = new SharpMap.Layers.TileAsyncLayer(
              BruTile.TileSource.Create(KnownTileServers.MapQuest), "MapQuest");

            foreach (var menuItem in Menu.Items.OfType<MenuItem>())
            {
                menuItem.IsChecked = false;
            }
            BgMapQuest.IsChecked = true;

            WpfMap.ZoomToExtents();
            e.Handled = true;
        }

        private void AddShapeLayer_OnClick(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog();
            ofd.Filter = "Shapefiles (*.shp)|*.shp";
            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                var ds = new SharpMap.Data.Providers.ShapeFile(ofd.FileName);
                var lay = new SharpMap.Layers.VectorLayer(System.IO.Path.GetFileNameWithoutExtension(ofd.FileName), ds);
                if (ds.CoordinateSystem != null)
                {
                    GeoAPI.CoordinateSystems.Transformations.ICoordinateTransformationFactory fact =
                        new ProjNet.CoordinateSystems.Transformations.CoordinateTransformationFactory();

                    lay.CoordinateTransformation = fact.CreateFromCoordinateSystems(ds.CoordinateSystem,
                        ProjNet.CoordinateSystems.ProjectedCoordinateSystem.WebMercator);
                    lay.ReverseCoordinateTransformation = fact.CreateFromCoordinateSystems(ProjNet.CoordinateSystems.ProjectedCoordinateSystem.WebMercator,
                        ds.CoordinateSystem);
                }
                WpfMap.MapLayers.Add(lay);
                if (WpfMap.MapLayers.Count == 1)
                {
                    Envelope env = lay.Envelope;
                    WpfMap.ZoomToEnvelope(env);
                }
            }
            e.Handled = true;
        }
    }
}
