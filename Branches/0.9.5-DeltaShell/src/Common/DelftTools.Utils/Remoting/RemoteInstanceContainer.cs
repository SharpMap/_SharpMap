using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Threading;
using DelftTools.Utils.RemoteInstanceServer;
using ProtoBufRemote;

namespace DelftTools.Utils.Remoting
{
    public static class RemoteInstanceContainer
    {
        private static readonly IDictionary<object, RemoteProcessInfo> RunningInstances = new Dictionary<object, RemoteProcessInfo>();

        public static int NumInstances { get { return RunningInstances.Count; } }

        public static bool IsRemoteInstance(object o)
        {
            return RunningInstances.ContainsKey(o);
        }

        public static TInterface CreateInstance<TInterface, TImpl>(string workingDirectory = null, bool showConsole=false)
            where TInterface : class
            where TImpl : TInterface, new()
        {
            workingDirectory = workingDirectory ?? "";

            lock (RunningInstances)
            {
                var index = RunningInstances.Values.Select(r => r.Index).DefaultIfEmpty().Max() + 1;
                var commName = string.Format("ds-worker-{0}-{1}-{2}", Process.GetCurrentProcess().Id,
                                             Thread.CurrentThread.ManagedThreadId, index);

                var typeConverterTypes =
                    RemotingTypeConverters.RegisteredConverters.Select(c => c.GetType().AssemblyQualifiedName).ToArray();

                Console.WriteLine("Remote: Attempting to grab: {0} {1} {2}", commName, Process.GetCurrentProcess().Id,
                                  Thread.CurrentThread.ManagedThreadId);
                bool created;
                var weAreAliveMutex = new Mutex(true, commName, out created);
                if (!weAreAliveMutex.WaitOne(1000)) //grab the mutex always
                {
                    throw new InvalidOperationException(
                        string.Format("Could not start remote instance, pipe {0} taken, existing instances: {1}",
                                      commName, RunningInstances.Count));
                }
                Console.WriteLine("Remote: Pipe grabbed: {0} {1} {2}", commName, Process.GetCurrentProcess().Id,
                                  Thread.CurrentThread.ManagedThreadId);

                //create the server process
                try
                {
                    var remoteInstanceLocation = Path.GetDirectoryName(typeof (RemoteInstanceContainer).Assembly.Location);
                    var remoteInstanceExePath = Path.Combine(remoteInstanceLocation, "DelftTools.Utils.RemoteInstanceServer.exe");
                    var p = Process.Start(new ProcessStartInfo(remoteInstanceExePath,
                                                               string.Format(
                                                                   @"""{0}"" ""{1}"" ""{2}"" ""{3}"" ""{4}"" ""{5}""",
                                                                   typeof (TInterface).Assembly.Location,
                                                                   typeof (TInterface).FullName,
                                                                   typeof (TImpl).Assembly.Location,
                                                                   typeof (TImpl).FullName,
                                                                   commName,
                                                                   string.Join("|", typeConverterTypes)))
                        {
                            WindowStyle = showConsole ? ProcessWindowStyle.Normal : ProcessWindowStyle.Hidden,
                            WorkingDirectory = workingDirectory,
                        });

                    //create the client
                    var controller = new RpcController();
                    var client = new RpcClient(controller, new Mutex(false, commName + "_srv", out created));
                    var channel = SetupConnection(controller, commName);

                    var service = client.GetProxy<TInterface>();

                    RunningInstances.Add(service, new RemoteProcessInfo(p, weAreAliveMutex, commName, index, channel));

                    Console.WriteLine("Remote: Successful creation: {0} {1} {2}", commName, Process.GetCurrentProcess().Id,
                                      Thread.CurrentThread.ManagedThreadId);
                    return service;
                }
                catch (Exception)
                {
                    Console.WriteLine("Remote: Failed creation: {0} {1} {2}", commName, Process.GetCurrentProcess().Id,
                                      Thread.CurrentThread.ManagedThreadId);
                    weAreAliveMutex.ReleaseMutex();
                    throw;
                }
            }
        }

