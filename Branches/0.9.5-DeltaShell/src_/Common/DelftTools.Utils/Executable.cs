using System.Diagnostics;

namespace DelftTools.Utils.Workflow
{
    public class Executable
    {
        private string name;
        private readonly Process process;

        public Executable()
            : this("", "")
        {
        }

        public Executable(string commandLine)
            : this(commandLine, "")
        {
        }

        public Executable(string commandLine, string arguments)
        {
            process = new Process();
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;

            CommandLine = commandLine;
            Arguments = arguments;
        }

        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        /// <summary>
        /// The Executable or batch file which will be launched for a process run
        /// </summary>
        public string CommandLine
        {
            get { return process.StartInfo.FileName; }
            set { process.StartInfo.FileName = value; }
        }

        public string Arguments
        {
            get { return process.StartInfo.Arguments; }
            set { process.StartInfo.Arguments = value; }
        }

        public string WorkingDirectory
        {
            get { return process.StartInfo.WorkingDirectory; }
            set { process.StartInfo.WorkingDirectory = value; }
        }

        public void Run()
        {
            process.Start();
        }

        public Process Process
        {
            get { return process; }
        }
    }
}