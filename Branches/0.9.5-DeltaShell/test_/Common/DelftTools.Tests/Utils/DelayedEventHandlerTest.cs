using System;
using System.ComponentModel;
using System.Threading;
using DelftTools.TestUtils;
using DelftTools.Utils;
using DelftTools.Utils.Threading;
using log4net;
using NUnit.Framework;

namespace DelftTools.Tests.Utils
{
    [TestFixture]
    public class DelayedEventHandlerTest
    {
        private event PropertyChangedEventHandler PropertyChanged;

        [SetUp]
        public void SetUp()
        {
            LogHelper.ConfigureLogging();
            var log = LogManager.GetLogger(typeof(DelayedEventHandlerTest)); // required in order to get messages from another thread
            log.DebugFormat("Initializing logging");
        }

        [Test]
        public void Cancel()
        {
            // ...
        }
        
        [Test]
        public void Calling10TimesWithinTimeLimitShouldResultInSingleCall()
        {
            var callCount = 0;

            var eventHandler = new DelayedEventHandler<PropertyChangedEventArgs>(delegate(object sender, PropertyChangedEventArgs e)
                                                                                     {
                                                                                         callCount++; 
                                                                                         Assert.AreEqual("9", e.PropertyName);
                                                                                     });
            PropertyChanged += eventHandler;

            for (var i = 0; i < 10; i++)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(i.ToString()));
            }

            PropertyChanged -= eventHandler;

            while (eventHandler.IsRunning || eventHandler.HasEventsToProcess)
            {
                Thread.Sleep(10);
            }

            callCount
                .Should().Be.EqualTo(1);
        }

        [Test]
        public void ComplexScenarioWhereEventsAreFiredWhileActionIsPerformed()
        {
            var callCount = 0;

            var eventHandler = new DelayedEventHandler<PropertyChangedEventArgs>(delegate
                                                                                     {
                                                                                         Thread.Sleep(10); 
                                                                                         callCount++;
                                                                                     });
            PropertyChanged += eventHandler;

            for (var i = 0; i < 10; i++)
            {
                PropertyChanged(this, null);
            }

            PropertyChanged -= eventHandler;

            while (eventHandler.IsRunning || eventHandler.HasEventsToProcess)
            {
                Thread.Sleep(10);
            }

            callCount
                .Should().Be.EqualTo(1);
        }
    }
}