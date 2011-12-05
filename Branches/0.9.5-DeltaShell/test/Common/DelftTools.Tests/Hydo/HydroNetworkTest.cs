using System;
using System.Diagnostics;
using DelftTools.Hydro;
using DelftTools.Hydro.Helpers;
using DelftTools.Hydro.Structures;
using DelftTools.TestUtils;
using DelftTools.Utils.Collections;
using log4net;
using log4net.Core;
using NetTopologySuite.Extensions.Networks;
using NUnit.Framework;
using PostSharp;

namespace DelftTools.Tests.Hydo
{
    [TestFixture]
    public class HydroNetworkTest
    {
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


        /// <summary>
        /// Branch property of cross section should be updated when the cross section
        /// is added to or removed from a branch.
        /// </summary>
        [Test]
        public void AddCrossSectionToBranch()
        {
            var crossSection = new CrossSection();
            var branch = new Channel(new HydroNode("from"), new HydroNode("To"));
            NetworkHelper.AddBranchFeatureToBranch(branch, crossSection, crossSection.Offset);
            Assert.AreEqual(branch, crossSection.Branch);

            //branch.BranchFeatures.Clear();
            //Assert.IsNull(crossSection.Branch);
        }

        [Test]
        [Ignore("TODO: not working anymore due to refactoring; re-enable later")]
        public void AddCrossSectionToBranchUsingCollections()
        {
            var crossSection = new CrossSection();
            var branch = new Channel(new HydroNode("from"), new HydroNode("To"));

            //NetworkHelper.AddBranchFeatureToBranch(branch, crossSection, crossSection.Offset);
            branch.BranchFeatures.Add(crossSection);
            Assert.AreEqual(branch, crossSection.Branch);

            branch.BranchFeatures.Clear();
            Assert.IsNull(crossSection.Branch);
        }

