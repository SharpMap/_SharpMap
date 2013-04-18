using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.ChunkingBinding;
using System.ServiceModel.Description;
using System.Threading;
using log4net;

namespace DelftTools.Utils.Remoting
{
    /// <summary>
    /// Runner is responsible for WCF initializaiton on client / server sides
    /// 
    /// TODO: extract functionality to be used on client / server into separate classes and use them here as private
    /// </summary>
    public class RemoteInstanceRunner : IDisposable
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(RemoteInstanceContainer));

        /// <summary>
        /// Default path to the server executable;
        /// </summary>
        public static string ServerExecutable = "DelftTools.Utils.RemoteInstanceServer.exe";

        public bool IsRunning { get; private set; }

        public Process Process { get; private set; }

        public int Port { get; private set; }

        public Type TypeInterface { get; private set; }

        public Type TypeClass { get; private set; }

        public object Instance { get; private set; }

        private ChannelFactory factory; // on client

        private ServiceHost host; // on server

        public RemoteInstanceRunner(Type typeInterface, Type typeClass, int port)
        {
            Port = port;
            TypeInterface = typeInterface;
            TypeClass = typeClass;
        }

        public void Run()
        {

            if (IsRunning)
            {
                throw new InvalidOperationException("Remote instance server is already running.");
            }

            // start new process
            Process = new Process
                          {
                              EnableRaisingEvents = true,
                              StartInfo =
                                  {
                                      FileName = ServerExecutable,
                                      Arguments = string.Format("\"-a={0}\" -i=\"{1}\" -c=\"{2}\" -p={3}",
                                                                TypeInterface.Assembly.Location,
                                                                TypeInterface.FullName,
                                                                TypeClass.FullName,
                                                                Port),
                                      WorkingDirectory = Directory.GetCurrentDirectory(),
                                      UseShellExecute = false,
                                      CreateNoWindow = true
                                  }
                          };
            
            Process.Exited += process_Exited;
            try
            {
                Process.Start();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Error starting RemoteInstanceRunner",ex);
                
            }


            log.Debug("Starting process: \"" + Process.StartInfo.FileName + " " + Process.StartInfo.Arguments + "\"");

            var runningStatusFile = GetStatusFilePathPrefix(TypeClass.Name, Port);
            
            while (!File.Exists(runningStatusFile))
            {
                if (Process == null) // DON'T REMOVE!
                {
                    var error = TryReadErrorFromFile();
                    throw new InvalidProgramException(String.Format("Server process has exited. Error: {0}", error));
                }
                Thread.Sleep(50);
            }

            log.Debug("Remote instance server has been started successfully.");

            IsRunning = true;
        }

        private string TryReadErrorFromFile()
        {
            var path = GetErrorFilePath(TypeClass.Name,Port);

            var error = "";

            try
            {
                error = File.ReadAllText(path);
                File.Delete(path);
            }
            catch(Exception e)
            {
                error = "<Unable to read remote instance error message>";
            }

            return error;
        }

        public static string GetErrorFilePath(string type, int port)
        {
            return GetStatusFilePathPrefix(type, port) + ".error";
        }

        public void Kill()
        {
            log.DebugFormat("Killing server, type: {0}, port: {1}", TypeInterface, Port);

            if (Process != null)
            {
                Process.Exited -= process_Exited;

                if (!Process.HasExited)
                {
                    Process.Kill();

                    CleanupStatusFile();
                }

                Process = null;
            }

            IsRunning = false;
        }

        private void process_Exited(object sender, EventArgs e)
        {
            if (Exited != null)
            {
                Exited(this, null);
            }

            log.Debug("process exited.");
            Process = null;
            IsRunning = false;
        }

        public static string GetStatusFilePathPrefix(string typeName, int port)
        {
            return Path.GetTempPath() + ServerExecutable + "." + typeName + "." + port;
        }

        public static string GetUri(Type type, int port)
        {
            return string.Format("net.pipe://localhost/{1}_{0}", port, type.Name);
        }

        private bool isDisposed;

        public void Dispose()
        {
            if (isDisposed)
            {
                return;
            }

            if (host != null) // server
            {
                host.Close();
            }

            if(factory != null)
            {
                try
                {
                    factory.Close();
                }
                catch (CommunicationException)
                {
                    factory.Abort();
                }
                catch (TimeoutException)
                {
                    factory.Abort();
                }
                catch (Exception)
                {
                    factory.Abort();
                    throw;
                }

                ((IDisposable)factory).Dispose();
            }

            Kill();
        }

        ~RemoteInstanceRunner()
        {
            Dispose();
        }

        /// <summary>
        /// Server
        /// </summary>
        /// <param name="typeClass"></param>
        /// <param name="typeInterface"></param>
        /// <param name="port"></param>
        public void StartServer()
        {
            var runningStatusFilePath = GetStatusFilePathPrefix(TypeClass.Name, Port);

            if (File.Exists(runningStatusFilePath))
            {
                // TODO: check if we can delete it, otherwise throw error
            }

            var uri = GetUri(TypeClass, Port);
            host = new ServiceHost(TypeClass, new Uri(uri));

            //EnableDebugging();

            var binding = GetChunkedBinding();

            host.AddServiceEndpoint(TypeInterface, binding, new Uri(uri));
            host.Description.Behaviors.Add(new RemoteServiceBehavior());
            host.Open();

            var file = File.CreateText(runningStatusFilePath);
            
            IsRunning = true;
        }

        public void StopServer()
        {
            if (IsRunning)
            {
                IsRunning = false;
                host.Close(new TimeSpan(0, 0, 1));

                CleanupStatusFile();
            }
        }

        private void CleanupStatusFile()
        {
            Thread.Sleep(100);

            var statusFile = GetStatusFilePathPrefix(TypeClass.Name,Port);
            if (File.Exists(statusFile))
            {
                try
                {
                    File.Delete(statusFile);
                }
                catch (Exception)
                {
                    //swallow exception
                }
            }
        }

        /// <summary>
        /// Client
        /// </summary>
        /// <typeparam name="TServiceInterface"></typeparam>
        /// <typeparam name="TServiceClass"></typeparam>
        /// <param name="uri"></param>
        /// <returns></returns>
        public TServiceInterface GetRemoteInstance<TServiceInterface>() where TServiceInterface:class
        {
            var address = new EndpointAddress(GetUri(TypeClass, Port));

            var binding = GetChunkedBinding();

            factory = new ChannelFactory<TServiceInterface>(binding, address);

            foreach (var operation in factory.Endpoint.Contract.Operations)
            {
                var behavior = operation.Behaviors.OfType<DataContractSerializerOperationBehavior>().FirstOrDefault();
                if (behavior != null)
                {
                    behavior.MaxItemsInObjectGraph = MaxTotalSize;
                }
            }

            var proxy = ((ChannelFactory<TServiceInterface>) factory).CreateChannel();
            return proxy;
        }

        private NamedPipeChunkingBinding GetChunkedBinding()
        {
            return new NamedPipeChunkingBinding
                       {
                           MaxBufferedChunks = 2048,
                           SetChunkSize = MaxChuckSize,
                           MaxReceivedMessageSize = MaxChunckBufferSize,
                           MaxBufferSize = MaxChunckBufferSize,
                           ReaderQuotas =
                               {
                                   MaxArrayLength = MaxTotalSize,
                                   MaxBytesPerRead = MaxTotalSize,
                                   MaxStringContentLength = MaxTotalSize
                               }
                       };
        }

        public const int MaxChuckSize = 64*65536;
        public const int MaxChunckBufferSize = (int)(MaxChuckSize * 1.05);
        public const int MaxTotalSize = 1000000000;

        private void EnableDebugging()
        {
            var debug = host.Description.Behaviors.Find<ServiceDebugBehavior>();

            // if not found - add behavior with setting turned on 
            if (debug == null)
            {
                host.Description.Behaviors.Add(
                    new ServiceDebugBehavior() { IncludeExceptionDetailInFaults = true });
            }
            else
            {
                // make sure setting is turned ON
                if (!debug.IncludeExceptionDetailInFaults)
                {
                    debug.IncludeExceptionDetailInFaults = true;
                }
            }
        }

        public event EventHandler Exited;
    }
}