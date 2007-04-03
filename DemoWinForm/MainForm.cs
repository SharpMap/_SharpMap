// Copyright 2007 - Rory Plaire (codekaizen@gmail.com)
//
// This file is part of SharpMap.
// SharpMap is free software; you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
// 
// SharpMap is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.

// You should have received a copy of the GNU Lesser General Public License
// along with SharpMap; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA 

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

using SharpMap.Geometries;
using SharpMap.Layers;
using SharpMap.Forms;
using SharpMap.Data.Providers;

using DemoWinForm.Properties;

namespace DemoWinForm
{
	public partial class MainForm : Form
	{
		Dictionary<string, ILayerFactory> _layerFactoryCatalog = new Dictionary<string, ILayerFactory>();

		public MainForm()
		{
			InitializeComponent();

			registerLayerFactories();

			MainMapImage.MapQueried += new MapImage.MapQueryHandler(MainMapImage_MapQueried);
		}

		private void registerLayerFactories()
		{
//			ConfigurationManager.GetSection("LayerFactories");
			_layerFactoryCatalog[".shp"] = new ShapeFileLayerFactory();
		}

		private void addLayer()
		{
			DialogResult result = AddLayerDialog.ShowDialog(this);

			if (result == DialogResult.OK)
			{
				foreach (string fileName in AddLayerDialog.FileNames)
				{
					string extension = Path.GetExtension(fileName);
					ILayerFactory layerFactory = null;
					
					if (!_layerFactoryCatalog.TryGetValue(extension, out layerFactory))
						continue;

					ILayer layer = layerFactory.Create(Path.GetFileNameWithoutExtension(fileName), fileName);

					MainMapImage.Map.Layers.Add(layer);

					LayersDataGridView.Rows.Add(true, getLayerTypeIcon(layer.GetType()), layer.LayerName);
				}

				MainMapImage.Refresh();
			}
		}

		private void removeLayer()
		{
			if (LayersDataGridView.SelectedRows.Count == 0)
				return;

			string layerName = LayersDataGridView.SelectedRows[0].Cells[2].Value as string;

			ILayer layerToRemove = null;

			foreach (ILayer layer in MainMapImage.Map.Layers)
				if (layer.LayerName == layerName)
				{
					layerToRemove = layer;
					break;
				}

			MainMapImage.Map.Layers.Remove(layerToRemove);
			LayersDataGridView.Rows.Remove(LayersDataGridView.SelectedRows[0]);
		}

		private void zoomToExtents()
		{
			MainMapImage.Map.ZoomToExtents();
			MainMapImage.Refresh();
		}

		private void changeMode(MapImage.Tools tool)
		{
			MainMapImage.ActiveTool = tool;

			ZoomInModeToolStripButton.Checked = (tool == MapImage.Tools.ZoomIn);
			ZoomOutModeToolStripButton.Checked = (tool == MapImage.Tools.ZoomOut);
			PanToolStripButton.Checked = (tool == MapImage.Tools.Pan);
			QueryModeToolStripButton.Checked = (tool == MapImage.Tools.Query);
		}

		private object getLayerTypeIcon(Type type)
		{
			if (type == typeof(VectorLayer))
			{
				return Resources.polygon;
			}

			return Resources.Raster;
		}

		private void changeUIOnLayerSelectionChange()
		{
			bool isLayerSelected = false;

			if (LayersDataGridView.SelectedRows.Count > 0)
				isLayerSelected = true;

			RemoveLayerToolStripButton.Enabled = isLayerSelected;
			RemoveLayerToolStripMenuItem.Enabled  = isLayerSelected;
		}

		void MainMapImage_MapQueried(SharpMap.Data.FeatureDataTable data)
		{
			FeaturesDataGridView.DataSource = data;
		}

		private void AddLayerToolStripButton_Click(object sender, EventArgs e)
		{
			BeginInvoke((MethodInvoker)delegate() { addLayer(); });
		}

		private void RemoveLayerToolStripButton_Click(object sender, EventArgs e)
		{
			BeginInvoke((MethodInvoker)delegate() { removeLayer(); });
		}

		private void AddLayerToolStripMenuItem_Click(object sender, EventArgs e)
		{
			BeginInvoke((MethodInvoker)delegate() { addLayer(); });
		}

		private void RemoveLayerToolStripMenuItem_Click(object sender, EventArgs e)
		{
			BeginInvoke((MethodInvoker)delegate() { removeLayer(); });
		}

		private void ZoomToExtentsToolStripButton_Click(object sender, EventArgs e)
		{
			BeginInvoke((MethodInvoker)delegate() { zoomToExtents(); });
		}

		private void PanToolStripButton_Click(object sender, EventArgs e)
		{
			BeginInvoke((MethodInvoker)delegate() { changeMode(MapImage.Tools.Pan); });
		}

		private void QueryModeToolStripButton_Click(object sender, EventArgs e)
		{
			BeginInvoke((MethodInvoker)delegate() { changeMode(MapImage.Tools.Query); });
		}

		private void ZoomInModeToolStripButton_Click(object sender, EventArgs e)
		{
			BeginInvoke((MethodInvoker)delegate() { changeMode(MapImage.Tools.ZoomIn); });
		}

		private void ZoomOutModeToolStripButton_Click(object sender, EventArgs e)
		{
			BeginInvoke((MethodInvoker)delegate() { changeMode(MapImage.Tools.ZoomOut); });
		}

		private void MoveUpToolStripMenuItem_Click(object sender, EventArgs e)
		{

		}

		private void MoveDownToolStripMenuItem_Click(object sender, EventArgs e)
		{

		}

		private void LayersDataGridView_SelectionChanged(object sender, EventArgs e)
		{
			BeginInvoke((MethodInvoker)delegate() 
			{ 
				changeUIOnLayerSelectionChange();

				if (LayersDataGridView.SelectedRows.Count > 0)
				{
					MainMapImage.QueryLayerIndex = LayersDataGridView.SelectedRows[0].Index;
				}
			});
		}
	}
}