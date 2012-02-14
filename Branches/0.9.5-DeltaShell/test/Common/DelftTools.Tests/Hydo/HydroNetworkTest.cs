using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using DelftTools.Hydro;
using DelftTools.Hydro.CrossSections;
using DelftTools.Hydro.Helpers;
using DelftTools.Hydro.Structures;
using DelftTools.TestUtils;
using DelftTools.TestUtils.TestReferenceHelper;
using DelftTools.Utils;
using DelftTools.Utils.Collections;
using DelftTools.Utils.Reflection;
using GisSharpBlog.NetTopologySuite.Geometries;
using log4net;
using log4net.Core;
using NetTopologySuite.Extensions.Networks;
using NUnit.Framework;
using PostSharp;
using Rhino.Mocks;

namespace DelftTools.Tests.Hydo
{
    [TestFixture]
    public class HydroNetworkTest
    {
        private static readonly MockRepository mocks = new MockRepository();
        private static readonly ILog log = LogManager.GetLogger(typeof (HydroNetworkTest));

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            LogHelper.ConfigureLogging();
            LogHelper.SetLoggingLevel(Level.Info);
        }

        [TestFixtureTearDown]
        public void TestFixtureTearDown()
        {
            LogHelper.ResetLogging();
        }

        [Test]
        public void DeletingDefaultCsSharedDefinitionClearsDefault()
        {
            var hydroNetwork = new HydroNetwork();

            var csDef1 = new CrossSectionDefinitionYZ();
            var csDef2 = new CrossSectionDefinitionYZ();
            var csDef3 = new CrossSectionDefinitionYZ();
            hydroNetwork.SharedCrossSectionDefinitions.Add(csDef1);
            hydroNetwork.SharedCrossSectionDefinitions.Add(csDef2);
            hydroNetwork.SharedCrossSectionDefinitions.Add(csDef3);
            hydroNetwork.DefaultCrossSectionDefinition = csDef2;

            Assert.AreSame(csDef2, hydroNetwork.DefaultCrossSectionDefinition);
            hydroNetwork.SharedCrossSectionDefinitions.Remove(csDef1);
            Assert.AreSame(csDef2, hydroNetwork.DefaultCrossSectionDefinition);
            hydroNetwork.SharedCrossSectionDefinitions.Remove(csDef3);
            Assert.AreSame(csDef2, hydroNetwork.DefaultCrossSectionDefinition);
            hydroNetwork.SharedCrossSectionDefinitions.Remove(csDef2);
            Assert.AreSame(null, hydroNetwork.DefaultCrossSectionDefinition);
        }

        [Test]
        [Ignore("TODO: not working anymore due to refactoring; re-enable later")]
        public void AddCrossSectionToBranchUsingCollections()
        {
            var crossSection = new CrossSection(null);
            var branch = new Channel(new HydroNode("from"), new HydroNode("To"));

            //NetworkHelper.AddBranchFeatureToBranch(branch, crossSection, crossSection.Offset);
            branch.BranchFeatures.Add(crossSection);

            Assert.AreEqual(branch, crossSection.Branch);

            branch.BranchFeatures.Clear();
            Assert.IsNull(crossSection.Branch);
        }

        [Test]
        [Category(TestCategory.Performance)]
        public void AddManyBranchesWithCrossSections()
        {
            TestHelper.AssertIsFasterThan(2275,() =>
                                                     {
                                                         const int count = 10000;
                                                         var network = new HydroNetwork();
                                                         for (int i = 0; i < count; i++)
                                                         {
                                                             var from = new HydroNode();
                                                             var to = new HydroNode();

                                                             network.Nodes.Add(from);
                                                             network.Nodes.Add(to);

                                                             var channel = new Channel {Source = from, Target = to};
                                                             HydroNetworkHelper.AddCrossSectionDefinitionToBranch(channel,
                                                                                                    new CrossSectionDefinitionXYZ(),
                                                                                                    0);
                                                         }

                                                         int crossSectionCount = 0;
                                                         foreach (var crossSection in network.CrossSections)
                                                         {
                                                             // access all CrossSections should be also fast
                                                             crossSectionCount++;
                                                         }
                                                     });
        }

