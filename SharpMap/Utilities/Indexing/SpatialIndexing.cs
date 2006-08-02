// Copyright 2005, 2006 - Morten Nielsen (www.iter.dk)
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
using System.Text;

namespace SharpMap.Utilities.SpatialIndexing
{
	/// <summary>
	/// Heuristics used for tree generation
	/// </summary>
	public struct Heuristic
	{
		/// <summary>
		/// Maximum tree depth
		/// </summary>
		public int maxdepth;
		/// <summary>
		/// Minimum object count at node
		/// </summary>
		public int mintricnt;
		/// <summary>
		/// Target object count at node
		/// </summary>
		public int tartricnt;
		/// <summary>
		/// Minimum Error metric � the volume of a box + a unit cube.
		/// The unit cube in the metric prevents big boxes that happen to be flat having a zero result and muddling things up.
		/// </summary>
		public int minerror;
	}
	
	/// <summary>
	/// Constructs a Quad-tree node from a object list and creates its children recursively
	/// </summary>
	public class QuadTree : IDisposable
	{
		private List<BoxObjects> _objList;
		private SharpMap.Geometries.BoundingBox _box;
		private QuadTree _child0;
		private QuadTree _child1;
		/// <summary>
		/// Nodes depth in a tree
		/// </summary>
		protected uint _Depth;
		/// <summary>
		/// Node ID
		/// </summary>
		public uint? _ID;

		/// <summary>
		/// BoundingBox and Feature ID structure used for storing in the quadtree 
		/// </summary>
		public struct BoxObjects
		{
			/// <summary>
			/// Boundingbox
			/// </summary>
			public SharpMap.Geometries.BoundingBox box;
			/// <summary>
			/// Feature ID
			/// </summary>
			public uint ID;
		}

		/// <summary>
		/// Creates a node and either splits the objects recursively into sub-nodes, or stores them at the node depending on the heuristics.
		/// Tree is built top->down
		/// </summary>
		/// <param name="objList">Geometries to index</param>
		/// <param name="depth">Current depth of tree</param>
		/// <param name="heurdata">Heuristics data</param>
		public QuadTree(List<BoxObjects> objList, uint depth, Heuristic heurdata)
		{
			_Depth = depth;

			_box = objList[0].box;
			for (int i = 0; i < objList.Count;i++ )
				_box = _box.Join(objList[i].box);
			
			// test our build heuristic - if passes, make children
			if (depth < heurdata.maxdepth && objList.Count > heurdata.mintricnt &&
				(objList.Count > heurdata.tartricnt || ErrorMetric(_box) > heurdata.minerror))
			{
				List<BoxObjects>[] objBuckets = new List<BoxObjects>[2]; // buckets of geometries
				objBuckets[0] = new List<BoxObjects>();
				objBuckets[1] = new List<BoxObjects>();

				uint longaxis = _box.LongestAxis; // longest axis
				double geoavg = 0; // geometric average - midpoint of ALL the objects

				// go through all bbox and calculate the average of the midpoints
				double frac = 1.0f / objList.Count;
				for (int i = 0; i < objList.Count;i++ )
					geoavg += objList[i].box.GetCentroid()[longaxis] * frac;

				// bucket bbox based on their midpoint's side of the geo average in the longest axis
				for (int i = 0; i < objList.Count;i++ )
					objBuckets[geoavg > objList[i].box.GetCentroid()[longaxis] ? 1 : 0].Add(objList[i]);

				//If objects couldn't be splitted, just store them at the leaf
				//TODO: Try splitting on another axis
				if (objBuckets[0].Count == 0 || objBuckets[1].Count == 0)
				{
					_child0 = null;
					_child1 = null;
					// copy object list
					_objList = objList;
				}
				else
				{
					// create new children using the buckets
					_child0 = new QuadTree(objBuckets[0], depth + 1, heurdata);
					_child1 = new QuadTree(objBuckets[1], depth + 1, heurdata);
				}
			}
			else
			{
				// otherwise the build heuristic failed, this is 
				// set the first child to null (identifies a leaf)
				_child0 = null;
				_child1 = null;
				// copy object list
				_objList = objList;
			}
		}

		/// <summary>
		/// This instantiator is used internally for loading a tree from a file
		/// </summary>
		private QuadTree()
		{
		}

		#region Read/Write index to/from a file

		private const double INDEXFILEVERSION = 1.0;

		internal class ObsoleteFileFormatException : System.Exception
		{
			/// <summary>
			/// Exception thrown when layer rendering has failed
			/// </summary>
			/// <param name="message"></param>
			public ObsoleteFileFormatException(string message)
				: base(message)
			{
			}
		}

		/// <summary>
		/// Loads a quadtree from a file
		/// </summary>
		/// <param name="filename"></param>
		/// <returns></returns>
		public static QuadTree FromFile(string filename)
		{
			System.IO.FileStream fs = new System.IO.FileStream(filename, System.IO.FileMode.Open,System.IO.FileAccess.Read);
			System.IO.BinaryReader br = new System.IO.BinaryReader(fs);
			if (br.ReadDouble() != INDEXFILEVERSION) //Check fileindex version
			{
				fs.Close();
				fs.Dispose();
				throw new ObsoleteFileFormatException("Invalid index file version. Please rebuild the spatial index by either deleting the index");
			}
			QuadTree node = ReadNode(0, ref br);
			br.Close();
			fs.Close();
			return node;
		}

