using System;
using DelftTools.Utils.Collections;
using GeoAPI.Extensions.Feature;
using NetTopologySuite.Extensions.Actions;
using QuickGraph.Algorithms;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using DelftTools.Utils;
using GeoAPI.Extensions.Networks;
using GeoAPI.Geometries;
using GeoAPI.Extensions.Coverages;
using GisSharpBlog.NetTopologySuite.Geometries;
using GisSharpBlog.NetTopologySuite.LinearReferencing;
using log4net;
using NetTopologySuite.Extensions.Geometries;
using NetTopologySuite.Extensions.Coverages;

namespace NetTopologySuite.Extensions.Networks
{
    public class SplitResult
    {
        public IBranch NewBranch { get; set; }
        public INode NewNode { get; set; }
    }
    public class NetworkHelper
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(NetworkHelper));

        /// <summary>
        /// connects branchFeature to the first branch is in range. This is not correct it should 
        /// take the nearest or only use a very small tolerance. Updateinf coordinates is primarily 
        /// the responsiblilty of the snapping layer.
        /// TODO: pass snapped info here? Small refactoring is required
        /// </summary>
        /// <param name="branches"></param>
        /// <param name="branchFeature"></param>
        /// <param name="tolerance">set distance tolerance used to detect nearest branch</param>
        public static IBranch AddBranchFeatureToNearestBranch(IEnumerable<IBranch> branches, IBranchFeature branchFeature, double tolerance)
        {
            IBranch nearestBranch = GetNearestBranch(branches, branchFeature.Geometry, tolerance);

            if (nearestBranch == null)
            {
                if (branchFeature.Branch != null)
                {
                    return branchFeature.Branch;
                }
                throw new ArgumentException("Consistency problem, feature can't be connected to the branch, no valid geometry overlapping found");
            }
            AddBranchFeatureToBranch(branchFeature, nearestBranch);
            return nearestBranch;
        }

        ///<summary>
        ///</summary>
        ///<param name="branchFeature"></param>
        ///<param name="nearestBranch"></param>
        public static void AddBranchFeatureToBranch(IBranchFeature branchFeature, IBranch nearestBranch)
        {
            var currentBranch = branchFeature.Branch;
            if (currentBranch != null && currentBranch != branchFeature.Branch) // remove feature from previous branch
            {
                branchFeature.Branch = null;
                currentBranch.BranchFeatures.Remove(branchFeature);
            }

            if (currentBranch != nearestBranch) // add feature to new branch
            {
                branchFeature.Branch = nearestBranch;
                
                //don't add network locations to the branchfeatures of the branch. 
                //this makes no sense since they belong to the coverage and this is not how they are mapped 
                //see issue 2358
                if (!(branchFeature is INetworkLocation))
                {
                    nearestBranch.BranchFeatures.Add(branchFeature);
                }
            }
        }

        public static void AddBranchFeatureToBranch(IBranchFeature branchFeature, IBranch branch, double offset)
        {
            if (offset > branch.Length)
            {
                offset = branch.Length;
            }

            branchFeature.Branch = branch;
            branchFeature.Offset = offset;

            if (!(branchFeature is INetworkLocation))
            {
                branch.BranchFeatures.Add(branchFeature);
            }
            
        }

        ///<summary>
        ///</summary>
        ///<param name="branches"></param>
        ///<param name="geometry"></param>
        ///<param name="tolerance"></param>
        ///<returns></returns>
        ///<exception cref="Exception"></exception>
        public static IBranch GetNearestBranch(IEnumerable<IBranch> branches, IGeometry geometry, double tolerance)
        {
            if (branches.Count() == 0)
            {
                throw new Exception("No branches found");
            }

            var minDistance = Double.MaxValue;
            var nearestBranch = (IBranch)null;

            // first select branches where envelope of branch overlaps with branchFeature
            var overlappingBranches = new List<IBranch>();
            foreach (var branch in branches)
            {
                var branchEnvelope = (IEnvelope)(branch.Geometry.EnvelopeInternal.Clone());
                var featureEnvelope = (IEnvelope)(geometry.EnvelopeInternal.Clone());

                branchEnvelope.ExpandBy(tolerance, tolerance);
                featureEnvelope.ExpandBy(tolerance, tolerance);

                if (branchEnvelope.Intersects(featureEnvelope))
                {
                    overlappingBranches.Add(branch);
                }
            }

            // then find nearest branch using Distance
            foreach (var branch in overlappingBranches)
            {
                var distance = branch.Geometry.Distance(geometry);

                if (distance >= minDistance || distance >= tolerance)
                    continue;
                nearestBranch = branch;
                minDistance = distance;
            }
            return nearestBranch;
        }

        public static double GetBranchFeatureOffsetFromGeometry(IBranch branch,IGeometry geometry)
        {
            double distance = 0.0;

            if (geometry is IPoint)
            {
                distance = GeometryHelper.Distance((ILineString) branch.Geometry, geometry.Coordinates[0]);
            }
            else //complex geometry, calculate intersection first
            {
                var intersectionGeometry = branch.Geometry.Intersection(geometry);
                if (!intersectionGeometry.IsEmpty)
                {
                    var intersectionPoint = intersectionGeometry.Coordinate; //first intersection
                    distance = GeometryHelper.Distance((ILineString) branch.Geometry, intersectionPoint);
                }
            }

            if (branch.IsLengthCustom)
            {
                distance *= branch.Length / branch.Geometry.Length;
            }

            return distance;
        }

        /// <summary>
        /// Moves branch features from <see cref="sourceBranch"/> to <see cref="targetBranch"/>
        /// </summary>
        /// <param name="targetBranch"></param>
        /// <param name="sourceBranch"></param>
        /// <param name="offset"></param>
        public static void MergeBranchFeatures(IBranch sourceBranch, IBranch targetBranch, double offset)
        {
            foreach (var branchFeature in sourceBranch.BranchFeatures.ToArray())
            {
                sourceBranch.BranchFeatures.Remove(branchFeature);
                branchFeature.Offset += offset;
                AddBranchFeatureToBranch(branchFeature, targetBranch, branchFeature.Offset);
            }
        }



        /// <summary>
        /// Tries to split a branch at the given offset. It will not create empty branches ( split at offset 0 or offset is length branch)
        /// All branch features are updated
        /// Offset is interpreted as offset in geometry, even if IsLengthCustom is true for branch
        /// </summary>
        /// <param name="branch"></param>
        /// <param name="offset">Local (or geometry-based) offset</param>
        public static SplitResult SplitBranchAtNode(IBranch branch, double offset)
        {
            bool startedEdit = false;
            BranchSplitAction branchSplitAction = null;
            //start editaction if not editing
            if (!branch.Network.IsEditing)
            {
                startedEdit = true;
                branchSplitAction = new BranchSplitAction();
                branch.Network.BeginEdit(branchSplitAction);
            }

            var lengthIndexedLine = new LengthIndexedLine(branch.Geometry);
            var splitLocation = lengthIndexedLine.ExtractPoint(offset);

            var node = (INode) Activator.CreateInstance(branch.Source.GetType());
            node.Name = GetUniqueName("Node{0:D3}", branch.Network.Nodes, "Node");
            node.Geometry =
                new Point(new Coordinate(splitLocation.X, splitLocation.Y,
                                         Double.IsNaN(splitLocation.Z) ? 0.0 : splitLocation.Z));

            var newBranch = SplitBranchAtNode(branch.Network, branch, node);
            SplitResult result = null;
            if (null != newBranch)
            {
                branch.Network.Nodes.Add(node);
                result = new SplitResult
                           {
                               NewNode = node,
                               NewBranch = newBranch
                           };
                             
            }

            if (startedEdit)
            {
                branchSplitAction.SplittedBranch = branch;
                if (result != null)
                {
                    branchSplitAction.NewBranch = result.NewBranch;
                }
                branch.Network.EndEdit();
            }
            
            return result;
        }

        /// <summary>
        /// todo merge with ProcessService
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="items"></param>
        /// <param name="prefix"></param>
        /// <returns></returns>
        public static string GetUniqueName(string filter, IEnumerable items, string prefix)
        {
            if (null != filter)
            {
                if (filter.Length == 0)
                {
                    // to do test if filter has format code
                    throw new ArgumentException("Can not create an unique name when filter is empty.");
                }
            }
            else
            {
                filter = prefix + "{0}";
            }

            var names = new Dictionary<string, int>();

            foreach (object o in items)
            {
                if (o is INameable)
                {
                    names[((INameable)o).Name] = 0;
                }
            }

            String unique;
            int id = 1;

            do
            {
                unique = String.Format(filter, id++);
            } while (names.ContainsKey(unique));

            return unique;
        }

        /// <summary>
        /// Splits a branch at the given node and connect resulting 2 branches to the node. 
        /// All related branch features are moved to the corresponding branch based on their geometry.
        /// </summary>
        /// <param name="network"></param>
        /// <param name="branch"></param>
        /// <param name="node"></param>
        public static IBranch SplitBranchAtNode(INetwork network, IBranch branch, INode node,bool doBeginEdit = false)
        {
            var originalFirstBranchGeometryLength = branch.Geometry.Length;

            var lengthIndexedLine = new LengthIndexedLine(branch.Geometry);
            var firstBranchGeometryLength = lengthIndexedLine.IndexOf(node.Geometry.Coordinates[0]);

            // compute geometries
            var firstBranchGeometry = (IGeometry)lengthIndexedLine.ExtractLine(0.0, firstBranchGeometryLength).Clone();
            if (firstBranchGeometry == GeometryCollection.Empty)
            {
                throw new ArgumentException(String.Format("Node {0} not located at line; unable to split branch {1}",
                                                          node.Id, branch.Id));
            }

            var secondBranchGeometry = (IGeometry)lengthIndexedLine.ExtractLine(firstBranchGeometryLength, originalFirstBranchGeometryLength).Clone();
            if (secondBranchGeometry == GeometryCollection.Empty)
            {
                throw new ArgumentException(String.Format("Node {0} not located at line; unable to split branch {1}",
                                                          node.Id, branch.Id));
            }

            if (secondBranchGeometry.Coordinates.Count() == 0 || firstBranchGeometry.Coordinates.Count() == 0
                || firstBranchGeometry.Length == 0.0 || secondBranchGeometry.Length == 0.0) 
            {
                return null; // nothing to split
            }

            // update existing branch
            var toNode = branch.Target;
            branch.Target = node;

            // create and add second branch
            var fraction = firstBranchGeometryLength / originalFirstBranchGeometryLength;

            var secondBranch = (IBranch)Activator.CreateInstance(branch.GetType());
            secondBranch.Name = GetUniqueName(null, branch.Network.Branches, "branch");
            secondBranch.Source = node;
            secondBranch.Target = toNode;
            secondBranch.Geometry = secondBranchGeometry;
            secondBranch.IsLengthCustom = branch.IsLengthCustom;
            secondBranch.Attributes = branch.Attributes == null ? null : (IFeatureAttributeCollection)branch.Attributes.Clone();
            network.Branches.Add(secondBranch);

            var originalOffsets = branch.BranchFeatures.ToDictionary(bf => bf, bf => bf.Offset);
            branch.Geometry = firstBranchGeometry;

            // FIX the branch geometries..NTS might have introduced NaNs that mess up serialization
            foreach  (var coordinate in branch.Geometry.Coordinates.Concat(secondBranch.Geometry.Coordinates))
            {
                coordinate.Z = 0;
            }

            // only update length of branch if it is not same as geometry.
            var originalLength = branch.Length;
            double newFirstLength, newSecondLength;
            if (branch.IsLengthCustom)
            {
                newFirstLength = originalLength * fraction;
                newSecondLength = originalLength * (1 - fraction);
            }
            else
            {
                newFirstLength = branch.Geometry.Length;
                newSecondLength = secondBranch.Geometry.Length;
            }
            // remember all branch features to be moved to the second branch
            var branchFeatureToMove = branch.BranchFeatures.Where(bf => bf.Offset >= newFirstLength).ToArray();
            var newOffsets = new List<double>(); 
            foreach (var branchFeature in branchFeatureToMove)
            {
                newOffsets.Add(branchFeature.Offset - newFirstLength);
                branchFeature.SetBeingMoved(true);
                branch.BranchFeatures.Remove(branchFeature);
            }

            branch.Length = newFirstLength;
            secondBranch.Length = newSecondLength;

            // move all features from first branch to the second branch
            for (var i = 0; i < branchFeatureToMove.Length; i++)
            {
                var branchFeature = branchFeatureToMove[i];
                branchFeature.Offset = newOffsets[i];
                AddBranchFeatureToBranch(branchFeature, secondBranch, branchFeature.Offset);
                branchFeature.SetBeingMoved(false);
            }
            // update offset in first branch to original offset
            for (int i = 0; i < branch.BranchFeatures.Count; i++ )
            {
                var branchFeature = branch.BranchFeatures[i];
                branchFeature.Offset = originalOffsets[branchFeature];
            }

            return secondBranch;
        }
        
        private static INetwork CreateLightNetworkCopyWithOldItemsAsAttributes(INetwork network)
        {
            return CreateLightNetworkCopyWithOldItemsAsAttributes(network, null);
        }

        public static INetwork CreateLightNetworkCopyWithOldItemsAsAttributes(INetwork originalNetwork, Func<IBranchFeature, bool> includeBranchFeature)
        {
            if (originalNetwork.IsEditing)
            {
                throw new InvalidOperationException("Invalid network.");
            }
            
            var network = new Network { Name = "Light_" + originalNetwork.Name };

            foreach (var node in originalNetwork.Nodes)
            {
                var newNodes = new Node
                {
                    Geometry = node.Geometry,
                    Name = node.Name
                };
                newNodes.Attributes["OriginalFeature"] = node;
                network.Nodes.Add(newNodes);
            }

            foreach (var branch in originalNetwork.Branches)
            {
                var newBranch = new Branch
                                    {
                                        Geometry = branch.Geometry,
                                        Length = branch.Length,
                                        IsLengthCustom = branch.IsLengthCustom,
                                        Source = network.Nodes[originalNetwork.Nodes.IndexOf(branch.Source)],
                                        Target = network.Nodes[originalNetwork.Nodes.IndexOf(branch.Target)],
                                        Name = branch.Name
                                    };
                newBranch.Attributes["OriginalFeature"] = branch;
                
                foreach (var branchFeature in branch.BranchFeatures)
                {
                    if (includeBranchFeature != null && includeBranchFeature(branchFeature))
                    {
                        newBranch.BranchFeatures.Add((IBranchFeature) branchFeature.Clone());
                    }
                }
                network.Branches.Add(newBranch);
            }

            return network;
        }

        public static IList<INetworkSegment> GetShortestPathBetweenBranchFeaturesAsNetworkSegments(INetwork network, IBranchFeature source, IBranchFeature target)
        {
            var graph = CreateLightNetworkCopyWithOldItemsAsAttributes(network);

            var sourceGraphBranch = graph.Branches[network.Branches.IndexOf(source.Branch)];
            var targetGraphBranch = graph.Branches[network.Branches.IndexOf(target.Branch)];

            INode sourceNode = GetOrCreateNodeOnBranch(MapOffset(sourceGraphBranch, source.Offset), source, sourceGraphBranch);
            var splitOccured = (network.Branches.Count != graph.Branches.Count);
            //if the first node splite the branch and the target was on that branch the target needs to move.
            var targetOffset = target.Offset;
            if (splitOccured && target.Branch == source.Branch)
            {
                if (target.Offset > source.Offset)
                {
                    targetGraphBranch = graph.Branches.Last();
                    //have to update the offset because the branch was split at source offset. Hence substract this offset
                    targetOffset -= source.Offset;
                }
            }
            INode targetNode = GetOrCreateNodeOnBranch(MapOffset(targetGraphBranch, targetOffset), target, targetGraphBranch);

            // check if we don't have adjacent nodes
            var result = graph.ShortestPathsDijkstra(b => b.Length, sourceNode);
            IEnumerable<IBranch> path;

            if (result(targetNode, out path)) // path exists
            {
               return  GetSegmentsForPath(sourceNode, path);
            }
            //return an empty segment list
            return new List<INetworkSegment>(); 
        }

        private static IList<INetworkSegment> GetSegmentsForPath(INode sourceNode, IEnumerable<IBranch> path)
        {
            var segments = new List<INetworkSegment>();
            var currentSourceNode = sourceNode;
            var i = 0;
            foreach (var branch in path)
            {
                var isDirectionPositive = branch.Source == currentSourceNode;
                var currentTargetNode = isDirectionPositive ? branch.Target : branch.Source;

                var originalBranch = (IBranch) branch.Attributes["OriginalFeature"];
                var originalFeature = currentSourceNode.Attributes.ContainsKey("OriginalFeature") ? currentSourceNode.Attributes["OriginalFeature"] : null;

                var newSegment = new NetworkSegment { Branch = originalBranch };
                if (isDirectionPositive)
                {
                    newSegment.Length = branch.Length;
                    if(originalFeature is IBranchFeature)
                    {
                        newSegment.Offset = ((IBranchFeature) originalFeature).Offset;
                    }
                    newSegment.Geometry = (IGeometry)branch.Geometry.Clone();
                }
                else
                {
                    newSegment.DirectionIsPositive = false;
                    newSegment.Length = branch.Length;
                    if (originalFeature is IBranchFeature)
                    {
                        newSegment.Offset = ((IBranchFeature) originalFeature).Offset;
                    }
                    else
                    {
                        newSegment.Offset = originalBranch.Length;
                    }
                    //newSegment.Offset = 0;
                    newSegment.Geometry = new LineString(branch.Geometry.Coordinates.Reverse().ToArray());
                }
                segments.Add(newSegment);
                    
                i++;

                currentSourceNode = currentTargetNode;
            }
            return segments;
        }

        /// <summary>
        /// Gets or creates a new node on the branch by splitting the branch
        /// </summary>
        private static INode GetOrCreateNodeOnBranch(double offset,IBranchFeature node, IBranch branch)
        {
            var splitResult = SplitBranchAtNode(branch, offset);
            var splitNode = (splitResult != null) ? splitResult.NewNode : null;

            if (null != splitNode)
            {
                splitNode.Name = node.ToString();
            }
            else
            {
                //no split so it must be the end or the start of the node
                //find out if it was start or end using geometry of the startNode
                splitNode = (branch.Source.Geometry.Equals(node.Geometry)) ? branch.Source : branch.Target;
            }
            splitNode.Attributes["OriginalFeature"] = node;
            return splitNode;
        }

        public static IEnumerable<INetworkSegment> GenerateSegmentsBetweenLocations(IEnumerable<INetworkLocation> branchLocations, IBranch branch, bool skipFirst, bool skipLast)
        {
            var segments = new List<INetworkSegment>();

            if (branchLocations.Count() > 1)
            {
                double startOffset;
                double endOffset;

                // first segment
                if (!skipFirst)
                {
                    var networkLocation = branchLocations.First();
                    startOffset = 0;
                    endOffset = networkLocation.Offset;
                    segments.Add(CreateSegment(branch, startOffset, endOffset));
                }

                // segments in-between
                startOffset = branchLocations.First().Offset;
                foreach (var networkLocation in branchLocations.Skip(1))
                {
                    endOffset = networkLocation.Offset;
                    segments.Add(CreateSegment(branch, startOffset, endOffset));
                    startOffset = endOffset;
                }

                // last segment
                if (!skipLast)
                {
                    var networkLocation = branchLocations.Last();
                    startOffset = networkLocation.Offset;
                    endOffset = branch.Length;
                    segments.Add(CreateSegment(branch, startOffset, endOffset));
                }
            }

            return segments;
        }

        public static INetworkSegment CreateSegment(IBranch branch, double startOffset, double endOffset)
        {
            var lengthIndexedLine = new LengthIndexedLine(branch.Geometry);

            var geometryStartOffset = startOffset;
            var geometryEndOffset = endOffset;
            IGeometry geometry = null;
            if (branch.Geometry != null)
            {
                if (branch.IsLengthCustom)
                {
                    geometryStartOffset = startOffset * (branch.Geometry.Length / branch.Length);
                    geometryEndOffset = endOffset * (branch.Geometry.Length / branch.Length);
                }

                geometry = branch.Geometry == null ? null : (IGeometry)lengthIndexedLine.ExtractLine(geometryStartOffset, geometryEndOffset).Clone();
            }

            var segment = new NetworkSegment
            {
                Geometry = geometry,
                Branch = branch,
                Offset = startOffset,
                Length = endOffset - startOffset
            };

            return segment;
        }

        public static IEnumerable<INetworkSegment> GenerateSegmentsPerLocation(IEnumerable<INetworkLocation> branchLocations, IBranch branch)
        {
            var segments = new List<INetworkSegment>();

            IList<INetworkLocation> bl = branchLocations.ToList();

            if (bl.Count == 1)
            {
                segments.Add(CreateSegment(branch, 0, branch.Length));
            }
            else
            {
                for (int i = 0; i < bl.Count; i++)
                {
                    double startOffset;
                    double endOffset;

                    if (0 == i)
                    {
                        startOffset = 0;
                        endOffset = bl[i].Offset + (bl[i + 1].Offset - bl[i].Offset) / 2;
                    }
                    else if ((bl.Count - 1) == i)
                    {
                        startOffset = bl[i].Offset - (bl[i].Offset - bl[i - 1].Offset) / 2;
                        endOffset = branch.Length;
                    }
                    else
                    {
                        startOffset = bl[i].Offset - (bl[i].Offset - bl[i - 1].Offset) / 2;
                        endOffset = bl[i].Offset + (bl[i + 1].Offset - bl[i].Offset) / 2;
                    }
                    segments.Add(CreateSegment(branch, startOffset, endOffset));
                }
            }

                //foreach (var networkLocation in branchLocations)
                //{
                //    double endOffset;

                //    if (networkLocation == branchLocations.Last())
                //    {
                //        endOffset = branch.Length;
                //    }
                //    else
                //    {
                //        endOffset = networkLocation.Offset + (branchLocations.First().Offset - networkLocation.Offset) / 2;
                //    }

                //    segments.Add(CreateSegment(branch, startOffset, endOffset));
                //    startOffset = endOffset;
                //}

            return segments;
        }

        public static void GetNeighboursOnBranch<T>(IBranch branch, double offset, out T neighbourBefore, out T neighbourAfter) where T : IBranchFeature
        {
            neighbourBefore = default(T);
            neighbourAfter = default(T);
            neighbourBefore = (T)branch.BranchFeatures.Where(bf => (bf.Offset < offset) && (bf is T)).OrderBy(cs => cs.Offset).LastOrDefault();
            neighbourAfter = (T)branch.BranchFeatures.Where(bf => bf.Offset > offset && (bf is T)).OrderBy(cs => cs.Offset).FirstOrDefault();
        }

        public static double InterpolateDouble(double startChainage, double endChainage, double chainage, double startValue, double endValue)
        {
            if (Math.Abs(endChainage - startChainage) < 1.0e-6)
            {
                return startValue;
            }
            if (chainage < Math.Min(startChainage, endChainage))
            {
                throw new ArgumentException(String.Format("chainage {0} not in range [{1}-{2}]; can not interpolate", chainage, startChainage, endChainage), "chainage");
            }
            if (chainage > Math.Max(startChainage, endChainage))
            {
                throw new ArgumentException(String.Format("chainage {0} not in range [{1}-{2}]; can not interpolate", chainage, startChainage, endChainage), "chainage");
            }
            double fraction = Math.Abs((chainage - startChainage) / (endChainage - startChainage));
            return startValue + fraction * (endValue - startValue);
        }

        public static double MapOffset(IBranch branch, double offset)
        {
            if (branch.IsLengthCustom)
            {
                var geometry = branch.Geometry;
                if (geometry != null)
                {
                    return (geometry.Length/branch.Length)*offset;
                }
            }
            return offset;
        }

        /// <summary>
        /// Merge 2 branches by removing the node that connects them.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="network"></param>
        public static void MergeNodeBranches(INode node, INetwork network)
        {


            // The first branch will be the incoming branch
            IBranch targetBranch = node.IncomingBranches[0];
            IBranch sourceBranch = node.OutgoingBranches[0];

            var canRemoveNodeAndMergeBranches = (node.OutgoingBranches.Count == 1) && (node.IncomingBranches.Count == 1);
            if (!canRemoveNodeAndMergeBranches)
            {
                throw new ArgumentException("Can only merge node with exactly 1 incoming and 1 outgoing branch and belonging to the same reach.");
            }
            if (targetBranch.IsLengthCustom != sourceBranch.IsLengthCustom)
            {
                throw new ArgumentException(string.Format("Cannot merge branch '{0}' and '{1}'. One has a custom length and the other does not.",targetBranch.Name,sourceBranch.Name));
            }

            var branchMergeAction = new BranchMergeAction
                                        {
                                            RemovedBranch = sourceBranch,
                                            RemovedNode = node,
                                            ExtendedBranch = targetBranch
                                        };
            //update the action and end the action so 'listeners' can react
            network.BeginEdit(branchMergeAction);
            
            //the action might have been cancelled
            if (network.EditWasCancelled)
            {
                return;
            }

            var vertices = new List<ICoordinate>();
            for (int i = 0; i < targetBranch.Geometry.Coordinates.Length; i++)
            {
                vertices.Add(new Coordinate(targetBranch.Geometry.Coordinates[i].X,
                                            targetBranch.Geometry.Coordinates[i].Y));
            }
            for (int i = 1; i < sourceBranch.Geometry.Coordinates.Length; i++)
            {
                vertices.Add(new Coordinate(sourceBranch.Geometry.Coordinates[i].X,
                                            sourceBranch.Geometry.Coordinates[i].Y));
            }

            var offset = targetBranch.Geometry.Length;

            targetBranch.Geometry = new LineString(vertices.ToArray()); // endGeometry;
            if (targetBranch.IsLengthCustom)
            {
                //disable customlength so the branchfeatures do not get rescaled on length change
                var oldLength = targetBranch.Length;
                targetBranch.IsLengthCustom = false;
                targetBranch.Length = oldLength + sourceBranch.Length;
                targetBranch.IsLengthCustom = true;
            }

            MergeBranchFeatures(sourceBranch, targetBranch, offset);

            // before removing the source branch reconnect the end of the target branch to the endnode of the source branch
            targetBranch.Target = sourceBranch.Target;

            network.Branches.Remove(sourceBranch);
            network.Nodes.Remove(node);
            
            network.EndEdit();
        }

        public static void UpdateBranchFeatureOffsetFromGeometry(IBranchFeature branchFeature)
        {
            branchFeature.Offset = GetBranchFeatureOffsetFromGeometry(branchFeature.Branch,
                                                                         branchFeature.Geometry);
        }
    }
}