        [Test]
        [Category(TestCategory.Performance)]
        [Category(TestCategory.BadQuality)] // TODO: test Add or change name
        public void AddManyBranchesWithSimpleBranchFeature()
        {
            const int count = 10000;
            int weirCount = 0;

            Action action = delegate // TODO: what are we testing here? Test only add.
                                {
                                    var network = new HydroNetwork();
                                    for (int i = 0; i < count; i++)
                                    {
                                        var from = new HydroNode();
                                        var to = new HydroNode();

                                        network.Nodes.Add(from);
                                        network.Nodes.Add(to);

                                        var channel = new Channel {Source = from, Target = to};

                                        var compositeBranchStructure = new CompositeBranchStructure();
                                        NetworkHelper.AddBranchFeatureToBranch(compositeBranchStructure, channel, 0);
                                        HydroNetworkHelper.AddStructureToComposite(compositeBranchStructure, new Weir());

                                        network.Branches.Add(channel);
                                    }

                                    foreach (IWeir weir in network.Weirs) // access all Weirs should be also fast
                                    {
                                        weirCount++;
                                    }
                                };

            TestHelper.AssertIsFasterThan(2750, string.Format("Added {0} branches with {1} weirs", count, weirCount), action);
        }

        [Test]
        public void BranchCrossSectionShouldRaiseCollectionChangedEvent()
        {
            var crossSection = new CrossSectionDefinitionXYZ();
            var branch = new Channel(new HydroNode("from"), new HydroNode("To"));

            int count = 0;
            Post.Cast<Channel, INotifyCollectionChange>(branch).CollectionChanged += delegate { count++; };

            HydroNetworkHelper.AddCrossSectionDefinitionToBranch(branch, crossSection, 0.0);
            Assert.AreEqual(1, count);

            branch.BranchFeatures.Clear();
            Assert.AreEqual(2, count);
        }

        [Test]
        public void CloneRewiresProxyDefinitions()
        {
            var network = HydroNetworkHelper.GetSnakeHydroNetwork(1);
            var sharedDefinition = new CrossSectionDefinitionYZ();
            
            network.SharedCrossSectionDefinitions.Add(sharedDefinition);
            var crossSectionDefinitionProxy = new CrossSectionDefinitionProxy(sharedDefinition);
            HydroNetworkHelper.AddCrossSectionDefinitionToBranch(network.Branches.First(),
                                                                 crossSectionDefinitionProxy, 10.0);
            
            var clonedNetwork = (HydroNetwork) network.Clone();
            Assert.AreEqual(1,clonedNetwork.SharedCrossSectionDefinitions.Count);
            //check the proxy got rewired
            var crossSectionClone = clonedNetwork.CrossSections.First();
            var clonedProxyDefinition = (CrossSectionDefinitionProxy)crossSectionClone.Definition;
            Assert.AreEqual(clonedProxyDefinition.InnerDefinition,clonedNetwork.SharedCrossSectionDefinitions.First());
        }

        
        [Test]
        [Category(TestCategory.Integration)]
        public void CloneHydroNetworkWithProxyDefinitions()
        {
            var network = HydroNetworkHelper.GetSnakeHydroNetwork(1);
            var sharedDefinition = new CrossSectionDefinitionYZ();
            network.SharedCrossSectionDefinitions.Add(sharedDefinition);
            HydroNetworkHelper.AddCrossSectionDefinitionToBranch(network.Channels.First(),
                                                                 new CrossSectionDefinitionProxy(sharedDefinition),
                                                                 10.0d);

            var clone = (HydroNetwork)network.Clone();
            TestReferenceHelper.AssertStringRepresentationOfGraphIsEqual(network, clone);
        }

