using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using DelftTools.TestUtils.TestClasses;
using DelftTools.Utils.Threading;
using DelftTools.Utils.Workflow;
using DeltaShell.IntegrationTestUtils;
using NUnit.Framework;

namespace DelftTools.Tests.Utils.Threading
{
    [TestFixture]
    public class ASynchTaskTest
    {
        [Test]
        public void OnTaskCompleted()
        {
            var counter = 0;
            var activity = new SimpleModel();
            activity.StartTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day);
            activity.TimeStep = new TimeSpan(1, 0, 0, 0);
            activity.StopTime = activity.StartTime + activity.TimeStep + activity.TimeStep;

            Action<IActivity> action = (a) => a.Execute();

            var asynchTask = new ASynchTask(activity, action);
            asynchTask.TaskCompleted += delegate { counter++; };
            asynchTask.Run();

            Thread.Sleep(100);

            //do events...otherwise taskcompleted wont run
            Application.DoEvents();
            Assert.AreEqual(activity.StartTime + activity.TimeStep, activity.CurrentTime);
            Assert.AreEqual(1, counter);
        }

        [Test]
        public void ASynchTaskDoesNotCompleteSuccesfullyIfAnExceptionWasThrown()
        {
            
            var activity = new CrashingActivity();

            var asynchTask = new ASynchTask(activity, (a) => a.Execute());
            int callCount = 0;
            asynchTask.TaskCompleted += (s,e) =>
                    {
                        callCount++;
                        Assert.IsFalse(((ASynchTask) s).TaskCompletedSuccesFully);
                    };
            asynchTask.Run();

            Thread.Sleep(100);

            //do events...otherwise taskcompleted wont run
            Application.DoEvents();
            
            Assert.AreEqual(1, callCount);
        }
    }
}
