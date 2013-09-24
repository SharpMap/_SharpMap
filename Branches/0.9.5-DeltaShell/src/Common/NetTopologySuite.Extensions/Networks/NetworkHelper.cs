using System;
using DelftTools.Utils.Reflection;
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
using NetTopologySuite.Extensions.Geometries;
using NetTopologySuite.Extensions.Coverages;

namespace NetTopologySuite.Extensions.Networks
{
    public class SplitResult
    {
        public IBranch NewBranch { get; set; }
        public INode NewNode { get; set; }
    }

    public static class NetworkHelper
    {
        /// <summary>
        /// connects branchFeature to the first branch is in range. This is not correct it should 
        /// take the nearest or only use a very small tolerance. Update in coordinates is primarily 
        /// the responsibility of the snapping layer.
        /// TODO: pass snapped info here? Small refactoring is required
        /// </summary>
        /// <param name="branches"></param>
        /// <param name="branchFeature"></param>
        /// <param name="tolerance">set distance tolerance used to detect nearest branch</param>
        public static IBranch AddBranchFeatureToNearestBranch(IEnumerable<IBranch> branches, IBranchFeature branchFeature, double tolerance)
        {
            var nearestBranch = GetNearestBranch(branches, branchFeature.Geometry, tolerance);

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
            if (Equals(currentBranch, nearestBranch)) 
                return;

            if (currentBranch != null) // remove feature from previous branch
            {
                branchFeature.Branch = null;
                currentBranch.BranchFeatures.Remove(branchFeature);
            }

            // add feature to new branch
            //don't add network locations to the branchfeatures of the branch. 
            //this makes no sense since they belong to the coverage and this is not how they are mapped 
            //see issue 2358
            if (branchFeature is INetworkLocation) 
                return;

            branchFeature.Branch = nearestBranch;
            nearestBranch.BranchFeatures.Add(branchFeature);
        }

        public static void AddBranchFeatureToBranch(IBranchFeature branchFeature, IBranch branch, double chainage)
        {
            branchFeature.Branch = branch; // set branch in advance
            branchFeature.Chainage = BranchFeature.SnapChainage(branch.Length, chainage);

            if (!(branchFeature is INetworkLocation))
            {
                branch.BranchFeatures.Add(branchFeature);
            }
        }

