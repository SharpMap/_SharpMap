using System;
using System.Runtime.Serialization;
//using ProtoBuf;

namespace DelftTools.Utils.Remoting
{
    [DataContract]
    //[ProtoContract]
    public class SerializableType
    {
        [DataMember(Name = "FullName", Order = 0)]
        //[ProtoMember(1)]
        private string FullName { get; set; }

        public SerializableType() {}

        public SerializableType(Type type)
        {
            FullName = type.FullName;
        }

        public static implicit operator SerializableType(Type type)
        {
            return new SerializableType(type);
        }

        public static implicit operator Type(SerializableType type)
        {
            return Type.GetType(type.FullName);
        }
    }
}