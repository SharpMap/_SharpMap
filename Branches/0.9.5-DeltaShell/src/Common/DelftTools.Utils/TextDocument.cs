using System;
using System.ComponentModel;
using DelftTools.Utils.Aop.NotifyPropertyChange;
using DelftTools.Utils.Data;

namespace DelftTools.Utils
{
    [NotifyPropertyChange]
    public class TextDocument : Unique<long>, INameable, ICloneable
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