        private static StreamRpcChannel SetupConnection(RpcController controller, string pipeName)
        {
            // create stream (IPC only)
            var pipeClientStreamIn = new NamedPipeClientStream(".", pipeName + "stoc", PipeDirection.In);
            var pipeClientStreamOut = new NamedPipeClientStream(".", pipeName + "ctos", PipeDirection.Out);

            try
            {
                pipeClientStreamIn.Connect(15000); //15secs to connect, otherwise fail
                pipeClientStreamOut.Connect(1000);
            }
            catch (TimeoutException)
            {
                throw new InvalidOperationException(
                    "Unable to connect to remote instance: instance crashed during initialization, or existing instance still running?");
            }

            //create and start the channel which will receive requests
            var channel = new StreamRpcChannel(controller, pipeClientStreamIn, pipeClientStreamOut,
                                               useSharedMemory: true);
            channel.Start();
            return channel;
        }

        private static FakeThingToKeepProjectReferenceAlive t;

        public static void RemoveInstance(object service)
        {
            lock (RunningInstances)
            {
                RemoteProcessInfo processInfo;
                if (RunningInstances.TryGetValue(service, out processInfo))
                {
                    processInfo.EndProcess();
                    RunningInstances.Remove(service);
                }
            }
        }

        static RemoteInstanceContainer()
        {
            AppDomain.CurrentDomain.DomainUnload += CurrentDomainUnload;
            RemotingTypeConverters.RegisterTypeConverter(new TypeToProtoConverter());
            RemotingTypeConverters.RegisterTypeConverter(new DateTimeToProtoConverter());
            RemotingTypeConverters.RegisterTypeConverter(new DateTimeArrayToProtoConverter());
            RemotingTypeConverters.RegisterTypeConverter(new TimeSpanToProtoConverter());
        }

        static void CurrentDomainUnload(object sender, EventArgs e)
        {
            // cleanup lingering instances
            lock (RunningInstances)
            {
                foreach (var runningInstance in RunningInstances.Values)
                {
                    runningInstance.EndProcess(false); //this thread does not own it
                }
                RunningInstances.Clear();
            }
        }

        private class RemoteProcessInfo
        {
            public int Index { get; set; }
            private readonly string pipeName;
            private readonly StreamRpcChannel channel;
            private Mutex ourMutex;
            private Process process;
            private int owningThread;

            public RemoteProcessInfo(Process process, Mutex ourMutex, string pipeName, int index, StreamRpcChannel channel)
            {
                owningThread = Thread.CurrentThread.ManagedThreadId;
                Index = index;
                this.process = process;
                this.ourMutex = ourMutex;
                this.pipeName = pipeName;
                this.channel = channel;
            }

            public void EndProcess(bool releaseMutex=true)
            {
                lock (this)
                {
                    Console.WriteLine("Remote: Ending process: {0}, {1}-{2} (release:{3})", pipeName,
                                      Process.GetCurrentProcess().Id,
                                      Thread.CurrentThread.ManagedThreadId, releaseMutex);

                    if (channel != null)
                    {
                        channel.CloseAndJoin(true, false); //do not wait for the closing; may hang??
                    }

                    if (releaseMutex)
                    {
                        if (owningThread != Thread.CurrentThread.ManagedThreadId)
                        {
                            throw new InvalidOperationException(
                                "You must remove the remote instance from the same thread you created it on");
                        }

                        if (ourMutex != null)
                        {
                            ourMutex.ReleaseMutex();
                            ourMutex = null;
                        }
                    }

                    if (process == null)
                        return;

                    try
                    {
                        process.Kill();
                    }
                    catch (Exception)
                    {
                    }
                    process = null;
                }
            }
        }
    }
}