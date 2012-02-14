using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using DelftTools.Utils.Remoting;
using log4net;
using log4net.Config;
using NDesk.Options;

namespace DelftTools.Utils.RemoteInstanceServer
{
    public class Program
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(Program));

        private static int timeout = 10000; // server will exit if there is no CPU activity

        private static Timer timer = new Timer(TimeoutCheck);

        private static RemoteInstanceRunner runner;

        static void Main(string[] args)
        {
            //Debugger.Launch(); //wait for debugger to launch & attach

            try
            {
                timer.Change(timeout, Timeout.Infinite);

                BasicConfigurator.Configure();

                ParseArguments(args);

                var assembly = Assembly.LoadFrom(AssemblyPath);

                var typeInterface = assembly.GetType(TypeInterface);
                var typeClass = assembly.GetType(TypeClass);

                runner = new RemoteInstanceRunner(typeInterface, typeClass, Port);

                runner.StartServer();

                AppDomain.CurrentDomain.ProcessExit += delegate
                {
                    runner.StopServer();
                };

                Console.WriteLine("Press enter to stop this process.");
                Console.ReadLine();
            }
            catch (Exception e)
            {
                WriteError(e);
                throw;
            }
        }

        private static void WriteError(Exception ex)
        {
            var className = TypeClass.Split('.').Last();

            var errorPath = RemoteInstanceRunner.GetErrorFilePath(className, Port);

            string error = ex.Message;

            if (ex.InnerException != null)
            {
                error += " (Inner Exception: ";
                error += ex.InnerException.Message;
                error += ")";
            }

            try
            {
                File.WriteAllText(errorPath, error);
            }
            catch (Exception e)
            {
            }
        }

        /// <summary>
        /// Note/todo: Ugly and will not work correctly if multiple processes exist with the same name!!
        /// </summary>
        /// <returns></returns>
        private static Process GetParentProcess()
        {
            try
            {
                var pc = new PerformanceCounter("Process",
                                                "Creating Process ID",
                                                Process.GetCurrentProcess().ProcessName);

                return Process.GetProcessById((int)pc.NextValue());
            }
            catch (Exception)
            {
                return null;
            } 
        }

        private static void TimeoutCheck(object state)
        {
            if(!IsAlive() && GetParentProcess() == null)
            {
                log.Debug("Timeout!");

                if (runner.IsRunning)
                {
                    runner.StopServer();
                }

                Environment.Exit(0);
            }

            timer.Change(timeout, Timeout.Infinite);
        }

        private static int Port;
        
        private static string TypeInterface;
        
        private static string TypeClass;

        private static string AssemblyPath;

        private static void ParseArguments(IEnumerable<string> arguments)
        {
            var p = new OptionSet
                        {
                            { "p|port=", delegate(string port) { Port = int.Parse(port); }},
                            { "i|type-interface=", delegate(string typeInterface) { TypeInterface = typeInterface; } },
                            { "c|type-class=", delegate(string typeClass) { TypeClass = typeClass; } },
                            { "a|assembly-path=", delegate(string assemblyPath) { AssemblyPath = assemblyPath; } }
                        };

            p.Parse(arguments);

            if(Port == 0)
            {
                Console.WriteLine("Error: no port specified");
                Environment.Exit(1);
            }

            if (AssemblyPath == "")
            {
                Console.WriteLine("Error: no assembly specified");
                Environment.Exit(1);
            }

            if (!File.Exists(AssemblyPath))
            {
                Console.WriteLine("Error: assembly does not exist");
                Environment.Exit(1);
            }

            if (TypeInterface == "")
            {
                Console.WriteLine("Error: no type specified");
                Environment.Exit(1);
            }


        }

        static bool IsAlive()
        {
            var process = Process.GetCurrentProcess();
            var beginCpuTime = process.TotalProcessorTime;
            
            //... wait a while
            process.Refresh();
            
            var endCpuTime = process.TotalProcessorTime;
            
            return endCpuTime - beginCpuTime >= TimeSpan.FromMilliseconds(timeout);
        }
    }
}
