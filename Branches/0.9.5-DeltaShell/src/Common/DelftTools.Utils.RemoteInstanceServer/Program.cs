using System;
using System.IO;
using System.IO.Pipes;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using ProtoBufRemote;

namespace DelftTools.Utils.RemoteInstanceServer
{
    class Program
    {
        private static Mutex weAreAliveMutex;
        private static bool streamsCreated;
        private static NamedPipeServerStream pipeServerStreamIn;
        private static NamedPipeServerStream pipeServerStreamOut;

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern void SetDllDirectory(string lpPathName);
        
        static void Main(string[] args)
        {
            InitializeErrorHandling();

            //Debugger.Launch();

            if (args.Length != 6)
            {
                Console.WriteLine("Invalid number of arguments, quitting");
                return; //nothing to do
            }

            var interfaceAssemblyPath = args[0];
            var interfaceName = args[1];
            var implAssemblyPath = args[2];
            var implName = args[3];
            var pipeName = args[4];
            var typeConverters = args[5];
            
            var interfaceAssembly = Assembly.LoadFile(Path.GetFullPath(interfaceAssemblyPath));
            var interfaceType = interfaceAssembly.GetType(interfaceName, true);

            var implAssembly = Assembly.LoadFile(implAssemblyPath);
            var implType = implAssembly.GetType(implName, true);
            
            ProcessTypeConverters(typeConverters);

            var implAssemblyDir = Path.GetDirectoryName(implAssemblyPath);
            SetDllDirectory(implAssemblyDir);
            
            SetupMutexes(pipeName);

            StartServer(interfaceType, implType, pipeName);
        }
        
        private static void ProcessTypeConverters(string typeConverters)
        {
            var types = typeConverters.Split('|');
            foreach (var type in types)
            {
                var inst = (ITypeToProtoConverter) Activator.CreateInstance(Type.GetType(type));
                RemotingTypeConverters.RegisterTypeConverter(inst);
            }
        }

        private static void StartServer(Type interfaceType, Type implType, string pipeName)
        {
            //create the server
            var controller = new RpcController();
            
            var server = new RpcServer(controller);

            //register the service with the server. We must specify the interface explicitly since we did not use attributes
            server.GetType()
                  .GetMethod("RegisterService")
                  .MakeGenericMethod(interfaceType)
                  .Invoke(server, new[] {Activator.CreateInstance(implType)});

            //build the connection using named pipes
            try
            {
                pipeServerStreamIn = CreateNamedPipe(pipeName + "ctos", PipeDirection.In);
                pipeServerStreamOut = CreateNamedPipe(pipeName + "stoc", PipeDirection.Out);
                streamsCreated = true;
                pipeServerStreamIn.WaitForConnection();
                pipeServerStreamOut.WaitForConnection();
                
                //create and start the channel which will receive requests
                var channel = new StreamRpcChannel(controller, pipeServerStreamIn, pipeServerStreamOut, useSharedMemory: true);
                channel.Start();
            }
            catch (IOException e)
            {
                //swallow and exit
                Console.WriteLine("Something went wrong (pipes busy?), quitting: " + e);
                throw;
            }
        }

        private static NamedPipeServerStream CreateNamedPipe(string pipeName, PipeDirection pipeDirection, int retry=10)
        {
            try
            {
                return new NamedPipeServerStream(pipeName, pipeDirection, 1);
            }
            catch (IOException)
            {
                Thread.Sleep(50); // wait for pipe to become available..
                return CreateNamedPipe(pipeName, pipeDirection, retry - 1);
            }
        }

        #region Error handling

        [DllImport("kernel32.dll")]
        static extern ErrorModes SetErrorMode(ErrorModes uMode);

        [Flags]
        public enum ErrorModes : uint
        {
            SYSTEM_DEFAULT = 0x0,
            SEM_FAILCRITICALERRORS = 0x0001,
            SEM_NOALIGNMENTFAULTEXCEPT = 0x0004,
            SEM_NOGPFAULTERRORBOX = 0x0002,
            SEM_NOOPENFILEERRORBOX = 0x8000
        }

        private static void InitializeErrorHandling()
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomainUnhandledException;
            const ErrorModes desiredErrorModes = ErrorModes.SEM_FAILCRITICALERRORS | ErrorModes.SEM_NOGPFAULTERRORBOX;
            var dwMode = SetErrorMode(desiredErrorModes);
            SetErrorMode(dwMode | desiredErrorModes);
        }

        static void CurrentDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            AttemptToCloseStreams();
            
            File.AppendAllText(Path.Combine(Path.GetTempPath(), "deltashell_remote.log"),
                               string.Format("{0}: {1}", DateTime.Now, e.ExceptionObject));
            Environment.Exit(-1); //prevent popup dialogs
        }

        private static void AttemptToCloseStreams()
        {
            if (streamsCreated)
            {
                try
                {
                    if (pipeServerStreamIn != null)
                        pipeServerStreamIn.Close();
                    pipeServerStreamIn = null;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
                try
                {
                    if (pipeServerStreamOut != null)
                        pipeServerStreamOut.Close();
                    pipeServerStreamOut = null;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }

        #endregion

        #region Mutexes

        private static void SetupMutexes(string mutexName)
        {
            bool created;
            weAreAliveMutex = new Mutex(true, mutexName + "_srv", out created);
            try
            {
                Console.WriteLine("Grabbing server mutex...");
                weAreAliveMutex.WaitOne(); //grab this mutex always 
                Console.WriteLine("Server mutex grabbed");
            }
            catch (AbandonedMutexException)
            {
                Console.WriteLine("Server mutex grabbed (abondoned)");
                //previous instance was forcefully exited..ok..good to know
                //gulp
            }

            var t = new Thread(() => MutexWatcher(mutexName));
            t.Start();
        }

        private static void MutexWatcher(string mutexName)
        {
            bool created;
            var mutex = new Mutex(false, mutexName, out created);
            try
            {
                mutex.WaitOne();
            }
            catch (AbandonedMutexException)
            {
                //parent process was abruptly killed!..ok
            }
            mutex.ReleaseMutex(); //we don't actually want the mutex! release it

            //if we get here, the parent process was killed, so we should exit too
            Environment.Exit(11);
        }

        #endregion
    }

    public class FakeThingToKeepProjectReferenceAlive { }
}