        ///<summary>
        /// Finds the nearest <see cref="IBranch"/> in <paramref name="branches"/> within a given tolerance
        /// from a reference location.
        ///</summary>
        ///<param name="branches">Collection of branches to select the nearest from.</param>
        ///<param name="geometry">Reference location.</param>
        ///<param name="tolerance">Tolerance from reference location.</param>
        ///<returns>The nearest branch, or null of none can be found</returns>
        ///<exception cref="Exception">When <paramref name="branches"/> is empty.</exception>
        public static IBranch GetNearestBranch(IEnumerable<IBranch> branches, IGeometry geometry, double tolerance)
        {
            var minDistance = Double.MaxValue;
            var nearestBranch = (IBranch)null;

            // first select branches where envelope of branch overlaps with branchFeature
            var overlappingBranches = new List<IBranch>();
            foreach (var branch in branches)
            {
                if(branch.Geometry == null) continue; // Skip branches without geometry specified

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

        public static double GetBranchFeatureChainageFromGeometry(IBranch branch,IGeometry geometry)
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

            return CalculationChainage(branch, distance);
        }
        
        /// <summary>
        /// Moves branch features from <see cref="sourceBranch"/> to <see cref="targetBranch"/>
        /// </summary>
        /// <param name="targetBranch"></param>
        /// <param name="sourceBranch"></param>
        /// <param name="chainage"></param>
        public static void MergeBranchFeatures(IBranch sourceBranch, IBranch targetBranch, double chainage)
        {
            foreach (var branchFeature in sourceBranch.BranchFeatures.ToArray())
            {
                sourceBranch.BranchFeatures.Remove(branchFeature);
                branchFeature.Chainage = BranchFeature.SnapChainage(targetBranch.Length, branchFeature.Chainage + chainage);
                AddBranchFeatureToBranch(branchFeature, targetBranch, branchFeature.Chainage);
            }
        }

        /// <summary>
        /// Tries to split a branch at the given chainage. It will not create empty branches ( split at chainage 0 or chainage is length branch)
        /// All branch features are updated
        /// Chainage is interpreted as chainage in geometry, even if IsLengthCustom is true for branch
        /// </summary>
        /// <param name="branch"></param>
        /// <param name="geometryOffset">Local (or geometry-based) chainage</param>
        public static SplitResult SplitBranchAtNode(IBranch branch, double geometryOffset)
        {
            bool startedEdit = false;
            BranchSplitAction branchSplitAction = null;
            //start editaction if not editing

            if (geometryOffset == 0.0)
                return null; //no split required

            if (geometryOffset == branch.Geometry.Length)
                return null; //no split required

            if (!branch.Network.IsEditing)
            {
                startedEdit = true;
                branchSplitAction = new BranchSplitAction();
                branch.Network.BeginEdit(branchSplitAction);
            }

            var lengthIndexedLine = new LengthIndexedLine(branch.Geometry);
            var splitLocation = lengthIndexedLine.ExtractPoint(geometryOffset);

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
        /// Splits a branch at the given node and connect resulting 2 branches to the node. 
        /// All related branch features are moved to the corresponding branch based on their geometry.
        /// </summary>
        /// <param name="network"></param>
        /// <param name="branch"></param>
        /// <param name="node"></param>
        public static IBranch SplitBranchAtNode(INetwork network, IBranch branch, INode node)
        {
            var originalFirstBranchGeometryLength = branch.Geometry.Length;

            var lengthIndexedLine = new LengthIndexedLine(branch.Geometry);
            var firstBranchGeometryLength = lengthIndexedLine.IndexOf(node.Geometry.Coordinates[0]);

            // compute geometries
            var firstBranchGeometry = (IGeometry)lengthIndexedLine.ExtractLine(0.0, firstBranchGeometryLength).Clone();
            var secondBranchGeometry = (IGeometry)lengthIndexedLine.ExtractLine(firstBranchGeometryLength, originalFirstBranchGeometryLength).Clone();

            if (firstBranchGeometry == GeometryCollection.Empty || secondBranchGeometry == GeometryCollection.Empty)
            {
                throw new ArgumentException(String.Format("Node {0} not located at line; unable to split branch {1}",
                                                          node.Id, branch.Id));
            }

            if (!secondBranchGeometry.Coordinates.Any() || !firstBranchGeometry.Coordinates.Any() ||
                firstBranchGeometry.Length == 0.0 || secondBranchGeometry.Length == 0.0)
            {
                return null; // nothing to split
            }

            // update existing branch
            var toNode = branch.Target;
            branch.Target = node;

            // create and add second branch
            var secondBranch = (IBranch)Activator.CreateInstance(branch.GetType());
            secondBranch.Name = GetUniqueName(null, branch.Network.Branches, "branch");
            secondBranch.Source = node;
            secondBranch.Target = toNode;
            secondBranch.Geometry = secondBranchGeometry;
            secondBranch.IsLengthCustom = branch.IsLengthCustom;
            secondBranch.OrderNumber = branch.OrderNumber;
            secondBranch.Attributes = branch.Attributes == null ? null : (IFeatureAttributeCollection)branch.Attributes.Clone();
            network.Branches.Add(secondBranch);

            // FIX the branch geometries..NTS might have introduced NaNs that mess up serialization
            foreach (var coordinate in firstBranchGeometry.Coordinates.Concat(secondBranch.Geometry.Coordinates))
            {
                coordinate.Z = 0;
            }

            if (branch.IsLengthCustom)
            {
                // Need to remember the current chainage values because setting the length of a branch with a custom 
                // length causes chainages to be compensated.
                var originalChainages = branch.BranchFeatures.Concat(secondBranch.BranchFeatures).ToDictionary(f => f, f => f.Chainage);
                
                var originalLength = branch.Length;
                var fraction = firstBranchGeometryLength / originalFirstBranchGeometryLength;
                
                branch.Length = originalLength * fraction;
                secondBranch.Length = originalLength*(1 - fraction);

                // restore original chainages
                foreach (var branchFeature in originalChainages.Keys)
                {
                    branchFeature.Chainage = originalChainages[branchFeature];
                }
            }

            MoveBranchFeatures(branch, branch.IsLengthCustom ? branch.Length : firstBranchGeometry.Length, secondBranch);

            // update 1st branch length
            branch.Geometry = firstBranchGeometry;

            SplitBranchFeaturesWithLength(branch, secondBranch);
            
            return secondBranch;
        }

        private static void MoveBranchFeatures(IBranch firstBranch, double newLength, IBranch secondBranch)
        {
            var branchFeatureToMove = firstBranch.BranchFeatures.Where(bf => bf.Chainage >= newLength).ToList();
            var newChainages = branchFeatureToMove.Select(bf => bf.Chainage - newLength).ToList();

            for (int i = 0; i < branchFeatureToMove.Count; i++)
            {
                var branchFeature = branchFeatureToMove[i];

                branchFeature.SetBeingMoved(true);
                firstBranch.BranchFeatures.Remove(branchFeature);
                AddBranchFeatureToBranch(branchFeature, secondBranch, newChainages[i]);
                branchFeature.SetBeingMoved(false);
            }
        }

        private static void SplitBranchFeaturesWithLength(IBranch firstBranch, IBranch secondBranch)
        {
            var featuresToSplit = firstBranch.BranchFeatures.Where(f => f.Chainage + f.Length > firstBranch.Length).ToList();

            foreach (var branchFeature in featuresToSplit)
            {
                if (branchFeature.Geometry is IPoint)
                {
                    // A culvert has length, but it's geometry is a point. We're mixing Length propery meaning, but I'm not 
                    // sure what the solution is. Fixing exception for now using this.
                    continue; 
                }

                branchFeature.SetBeingMoved(true);

                var lengthFirstBranchFeature = firstBranch.Length - branchFeature.Chainage;
                var lengthSecondBranchFeature = branchFeature.Length - lengthFirstBranchFeature;

                var firstBranchFeatureChainage = branchFeature.Chainage;
                var featureOnFirstBranch = (lengthFirstBranchFeature >= lengthSecondBranchFeature);

                if (!featureOnFirstBranch)
                {
                    firstBranch.BranchFeatures.Remove(branchFeature);
                }

                var newBranchFeature = (IBranchFeature)Activator.CreateInstance(branchFeature.GetType());

                var firstBranchFeature = featureOnFirstBranch ? branchFeature : newBranchFeature;
                var secondBranchFeature = featureOnFirstBranch ? newBranchFeature : branchFeature;

                var originalName = branchFeature.Name;
                var lengthIndexedLine = new LengthIndexedLine(branchFeature.Geometry);
                var factor = firstBranch.Geometry.Length/firstBranch.Length;

                firstBranchFeature.Length = lengthFirstBranchFeature;
                firstBranchFeature.Name = originalName + "_1";
                firstBranchFeature.Geometry = (IGeometry)lengthIndexedLine.ExtractLine(lengthIndexedLine.StartIndex, lengthFirstBranchFeature * factor).Clone();

                secondBranchFeature.Length = lengthSecondBranchFeature;
                secondBranchFeature.Name = originalName + "_2";
                secondBranchFeature.Geometry = (IGeometry)lengthIndexedLine.ExtractLine(lengthFirstBranchFeature * factor, lengthIndexedLine.EndIndex).Clone();

                AddBranchFeatureToBranch(secondBranchFeature, secondBranch, 0);

                if (!featureOnFirstBranch)
                {
                    AddBranchFeatureToBranch(firstBranchFeature, firstBranch, firstBranchFeatureChainage);
                }

                branchFeature.SetBeingMoved(false);
            }
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
                var newNode = new Node
                {
                    Geometry = node.Geometry,
                    Name = node.Name,
                    Network = network
                };
                newNode.Attributes["OriginalFeature"] = node;
                network.Nodes.Add(newNode);
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
                                        Name = branch.Name,
                                        Network = network
                                    };
                newBranch.Attributes["OriginalFeature"] = branch;
                
                foreach (var branchFeature in branch.BranchFeatures)
                {
                    if (includeBranchFeature != null && includeBranchFeature(branchFeature))
                    {
                        var newFeature = (IBranchFeature) branchFeature.Clone();
                        newFeature.Network = network;
                        newBranch.BranchFeatures.Add(newFeature);
                    }
                }
                network.Branches.Add(newBranch);
            }

            return network;
        }