		/// <summary>
		/// Reads a node from a stream recursively
		/// </summary>
		/// <param name="depth">Current depth</param>
		/// <param name="br">Binary reader reference</param>
		/// <returns></returns>
		private static QuadTree ReadNode(uint depth, ref System.IO.BinaryReader br)
		{
			QuadTree node = new QuadTree();
			node._Depth = depth;
			node.Box = new SharpMap.Geometries.BoundingBox(br.ReadDouble(),br.ReadDouble(),br.ReadDouble(),br.ReadDouble());
			bool IsLeaf = br.ReadBoolean();
			if (IsLeaf)
			{
				int FeatureCount = br.ReadInt32();
				node._objList = new List<BoxObjects>();
				for (int i = 0; i < FeatureCount; i++)
				{
					BoxObjects box = new BoxObjects();
					box.box = new SharpMap.Geometries.BoundingBox(br.ReadDouble(), br.ReadDouble(), br.ReadDouble(), br.ReadDouble());
					box.ID = (uint)br.ReadInt32();
					node._objList.Add(box);
				}
			}
			else
			{
				node.Child0 = ReadNode(node._Depth + 1, ref br);
				node.Child1 = ReadNode(node._Depth + 1, ref br);
			}
			return node;
		}

		/// <summary>
		/// Saves the Quadtree to a file
		/// </summary>
		/// <param name="filename"></param>
		public void SaveIndex(string filename)
		{
			System.IO.FileStream fs = new System.IO.FileStream(filename, System.IO.FileMode.Create);
			System.IO.BinaryWriter bw = new System.IO.BinaryWriter(fs);
			bw.Write(INDEXFILEVERSION); //Save index version
			SaveNode(this, ref bw);
			bw.Close();
			fs.Close();
		}
		/// <summary>
		/// Saves a node to a stream recursively
		/// </summary>
		/// <param name="node">Node to save</param>
		/// <param name="sw">Reference to BinaryWriter</param>
		private void SaveNode(QuadTree node, ref System.IO.BinaryWriter sw)
		{
			//Write node boundingbox
			sw.Write(node.Box.Min.X);
			sw.Write(node.Box.Min.Y);
			sw.Write(node.Box.Max.X);
			sw.Write(node.Box.Max.Y);
			sw.Write(node.IsLeaf);
			if (node.IsLeaf)
			{
				sw.Write(node._objList.Count); //Write number of features at node
				for (int i = 0; i < node._objList.Count;i++ ) //Write each featurebox
				{
					sw.Write(node._objList[i].box.Min.X);
					sw.Write(node._objList[i].box.Min.Y);
					sw.Write(node._objList[i].box.Max.X);
					sw.Write(node._objList[i].box.Max.Y);
					sw.Write(node._objList[i].ID);
				}
			}
			else if (!node.IsLeaf) //Save next node
			{
				SaveNode(node.Child0, ref sw);
				SaveNode(node.Child1, ref sw);
			}
		}

		#endregion

		/// <summary>
		/// Calculate the floating point error metric 
		/// </summary>
		/// <returns></returns>
		public double ErrorMetric(SharpMap.Geometries.BoundingBox box)
		{
			SharpMap.Geometries.Point temp = new SharpMap.Geometries.Point(1, 1) + (box.Max - box.Min);
			return temp.X*temp.Y;
		}
		
		/// <summary>
		/// Determines whether the node is a leaf (if data is stored at the node, we assume the node is a leaf)
		/// </summary>
		public bool IsLeaf
		{
			get { return (_objList != null); }
		}

		///// <summary>
		///// Gets/sets the list of objects in the node
		///// </summary>
		//public List<SharpMap.Geometries.IGeometry> ObjList
		//{
		//    get { return _objList; }
		//    set { _objList = value; }
		//}

		/// <summary>
		/// Gets/sets the Axis Aligned Bounding Box
		/// </summary>
		public SharpMap.Geometries.BoundingBox Box
		{
		    get { return _box; }
		    set { _box = value; }
		}

		/// <summary>
		/// Gets/sets the left child node
		/// </summary>
		public QuadTree Child0
		{
			get { return _child0; }
			set { _child0 = value; }
		}

		/// <summary>
		/// Gets/sets the right child node
		/// </summary>
		public QuadTree Child1
		{
			get { return _child1; }
			set { _child1 = value; }
		}
		/// <summary>
		/// Gets the depth of the current node in the tree
		/// </summary>
		public uint Depth
		{
			get { return _Depth; }
		}

		/// <summary>
		/// Disposes the node
		/// </summary>
		public void Dispose()
		{
			//this._box = null;
			this._child0 = null;
			this._child1 = null;
			this._objList = null;
		}

		/// <summary>
		/// Searches the tree and looks for intersections with the boundingbox 'bbox'
		/// </summary>
		/// <param name="box">Boundingbox to intersect with</param>
		public List<uint> Search(SharpMap.Geometries.BoundingBox box)
		{
			List<uint> objectlist = new List<uint>();
			IntersectTreeRecursive(box, this, ref objectlist);
			return objectlist;
		}

		/// <summary>
		/// Recursive function that traverses the tree and looks for intersections with a boundingbox
		/// </summary>
		/// <param name="box">Boundingbox to intersect with</param>
		/// <param name="node">Node to search from</param>
		/// <param name="list">List of found intersections</param>
		private void IntersectTreeRecursive(SharpMap.Geometries.BoundingBox box, QuadTree node, ref List<uint> list)
		{
			if (node.IsLeaf) //Leaf has been reached
			{
				for (int i = 0; i < node._objList.Count;i++ )
					list.Add(node._objList[i].ID);
			}
			else
			{
				if(node.Box.Intersects(box))
				{
					IntersectTreeRecursive(box, node.Child0, ref list);
					IntersectTreeRecursive(box, node.Child1, ref list);
				}
			}
		}
	}
}