        [Test]
        [Category(TestCategory.Integration)]
        public void CloneHydroNetworkWithData()
        {
            var network = HydroNetworkHelper.GetSnakeHydroNetwork(1);

            var publicProperties = ReflectionTestHelper.GetPublicListProperties(network);
            foreach(var prop in publicProperties)
            {
                try
                {
                    var value = prop.GetValue(network, null);
                    if (value is IList)
                    {
                        var genericType = TypeUtils.GetFirstGenericTypeParameter(value.GetType());
                        
                        var concreteType =
                            genericType.Assembly.GetTypes().Where(
                                t => genericType.IsAssignableFrom(t) 
                                    && !t.IsInterface 
                                    && !t.IsAbstract).FirstOrDefault();

                        var instance = Activator.CreateInstance(concreteType);
                        (value as IList).Add(instance);
                    }
                }
                catch (Exception)
                {
                    Console.WriteLine(String.Format("Unable to set property: {0}", prop));
                }
            }

            var clone = (HydroNetwork)network.Clone();
            TestReferenceHelper.AssertStringRepresentationOfGraphIsEqual(network, clone);
        }

        [Test]
        [Category(TestCategory.Integration)]
        public void CloneHydroNetworkWithLinkSources()
        {
            var network = HydroNetworkHelper.GetSnakeHydroNetwork(1);
            var catchment = new Catchment();
            network.Catchments.Add(catchment);
            var wasteWaterTreatmentPlant = new WasteWaterTreatmentPlant();
            network.WasteWaterTreatmentPlants.Add(wasteWaterTreatmentPlant);

            var lateral = new LateralSource();
            network.Branches.First().BranchFeatures.Add(lateral);

            catchment.OutgoingLinks.Add(wasteWaterTreatmentPlant);
            catchment.OutgoingLinks.Add(lateral);
            wasteWaterTreatmentPlant.IncomingLinks.Add(catchment);
            lateral.IncomingLinks.Add(catchment);

            var clone = (HydroNetwork)network.Clone();
            var links = TestReferenceHelper.SearchObjectInObjectGraph(catchment, clone);
            links.ForEach(Console.WriteLine);
            Assert.AreEqual(0, links.Count);

            Assert.AreEqual(2, clone.Catchments.First().OutgoingLinks.Count);
            Assert.AreEqual(1, clone.WasteWaterTreatmentPlants.First().IncomingLinks.Count);
            Assert.AreEqual(1, clone.LateralSources.First().IncomingLinks.Count);
        }

        [Test]
        [Category(TestCategory.Integration)]
        public void CloneHydroNetwork()
        {
            var network = new HydroNetwork();
            var from = new HydroNode();
            var to = new HydroNode();
            network.Nodes.Add(from);
            network.Nodes.Add(to);
            var channel = new Channel {Source = from, Target = to};
            network.Branches.Add(channel);
            network.CrossSectionSectionTypes.Add(new CrossSectionSectionType {Name = "JemigdePemig"});
            var crossSectionSectionTypesCount = network.CrossSectionSectionTypes.Count;
            // The default CrossSectionSectionType and JDP
            Assert.AreEqual(2, crossSectionSectionTypesCount);
            var clonedHydroNetwork = (IHydroNetwork) network.Clone();
            clonedHydroNetwork.GetType().Should().Be.EqualTo(typeof (HydroNetwork));

            clonedHydroNetwork.Branches.Count.Should().Be.EqualTo(1);
            clonedHydroNetwork.Nodes.Count.Should().Be.EqualTo(2);
            clonedHydroNetwork.CrossSectionSectionTypes.Count.Should().Be.EqualTo(crossSectionSectionTypesCount);
            Assert.AreEqual("JemigdePemig", clonedHydroNetwork.CrossSectionSectionTypes.Last().Name);
        }

