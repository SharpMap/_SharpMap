using System;
using System.Collections;
using System.Runtime.Serialization;
using DelftTools.Utils.Remoting.SharedMemory;
//using ProtoBuf;

namespace DelftTools.Utils.Remoting
{
    [DataContract]
    //[ProtoContract, ProtoInclude(100, typeof(SharedMemoryArray))]
    public class SerializableArray
    {
        #region value arrays

        [DataMember(Order = 0)] 
        //[ProtoMember(1)]
        private float[] valuesFloat;

        [DataMember(Order = 1)]
        //[ProtoMember(2)]
        private byte[] valuesByte;

        [DataMember(Order = 2)]
        //[ProtoMember(3)]
        private int[] valuesInt;

        [DataMember(Order = 3)]
        //[ProtoMember(4)]
        private short[] valuesShort;

        #endregion

        #region Constructors and Conversion

        public SerializableArray() {}

        public SerializableArray(Array array)
        {
            SetActiveArray(array);
        }
        
        public static implicit operator SerializableArray(Array array)
        {
            return new SerializableArray(array);
        }

        public static implicit operator Array(SerializableArray sarray)
        {
            return sarray.GetActiveArray();
        }

        public IList AsList()
        {
            return GetActiveArray();
        }

        #endregion

        private void SetActiveArray(Array array)
        {
            if (array is float[])
            {
                valuesFloat = (float[])array;
            }
            else if (array is short[])
            {
                valuesShort = (short[]) array;
            }
            else if (array is int[])
            {
                valuesInt = (int[]) array;
            }
            else if (array is byte[])
            {
                valuesByte = (byte[])array;
            }
        }

        private Array GetActiveArray()
        {
            if (valuesFloat != null)
            {
                return valuesFloat;
            }
            else if (valuesInt != null)
            {
                return valuesInt;
            }
            else if (valuesShort != null)
            {
                return valuesShort;
            }
            else if (valuesByte != null)
            {
                return valuesByte;
            }
            return null;
        }
    }
}