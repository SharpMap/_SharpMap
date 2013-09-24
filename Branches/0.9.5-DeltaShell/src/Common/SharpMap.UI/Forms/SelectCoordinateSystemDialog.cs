using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using GeoAPI.CoordinateSystems;

namespace SharpMap.UI.Forms
{
    public partial class SelectCoordinateSystemDialog : Form
    {
        private readonly List<TreeNode> gcsNodes = new List<TreeNode>();
        private readonly List<TreeNode> pcsNodes = new List<TreeNode>();
        
        private readonly Timer timerFilterChanged;
        private IList<ICoordinateSystem> supportedCoordinateSystems;

        public event Action<ICoordinateSystem> SelectedCoordinateSystemChanged;

        private ICoordinateSystem selectedCoordinateSystem;

        public ICoordinateSystem SelectedCoordinateSystem
        {
            get { return treeViewProjections.Nodes.Count > 0 ? treeViewProjections.SelectedNode.Tag as ICoordinateSystem : null; }
            set
            {
                selectedCoordinateSystem = value;

                UpdateSelectedCoordinateSystemNode();
            }
        }

        private void UpdateSelectedCoordinateSystemNode()
        {
            if (selectedCoordinateSystem == null)
            {
                treeViewProjections.SelectedNode = treeViewProjections.Nodes[0];

                return; // no coordinate system selected
            }

            var node = gcsNodes.Concat(pcsNodes).FirstOrDefault(n => Equals(((ICoordinateSystem)n.Tag).WKT, selectedCoordinateSystem.WKT));
            if (node == null)
            {
                return; // can't find node for a given coordinate system
            }

            treeViewProjections.SelectedNode = node;
        }

        public SelectCoordinateSystemDialog(IList<ICoordinateSystem> supportedCoordinateSystems)
        {
            this.supportedCoordinateSystems = supportedCoordinateSystems;
            
            InitializeComponent();

            timerFilterChanged = new Timer { Interval = 200 };
            timerFilterChanged.Tick += delegate { FilterTree(); };
            components.Add(timerFilterChanged);
        }

        protected override void OnLoad(EventArgs e)
        {
            CenterToScreen();

            FillProjectionsTreeView();

            treeViewProjections.ExpandAll();

            treeViewProjections.AfterSelect += TreeViewProjectionsOnAfterSelect;

            gcsNodes.AddRange(((TreeNode)treeViewProjections.Nodes[0].Clone()).Nodes.Cast<TreeNode>());
            pcsNodes.AddRange(((TreeNode)treeViewProjections.Nodes[1].Clone()).Nodes.Cast<TreeNode>());

            treeViewProjections.TopNode = treeViewProjections.Nodes[0];

            UpdateSelectedCoordinateSystemNode();
        }

        private void FillProjectionsTreeView()
        {
            treeViewProjections.Nodes.Clear();

            var parentNode = treeViewProjections.Nodes.Add("geographic", "Geographic Coordinate Systems", 0);

            foreach (var coordinateSystem in supportedCoordinateSystems.Where(crs => crs.IsGeographic))
            {
                var name = coordinateSystem.Name;
                var childNode = parentNode.Nodes.Add(name, name, 1);
                childNode.SelectedImageIndex = 1;
                childNode.Tag = coordinateSystem;
            }

            parentNode = treeViewProjections.Nodes.Add("projected", "Projected Coordinate Systems", 0);

            foreach (var coordinateSystem in supportedCoordinateSystems.Where(crs => !crs.IsGeographic))
            {
                var name = coordinateSystem.Name;
                var childNode = parentNode.Nodes.Add(name, name, 2);
                childNode.SelectedImageIndex = 2; 
                childNode.Tag = coordinateSystem;
            }
        }

        private void TreeViewProjectionsOnAfterSelect(object sender, TreeViewEventArgs treeViewEventArgs)
        {
            if (treeViewProjections.SelectedNode != null && SelectedCoordinateSystemChanged != null)
            {
                SelectedCoordinateSystemChanged(treeViewProjections.SelectedNode.Tag as ICoordinateSystem);
            }

            if (treeViewProjections.SelectedNode != null && treeViewProjections.SelectedNode.Tag is ICoordinateSystem)
            {
                var crs = treeViewProjections.SelectedNode.Tag as ICoordinateSystem;
                
                try
                {
                    textBoxSrs.Text = "PROJ.4: " + crs.PROJ4;
                }
                catch (Exception e)
                {
                    textBoxSrs.Text = "PROJ.4: " + e.Message;
                }

                textBoxSrs.Text += "\r\n\r\nWKT:\r\n" + crs.WKT.Replace("\n", "\r\n");
            }
            else
            {
                textBoxSrs.Text = "";
            }
        }

        private void textBoxFilter_TextChanged(object sender, EventArgs e)
        {
            timerFilterChanged.Start();
        }

        private void FilterTree()
        {
            timerFilterChanged.Stop();

            treeViewProjections.SuspendLayout();
            var node1Expanded = treeViewProjections.Nodes[0].IsExpanded;
            var node2Expanded = treeViewProjections.Nodes[1].IsExpanded;

            var gcsNodesFiltered = new List<TreeNode>();
            var pcsNodesFiltered = new List<TreeNode>();

            if (!string.IsNullOrWhiteSpace(textBoxFilter.Text))
            {
                var filter = textBoxFilter.Text.ToLower().TrimStart(' ').TrimEnd(' ');
                gcsNodesFiltered.AddRange(from node in gcsNodes where node.Name.ToLower().Contains(filter) select (TreeNode) node.Clone());
                pcsNodesFiltered.AddRange(from node in pcsNodes where node.Name.ToLower().Contains(filter) select (TreeNode) node.Clone());
            }
            else
            {
                gcsNodesFiltered.AddRange(from node in gcsNodes select (TreeNode)node.Clone());
                pcsNodesFiltered.AddRange(from node in pcsNodes select (TreeNode)node.Clone());
            }

            treeViewProjections.Nodes[0].Nodes.Clear();
            treeViewProjections.Nodes[0].Nodes.AddRange(gcsNodesFiltered.ToArray());
            treeViewProjections.Nodes[1].Nodes.Clear();
            treeViewProjections.Nodes[1].Nodes.AddRange(pcsNodesFiltered.ToArray());

            if (node1Expanded) treeViewProjections.Nodes[0].Expand();
            if (node2Expanded) treeViewProjections.Nodes[1].Expand();
            treeViewProjections.TopNode = treeViewProjections.Nodes[0];
            treeViewProjections.SelectedNode = treeViewProjections.Nodes[0];
            treeViewProjections.ResumeLayout();
        }
    }
}
