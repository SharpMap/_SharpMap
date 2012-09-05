using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace DelftTools.Utils.Remoting.SharedMemory
{
    public class SharedMemoryBlob
    {
        private readonly Process process;
        private readonly IntPtr baseAddress;

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, uint nSize, out UIntPtr lpNumberOfBytesWritten);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, [Out] byte[] lpBuffer, int dwSize, out int lpNumberOfBytesRead);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, IntPtr lpBuffer, int dwSize, out int lpNumberOfBytesRead);

        public SharedMemoryBlob(Process process, IntPtr baseAddress)
        {
            this.process = process;
            this.baseAddress = baseAddress;
        }

        public int Write(byte[] bytes)
        {
            UIntPtr retVal;

            var success = WriteProcessMemory(process.Handle, baseAddress, bytes, (uint)bytes.Length, out retVal);

            return success ? (int)retVal : -1;
        }

        public byte[] Read(int length)
        {
            var buffer = new byte[length];
            int bytesread;

            var success = ReadProcessMemory(process.Handle, baseAddress, buffer, length, out bytesread);
            
            return success ? buffer : null;
        }

        public bool ReadAndWrite(IntPtr destination, int byteLength)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            int bytesread;

            var success = ReadProcessMemory(process.Handle, baseAddress, destination, byteLength, out bytesread);
            
            stopwatch.Stop();

            Console.WriteLine("Transfering {0:0.00}mb between processes took: {1}ms", ((byteLength/1024.0)/1024.0),
                              stopwatch.ElapsedMilliseconds);

            return success;
        }
    }
}