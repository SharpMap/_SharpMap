using System;
using System.Xml.Serialization;

namespace DelftTools.Tests.Utils.Xml.Serialization.TestClasses
{
    [Serializable]
    [XmlRoot("baseclass", Namespace = "", IsNullable = false)]
    public class BaseClass
    {
        protected int id = 2;

        [XmlElement("id", DataType = "int")]
        public virtual int Id
        {
            get { return id; }
            set { id = value; }
        }
    }
}