using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
//using ProtoBuf;

namespace DelftTools.Utils.Remoting.SharedMemory
{
    [DataContract]
    public class SharedMemoryArray : SerializableArray
    {
        [DataMember(Order = 0)]
        //[ProtoMember(1)]
        public long BaseAddress;

        [DataMember(Order = 1)]
        //[ProtoMember(2)]
        public int ProcessId;

        [DataMember(Order = 2)]
        //[ProtoMember(3)]
        public int Length;

        [DataMember(Order = 3)]
        //[ProtoMember(4)]
        public SerializableType ValueType;
        
        private GCHandle GCHandle;

        private IntPtr GetBaseAddress()
        {
            return new IntPtr(BaseAddress);
        }
        
        public SharedMemoryArray() //required for serialization
        {
            
        }

        public SharedMemoryArray(IntPtr pinnedArray, Type elementType, int arrayLength) : this(elementType, arrayLength)
        {
            BaseAddress = pinnedArray.ToInt64();
        }



        public SharedMemoryArray(Array contents) : this(contents.GetType().GetElementType(), contents.Length)
        {
            //prevent GC to collect this managed memory while we're copying from the other side
            GCHandle = GCHandle.Alloc(contents, GCHandleType.Pinned); 
            //get address of (managed) array in our memory, so the other side can grab it
            BaseAddress = Marshal.UnsafeAddrOfPinnedArrayElement(contents, 0).ToInt64(); 
        }

        protected SharedMemoryArray(Type elementType, int length)
        {
            //store our processid, so the other side can find us
            ProcessId = Process.GetCurrentProcess().Id;
            Length = length;
            ValueType = elementType;
        }

        public void CopyTo(IntPtr targetAddress)
        {
            var blob = new SharedMemoryBlob(Process.GetProcessById(ProcessId), GetBaseAddress());
            blob.ReadAndWrite(targetAddress, ByteLength);
        }

        public int ByteLength
        {
            get { return Length*Marshal.SizeOf(ValueType); }
        }

        public void Cleanup() //do not use IDisposable interface, will cause unwanted cleanups
        {
            GCHandle.Free();
        }

        public Array ToArray()
        {
            var blob = new SharedMemoryBlob(Process.GetProcessById(ProcessId), GetBaseAddress());

            var array = Array.CreateInstance(ValueType, Length);
            var tempHandle = GCHandle.Alloc(array, GCHandleType.Pinned);
            var destination = Marshal.UnsafeAddrOfPinnedArrayElement(array, 0);
            blob.ReadAndWrite(destination, ByteLength);
            tempHandle.Free();
            
            return array;
        }
    }
}