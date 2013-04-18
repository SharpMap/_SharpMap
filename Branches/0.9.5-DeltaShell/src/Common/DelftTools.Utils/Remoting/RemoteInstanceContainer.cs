using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Channels;
using System.ServiceModel;
using System.Threading;
using DelftTools.Utils.IO;
using log4net;

namespace DelftTools.Utils.Remoting
{
    /// <summary>
    /// This class will be used by clients who wants to run instance of a class in a separate process.
    /// 
    /// Manages instances of the RemoteInstanceServer.exe. Runs server using parameters:
    /// 
    /// RemoteInstanceServer.exe [path to assembly] [type] [port]
    ///
    /// temp/
    ///     RemoteInstanceServer.TypeName.Port....... file indicating that server for a specific type is running
    /// 
    /// </summary>
    public class RemoteInstanceContainer : IDisposable
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(RemoteInstanceContainer));

        private static readonly IList<RemoteInstanceRunner> remoteInstanceServerRunners = new List<RemoteInstanceRunner>();
        private static readonly IList<object> remoteInstances = new List<object>();

        private static int startPort = 9000;
        private static int endPort = 9900;
        private static int currentPort = startPort;

        /// <summary>
        /// Connects to existing remote instance of starts server and creates a new one.
        /// </summary>
        /// <typeparam name="TServiceInterface"></typeparam>
        /// <typeparam name="TServiceClass"></typeparam>
        /// <returns></returns>
        public static TServiceInterface CreateInstance<TServiceInterface, TServiceClass>() where TServiceInterface : class
        {
            // start server and create a new instance
            var port = GetFreePort(typeof(TServiceClass));

            var interfaceType = typeof(TServiceInterface);

            log.DebugFormat("Starting server at port {0}, type {1}", port, interfaceType.Name);

            var runner = new RemoteInstanceRunner(interfaceType, typeof(TServiceClass), port);
            runner.Exited += runner_Exited;
            runner.Run();

            var remoteInstance = runner.GetRemoteInstance<TServiceInterface>();
            remoteInstances.Add(remoteInstance);

            remoteInstanceServerRunners.Add(runner);

            return remoteInstance;
        }

        static void runner_Exited(object sender, EventArgs e)
        {
            var runner = (RemoteInstanceRunner) sender;
            var index = remoteInstanceServerRunners.IndexOf(runner);
            if (index != -1)
            {
                remoteInstanceServerRunners.RemoveAt(index);
                remoteInstances.RemoveAt(index);
            }
        }

        public static void RemoveInstance(object instance)
        {
            var index = remoteInstances.IndexOf(instance);

            if(index != -1)
            {
                remoteInstances.RemoveAt(index);
                var runner = remoteInstanceServerRunners[index];
                runner.Exited -= runner_Exited;
                runner.Dispose();
                remoteInstanceServerRunners.RemoveAt(index);
            }
        }

        private static int GetFreePort(Type typeClass)
        {
            currentPort++;

            if (currentPort >= endPort)
            {
                currentPort = startPort;
            }

            var port = currentPort;

            while(FileExistsAndCantBeRemoved(RemoteInstanceRunner.GetStatusFilePathPrefix(typeClass.Name, port)))
            {
                log.DebugFormat("Status file for port {0} exists, using next port", port);
                port++;
            }

            return port;
        }

        private static bool FileExistsAndCantBeRemoved(string filePath)
        {
            try
            {
                FileUtils.DeleteIfExists(filePath);
            }
            catch
            {
                return true;
            }

            return false;
        }

        public static IEnumerable<RemoteInstanceRunner> RemoteInstanceServerRunners
        {
            get { return remoteInstanceServerRunners; }
        }

        private bool isDisposed;

        public void Dispose()
        {
            if(isDisposed)
            {
                return;
            }

            // stop all servers
            foreach (var remoteInstanceServerRunner in remoteInstanceServerRunners)
            {
                try
                {

                }
                catch (Exception)
                {
                    remoteInstanceServerRunner.Dispose();
                }
            }

            remoteInstanceServerRunners.Clear();
            remoteInstances.Clear();
        }

        ~RemoteInstanceContainer()
        {
            Dispose();
        }
    }
}