        [Test]
        [Category(TestCategory.Integration)]
        public void CloneHydroNetworkWithCrossSectionSectionTypes()
        {
            var network = new HydroNetwork();
            var crossSectionSectionType = new CrossSectionSectionType{Name = "Jan"};
            network.CrossSectionSectionTypes.Add(crossSectionSectionType);
            crossSectionSectionType.Id = 666;//debug easy by idd
            var from = new HydroNode();
            var to = new HydroNode();
            network.Nodes.Add(from);
            network.Nodes.Add(to);
            var channel = new Channel { Source = from, Target = to };
            network.Branches.Add(channel);
            var crossSectionXYZ = new CrossSectionDefinitionXYZ
            {
                Geometry = new LineString(new[] { new Coordinate(0, 0), new Coordinate(10, 0) })
            };

            crossSectionXYZ.Sections.Add(new CrossSectionSection { SectionType = crossSectionSectionType });

            HydroNetworkHelper.AddCrossSectionDefinitionToBranch(channel, crossSectionXYZ, 0);

            var clonedHydroNetwork = (IHydroNetwork)network.Clone();
            clonedHydroNetwork.CrossSections.Should().Have.Count.EqualTo(1);
            var cloneCrossSection = clonedHydroNetwork.CrossSections.FirstOrDefault();
            var clonedType = clonedHydroNetwork.CrossSectionSectionTypes.FirstOrDefault(t => t.Name == "Jan");
            
            //the type should be cloned
            Assert.AreNotEqual(clonedType, crossSectionSectionType);
            //the crosssection reference should be updated to use the cloned type
            Assert.AreEqual(clonedType, cloneCrossSection.Definition.Sections[0].SectionType);
        }

        [Test]
        public void ClonedNetworkIsCollected()
        {
            //issue 5410 openda memory problems
            var weakReference = new WeakReference(null);
            HydroNetwork network = GetNetwork();
            for (int i = 0; i < 10;i++ )
            {
                weakReference.Target = network.Clone();//create clones that get out of scope
            }
            GC.Collect();
            //test it was collected
            Assert.IsNull(weakReference.Target);
        }


        private HydroNetwork GetNetwork()
        {
            var network = new HydroNetwork();
            var crossSectionSectionType = new CrossSectionSectionType { Name = "Jan" };
            network.CrossSectionSectionTypes.Add(crossSectionSectionType);
            crossSectionSectionType.Id = 666;//debug easy by idd
            var from = new HydroNode();
            var to = new HydroNode();
            network.Nodes.Add(from);
            network.Nodes.Add(to);
            var channel = new Channel { Source = from, Target = to };
            network.Branches.Add(channel);
            var crossSectionXYZ = new CrossSectionDefinitionXYZ
                                      {
                                          Geometry = new LineString(new[] { new Coordinate(0, 0), new Coordinate(10, 0) })
                                      };

            crossSectionXYZ.Sections.Add(new CrossSectionSection { SectionType = crossSectionSectionType });

            HydroNetworkHelper.AddCrossSectionDefinitionToBranch(channel, crossSectionXYZ, 0);
            return network;
        }


        [Test]
        [Category(TestCategory.Integration)]
        public void CloneHydroNetworkWithCrossSection()
        {
            var network = new HydroNetwork();
            var from = new HydroNode();
            var to = new HydroNode();
            network.Nodes.Add(from);
            network.Nodes.Add(to);
            var channel = new Channel {Source = from, Target = to};
            network.Branches.Add(channel);
            var crossSectionXYZ = new CrossSectionDefinitionXYZ
                                      {
                                          Geometry = new LineString(new[] {new Coordinate(0, 0), new Coordinate(10, 0)})
                                      };
            
            HydroNetworkHelper.AddCrossSectionDefinitionToBranch(channel, crossSectionXYZ, 0);

            var clonedHydroNetwork = (IHydroNetwork) network.Clone();
            clonedHydroNetwork.CrossSections.Should().Have.Count.EqualTo(1);
        }