        /// <summary>
        /// bloody undo/redo..grrrrrr
        /// </summary>
        /// <param name="graph"></param>
        private static void FixBranchReferencesOnNode(INetwork graph)
        {
            var boundaryNodes = graph.Nodes.Where(n => n.IncomingBranches.Count == 0 || n.OutgoingBranches.Count == 0);
            foreach(var node in boundaryNodes)
            {
                var incomingBranches = graph.Branches.Where(br => br.Target == node).ToList();
                var outgoingBranches = graph.Branches.Where(br => br.Source == node).ToList();

                node.IncomingBranches.AddRange(incomingBranches);
                node.OutgoingBranches.AddRange(outgoingBranches);
            }
        }

        public static IList<INetworkSegment> GetShortestPathBetweenBranchFeaturesAsNetworkSegments(INetwork network, IBranchFeature source, IBranchFeature target)
        {
            var graph = CreateLightNetworkCopyWithOldItemsAsAttributes(network);
            FixBranchReferencesOnNode(graph);

            var sourceGraphBranch = graph.Branches[network.Branches.IndexOf(source.Branch)];
            var targetGraphBranch = graph.Branches[network.Branches.IndexOf(target.Branch)];

            INode sourceNode = GetOrCreateNodeOnBranch(MapChainage(sourceGraphBranch, source.Chainage), source, sourceGraphBranch);
            FixBranchReferencesOnNode(graph);
            var splitOccured = (network.Branches.Count != graph.Branches.Count);
            //if the first node split the branch and the target was on that branch the target needs to move.
            var targetChainage = target.Chainage;
            if (splitOccured && target.Branch == source.Branch)
            {
                if (target.Chainage > source.Chainage)
                {
                    targetGraphBranch = graph.Branches.Last();
                    //have to update the chainage because the branch was split at source chainage. Hence subtract this chainage
                    targetChainage -= source.Chainage;
                }
            }
            INode targetNode = GetOrCreateNodeOnBranch(MapChainage(targetGraphBranch, targetChainage), target, targetGraphBranch);
            FixBranchReferencesOnNode(graph);
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
                    var branchFeature = originalFeature as IBranchFeature;
                    if (branchFeature != null)
                    {
                        // If the node is a boundary node it may be defined on an adjacent branch, and the chainage is zero.
                        newSegment.Chainage = Equals(originalBranch, branchFeature.Branch) ? branchFeature.Chainage : 0;
                    }
                    newSegment.Geometry = (IGeometry)branch.Geometry.Clone();
                }
                else
                {
                    newSegment.DirectionIsPositive = false;
                    newSegment.Length = branch.Length;
                    var branchFeature = originalFeature as IBranchFeature;
                    if (branchFeature != null)
                    {
                        // If the node is a boundary node it may be defined on an adjacent branch, and the chainage is the full length.
                        newSegment.Chainage = Equals(originalBranch, branchFeature.Branch) ? branchFeature.Chainage : originalBranch.Length;
                    }
                    else
                    {
                        newSegment.Chainage = originalBranch.Length;
                    }
                    newSegment.Geometry = new LineString(branch.Geometry.Coordinates.Reverse().ToArray());
                }
                segments.Add(newSegment);

