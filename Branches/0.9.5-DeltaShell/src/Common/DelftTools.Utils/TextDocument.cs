using System;
using DelftTools.Utils.Aop;
using DelftTools.Utils.Data;

namespace DelftTools.Utils
{
    [Entity(FireOnCollectionChange = false)]
    public class TextDocument : EditableObjectUnique<long>, INameable, ICloneable
    {
        private bool readOnly;

        public TextDocument():this(false){}

        
        public TextDocument(bool readOnly)
        {
           this.readOnly = readOnly;
        }
        
        public virtual bool ReadOnly
        {
            get { return readOnly; }
        }

        public virtual string Name { get; set; }

        public override string ToString()
        {
            return Name;
        }
        
        //don't use ReadOnly it messes up data binding
        //[ReadOnly(true)]
        public virtual string Content { get; set; }

        public object Clone()
        {
            var clone = new TextDocument(readOnly)
                            {
                                Name = Name,
                                Content = Content
                            };
            return clone;

        }
    }
}