        [Test]
        [Category("Performance")]
        public void AddManyBranchesWithCrossSections()
        {
            TestHelper.AssertIsFasterThan(2000,() =>
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
                                                             NetworkHelper.AddBranchFeatureToBranch(channel,
                                                                                                    new CrossSection(),
                                                                                                    0);

                                                             network.Branches.Add(channel);
                                                         }

                                                         int crossSectionCount = 0;
                                                         foreach (ICrossSection crossSection in network.CrossSections)
                                                             // access all CrossSections should be also fast
                                                         {
                                                             crossSectionCount++;
                                                         }
                                                     });
        }

        [Test]
        [Category("Performance")]
        public void AddManyBranchesWithSimpleBranchFeature()
        {
            DateTime t = DateTime.Now;

            const int count = 10000;
            var network = new HydroNetwork();
            for (int i = 0; i < count; i++)
            {
                var from = new HydroNode();
                var to = new HydroNode();

                network.Nodes.Add(from);
                network.Nodes.Add(to);

                var channel = new Channel {Source = from, Target = to};

                var compositeBranchStructure = new CompositeBranchStructure();
                NetworkHelper.AddBranchFeatureToBranch(channel, compositeBranchStructure, 0);
                HydroNetworkHelper.AddStructureToComposite(compositeBranchStructure, new Weir());

                network.Branches.Add(channel);
            }

            int weirCount = 0;
            foreach (IWeir weir in network.Weirs) // access all Weirs should be also fast
            {
                weirCount++;
            }

            TimeSpan dt = DateTime.Now - t;

            log.InfoFormat("Added {0} branches with {1} weirs in {2} sec", count, weirCount, dt.TotalSeconds);

            // 20091029 set to 5 seconds; original test only added weirs and created an invalid hydronetwork
            Assert.LessOrEqual(dt.TotalSeconds, 2.7);
        }

        [Test]
        public void BranchCrossSectionShouldRaiseCollectionChangedEvent()
        {
            var crossSection = new CrossSection();
            var branch = new Channel(new HydroNode("from"), new HydroNode("To"));

            int count = 0;
            Post.Cast<Channel, INotifyCollectionChanged>(branch).CollectionChanged += delegate { count++; };

            NetworkHelper.AddBranchFeatureToBranch(branch, crossSection, crossSection.Offset);
            Assert.AreEqual(1, count);

            branch.BranchFeatures.Clear();
            Assert.AreEqual(2, count);
        }

        [Test]
        [Category("Performance")]
        public void CloneHydroNetwork()
        {
            var network = new HydroNetwork();
            var from = new HydroNode();
            var to = new HydroNode();
            network.Nodes.Add(from);
            network.Nodes.Add(to);
            var channel = new Channel {Source = from, Target = to};
            network.Branches.Add(channel);

            var clonedHydroNetwork = (IHydroNetwork) network.Clone();
            clonedHydroNetwork.GetType().Should().Be.EqualTo(typeof (HydroNetwork));

            clonedHydroNetwork.Branches.Count.Should().Be.EqualTo(1);
            clonedHydroNetwork.Nodes.Count.Should().Be.EqualTo(2);
        }

        [Test]
        [Category("Performance")]
        public void CloneHydroNetworkWithCrossSection()
        {
            var network = new HydroNetwork();
            var from = new HydroNode();
            var to = new HydroNode();
            network.Nodes.Add(from);
            network.Nodes.Add(to);
            var channel = new Channel {Source = from, Target = to};
            network.Branches.Add(channel);
            NetworkHelper.AddBranchFeatureToBranch(channel, new CrossSection(), 0);

            var clonedHydroNetwork = (IHydroNetwork) network.Clone();
            clonedHydroNetwork.CrossSections.Should().Have.Count.EqualTo(1);
        }

        [Test]
        [Category("Integration")]
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
            NetworkHelper.AddBranchFeatureToBranch(channel, compositeBranchStructure, 0);
            HydroNetworkHelper.AddStructureToComposite(compositeBranchStructure, new Weir());
            HydroNetworkHelper.AddStructureToComposite(compositeBranchStructure, new Pump());

            NetworkHelper.AddBranchFeatureToBranch(channel, new CrossSection(), 0);

            var clonedHydroNetwork = (IHydroNetwork) network.Clone();
            clonedHydroNetwork.CrossSections.Should().Have.Count.EqualTo(1);
            clonedHydroNetwork.CompositeBranchStructures.Should().Have.Count.EqualTo(1);
            clonedHydroNetwork.Weirs.Should().Have.Count.EqualTo(1);
            clonedHydroNetwork.Pumps.Should().Have.Count.EqualTo(1);
        }
 
        [Test]
        [NUnit.Framework.Category("Performance")]
        public void CloneNetworkWithManyCrossSectionWithProcessedData()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var network = new HydroNetwork();
            var fromNode = new Node();
            var toNode = new Node();
            var branch = new Channel(fromNode, toNode, 5000);
            network.Nodes.Add(fromNode);
            network.Nodes.Add(toNode);
            network.Branches.Add(branch);

            for (var i = 0.0; i <= 5000; i++)
            {
                var crossSection = new CrossSection { Offset = i };
                var processedData = crossSection.ConveyanceData; // makes sure it is created
                branch.BranchFeatures.Add(crossSection);
            }
            stopwatch.Stop();

            log.DebugFormat("It took {0} ms to create network containing 5000 cross-sections", stopwatch.ElapsedMilliseconds);

            stopwatch = new Stopwatch();
            stopwatch.Start();
            var networkClone = network.Clone();
            stopwatch.Stop();

            log.DebugFormat("It took {0} ms to clone network containing 5000 cross-sections", stopwatch.ElapsedMilliseconds);

            stopwatch.ElapsedMilliseconds
                .Should("Clone of network containing many cross-sections with processed data should be reasonable")
                    .Be.LessThan(13000);
        }
   }
}