                currentSourceNode = currentTargetNode;
            }
            return segments;
        }

        /// <summary>
        /// Gets or creates a new node on the branch by splitting the branch
        /// 
        /// HACK, TODO: node is of type IBranchFeature?!? node / branch feature are messed-up here.
        /// </summary>
        private static INode GetOrCreateNodeOnBranch(double chainage, IBranchFeature node, IBranch branch)
        {
            var splitResult = SplitBranchAtNode(branch, chainage);
            var splitNode = (splitResult != null) ? splitResult.NewNode : null;

            if (null != splitNode)
            {
                splitNode.Name = node.ToString();
                splitResult.NewBranch.Network = branch.Network;
            }
            else
            {
                //no split so it must be the end or the start of the node
                //find out if it was start or end using geometry of the startNode
                var isSource = branch.Source.Geometry.Coordinate.X == node.Geometry.Coordinate.X &&
                            branch.Source.Geometry.Coordinate.Y == node.Geometry.Coordinate.Y;
                splitNode = isSource ? branch.Source : branch.Target;
            }
            splitNode.Attributes["OriginalFeature"] = node;
            return splitNode;
        }

        public static IEnumerable<INetworkSegment> GenerateSegmentsBetweenLocations(IEnumerable<INetworkLocation> branchLocations, IBranch branch, bool skipFirst, bool skipLast)
        {
            var segments = new List<INetworkSegment>();

            if (branchLocations.Count() > 1)
            {
                double startChainage;
                double endChainage;

                // first segment
                if (!skipFirst)
                {
                    var networkLocation = branchLocations.First();
                    startChainage = 0;
                    endChainage = networkLocation.Chainage;
                    segments.Add(CreateSegment(branch, startChainage, endChainage));
                }

                // segments in-between
                startChainage = branchLocations.First().Chainage;
                foreach (var networkLocation in branchLocations.Skip(1))
                {
                    endChainage = networkLocation.Chainage;
                    segments.Add(CreateSegment(branch, startChainage, endChainage));
                    startChainage = endChainage;
                }

                // last segment
                if (!skipLast)
                {
                    var networkLocation = branchLocations.Last();
                    startChainage = networkLocation.Chainage;
                    endChainage = branch.Length;
                    segments.Add(CreateSegment(branch, startChainage, endChainage));
                }
            }

            return segments;
        }

        public static INetworkSegment CreateSegment(IBranch branch, double startChainage, double endChainage)
        {
            var lengthIndexedLine = new LengthIndexedLine(branch.Geometry);

            var geometryStartChainage = startChainage;
            var geometryEndChainage = endChainage;
            IGeometry geometry = null;
            if (branch.Geometry != null)
            {
                if (branch.IsLengthCustom)
                {
                    geometryStartChainage = startChainage * (branch.Geometry.Length / branch.Length);
                    geometryEndChainage = endChainage * (branch.Geometry.Length / branch.Length);
                }

                geometry = (IGeometry)lengthIndexedLine.ExtractLine(geometryStartChainage, geometryEndChainage).Clone();
            }

            var segment = new NetworkSegment
            {
                Geometry = geometry,
                Branch = branch,
                Chainage = startChainage,
                Length = endChainage - startChainage
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
                    double startChainage;
                    double endChainage;

                    if (0 == i)
                    {
                        startChainage = 0;
                        endChainage = bl[i].Chainage + (bl[i + 1].Chainage - bl[i].Chainage) / 2;
                    }
                    else if ((bl.Count - 1) == i)
                    {
                        startChainage = bl[i].Chainage - (bl[i].Chainage - bl[i - 1].Chainage) / 2;
                        endChainage = branch.Length;
                    }
                    else
                    {
                        startChainage = bl[i].Chainage - (bl[i].Chainage - bl[i - 1].Chainage) / 2;
                        endChainage = bl[i].Chainage + (bl[i + 1].Chainage - bl[i].Chainage) / 2;
                    }
                    segments.Add(CreateSegment(branch, startChainage, endChainage));
                }
            }

            return segments;
        }

        public static void GetNeighboursOnBranch<T>(IBranch branch, double chainage, out T neighbourBefore, out T neighbourAfter) where T : IBranchFeature
        {
            neighbourBefore = default(T);
            neighbourAfter = default(T);
            neighbourBefore = (T)branch.BranchFeatures.Where(bf => (bf.Chainage < chainage) && (bf is T)).OrderBy(cs => cs.Chainage).LastOrDefault();
            neighbourAfter = (T)branch.BranchFeatures.Where(bf => bf.Chainage > chainage && (bf is T)).OrderBy(cs => cs.Chainage).FirstOrDefault();
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

        public static double CalculationChainage(IBranch branch, double mapChainage)
        {
            var chainage = mapChainage;
            if (branch.IsLengthCustom)
            {
                chainage = BranchFeature.SnapChainage(branch.Length, chainage*branch.Length/branch.Geometry.Length);
            }
            return chainage;
        }

        public static double MapChainage(IBranchFeature branchFeature)
        {
            return MapChainage(branchFeature.Branch, branchFeature.Chainage);
        }

        public static double MapChainage(IBranch branch, double chainage)
        {
            if (branch == null) return chainage;

            return MapChainage(branch, branch.Geometry, chainage);
        }

        /// <summary>
        /// Calculates the map (geometry) chainage from a user chainage
        /// </summary>
        /// <param name="branch"></param>
        /// <param name="effectiveBranchGeometry">The branch geometry to calculate with. Useful in case the current 
        /// Branch.Geometry is about to be updated and not yet changed</param>
        /// <param name="chainage"></param>
        /// <returns></returns>
        public static double MapChainage(IBranch branch, IGeometry effectiveBranchGeometry, double chainage)
        {
            if (branch != null && branch.IsLengthCustom)
            {
                if (effectiveBranchGeometry != null)
                {
                    return BranchFeature.SnapChainage(effectiveBranchGeometry.Length, (effectiveBranchGeometry.Length / branch.Length) * chainage);
                }
            }
            return chainage;
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
                network.CancelEdit(); //not one of my prettier hacks, sorry
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

            var chainage = targetBranch.Geometry.Length;

            targetBranch.Geometry = new LineString(vertices.ToArray()); // endGeometry;
            if (targetBranch.IsLengthCustom)
            {
                // Use TypeUtils to circumvent chainage correction for BranchFeatures on targetBranch.
                TypeUtils.SetField(targetBranch, "length", targetBranch.Length + sourceBranch.Length);
            }

            MergeBranchFeatures(sourceBranch, targetBranch, chainage);

            // before removing the source branch reconnect the end of the target branch to the end node of the source branch
            targetBranch.Target = sourceBranch.Target;

            network.Branches.Remove(sourceBranch);
            network.Nodes.Remove(node);
            
            network.EndEdit();
        }

        public static void UpdateBranchFeatureChainageFromGeometry(IBranchFeature branchFeature)
        {
            branchFeature.Chainage = GetBranchFeatureChainageFromGeometry(branchFeature.Branch,
                                                                         branchFeature.Geometry);
        }

        /// <summary>
        /// Removes nodes (orphans) that are not connected to any branch. For example if in a network with 
        /// 1 branch and 2 nodes the branch is deleted both nodes should also be deleted.
        /// </summary>
        public static INode[] RemoveUnusedNodes(INetwork network)
        {
            INode[] toBeRemoved =
                network.Nodes.Where(n => n.IncomingBranches.Count == 0 && n.OutgoingBranches.Count == 0).ToArray();
            foreach (INode node in toBeRemoved)
            {
                network.Nodes.Remove(node);
            }

            return toBeRemoved;
        }

        public static void UpdateLineGeometry(IBranchFeature branchFeature, IGeometry effectiveBranchGeometry)
        {
            double mapChainage, mapLength;
            var branch = branchFeature.Branch;

            if (branch != null)
            {
                var branchLength = branch.IsLengthCustom ? branch.Length : effectiveBranchGeometry.Length;
                FitBranchFeatureWithLengthInBranch(branchFeature, branchLength);
                mapChainage = MapChainage(branchFeature.Branch, effectiveBranchGeometry,
                                                        branchFeature.Chainage);
                mapLength = MapChainage(branchFeature.Branch, effectiveBranchGeometry,
                                                        branchFeature.Length);
            }
            else
            {
                mapChainage = branchFeature.Chainage;
                mapLength = branchFeature.Length;
            }

            var lengthIndexedLine = new LengthIndexedLine(effectiveBranchGeometry);
            branchFeature.Geometry = lengthIndexedLine.ExtractLine(mapChainage, mapChainage + mapLength);
        }

        private static void FitBranchFeatureWithLengthInBranch(IBranchFeature branchFeature, double branchCalculationLength)
        {
            if (branchFeature.Length > branchCalculationLength)
            {
                branchFeature.Chainage = 0;
                branchFeature.Length = branchCalculationLength;
            }
            else if ((branchFeature.Length + branchFeature.Chainage) > branchCalculationLength)
            {
                branchFeature.Chainage = branchCalculationLength - branchFeature.Length;
            }
        }


        ///<summary>
        /// Adds branch to a network. Replaces source/target node with existing node if found.
        ///</summary>
        ///<param name="network">Network to add a branch to</param>
        ///<param name="branch">Branch to add</param>
        public static void AddChannelToHydroNetwork(INetwork network, IBranch branch)
        {
            if (branch.Source == null)
            {
                branch.Source = GetExistingOrNewNodeFromNetwork(network, branch.Geometry.Coordinates.First());
            }
            if (branch.Target == null)
            {
                branch.Target = GetExistingOrNewNodeFromNetwork(network, branch.Geometry.Coordinates.Last());
            }

            lock (network.Branches)
            {
                network.Branches.Add(branch);
            }

            BranchOrderHelper.SetOrderForBranch(network, branch);
        }

        /// <summary>
        /// Create a node for a location or returns the existing node for that location
        /// </summary>
        /// <param name="network"></param>
        /// <param name="coordinate"></param>
        /// <returns></returns>
        private static INode GetExistingOrNewNodeFromNetwork(INetwork network, ICoordinate coordinate)
        {
            //cannot do coordinate equals :( always false.
            var node = network.Nodes.FirstOrDefault(n => (n.Geometry.Coordinate.X == coordinate.X) && (n.Geometry.Coordinate.Y == coordinate.Y));
            if (node != null)
            {
                return node;
            }

            string name = NetworkHelper.GetUniqueName("Node{0:D3}", network.Nodes, "Node");
            var newNode = network.NewNode();
            newNode.Name = name;
            newNode.Geometry = new Point(coordinate);
            network.Nodes.Add(newNode);

            return newNode;
        }

        public static void ClearLocations(INetworkCoverage networkCoverage, IBranch branch)
        {
            var locationsToRemove = networkCoverage.GetLocationsForBranch(branch);
            if (locationsToRemove.Count != 0)
            {
                networkCoverage.Locations.RemoveValues(networkCoverage.Locations.CreateValuesFilter(locationsToRemove));
            }
        }
    }
}