        [Test]
        [Category(TestCategory.Integration)]
        public void CloneHydroNetworkWithVariousBranchFeatures()
        {
            var network = new HydroNetwork();
            var from = new HydroNode();
            var to = new HydroNode();
            network.Nodes.Add(from);
            network.Nodes.Add(to);
            var channel = new Channel {Source = from, Target = to};
            network.Branches.Add(channel);
            var compositeBranchStructure = new CompositeBranchStructure();
            NetworkHelper.AddBranchFeatureToBranch(compositeBranchStructure, channel, 0);
            HydroNetworkHelper.AddStructureToComposite(compositeBranchStructure, new Weir());
            HydroNetworkHelper.AddStructureToComposite(compositeBranchStructure, new Pump());

            var crossSectionXYZ = new CrossSectionDefinitionXYZ
            {
                Geometry = new LineString(new[] { new Coordinate(0, 0), new Coordinate(10, 0) })
            };
            HydroNetworkHelper.AddCrossSectionDefinitionToBranch(channel, crossSectionXYZ, 0);

            var clonedHydroNetwork = (IHydroNetwork) network.Clone();
            clonedHydroNetwork.CrossSections.Should().Have.Count.EqualTo(1);
            clonedHydroNetwork.CompositeBranchStructures.Should().Have.Count.EqualTo(1);
            clonedHydroNetwork.Weirs.Should().Have.Count.EqualTo(1);
            clonedHydroNetwork.Pumps.Should().Have.Count.EqualTo(1);
        }

        [Test]
        [Category(TestCategory.Integration)]
        public void CloneHydroNetworkAndAddBranch()
        {
            var network = new HydroNetwork();
            var from = new HydroNode();
            var to = new HydroNode();
            network.Nodes.Add(from);
            network.Nodes.Add(to);
            var channel = new Channel { Source = from, Target = to };
            network.Branches.Add(channel);

            var clonedNetwork = (IHydroNetwork)network.Clone();

            var from2 = new HydroNode("from2");
            var to2 = new HydroNode("to2");
            clonedNetwork.Nodes.Add(from2);
            clonedNetwork.Nodes.Add(to2);
            var channel2 = new Channel {Name = "channel2", Source = from2, Target = to2 };
            clonedNetwork.Branches.Add(channel2);

            Assert.AreEqual(1,network.Branches.Count);
            Assert.AreEqual(2, clonedNetwork.Branches.Count);
        }

        [Test]
        public void GetAllItemsRecursive()
        {
            //TODO: expand the asserts..
            var network = new HydroNetwork();
            var allItems = network.GetAllItemsRecursive().ToArray();
            Assert.AreEqual(new object[] { network, network.CrossSectionSectionTypes[0] }, allItems);
        }

        [Test]
        public void CannotRemoveSectionTypesThatAreUsedByCrossSections()
        {
            //setup a network with a crossection and a sectiontype that is used
            var channel = new Channel();
            var network = new HydroNetwork();
            var crossSectionZW = new CrossSectionDefinitionZW();
            var crossSectionSectionType = new CrossSectionSectionType();
            
            crossSectionZW.Sections.Add(new CrossSectionSection { SectionType = crossSectionSectionType });
            HydroNetworkHelper.AddCrossSectionDefinitionToBranch(channel,crossSectionZW,0.0);
            
            network.CrossSectionSectionTypes.Add(crossSectionSectionType);
            network.Branches.Add(channel);

            
            //action! remove the sectiontype
            network.CrossSectionSectionTypes.Remove(crossSectionSectionType);

            //still have 2. one plus a 'default'?
            Assert.AreEqual(2,network.CrossSectionSectionTypes.Count);

            Assert.IsTrue(network.CrossSectionSectionTypes.Contains(crossSectionSectionType));
                
        }
        

   }
}