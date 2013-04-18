using System;

namespace DelftTools.Utils
{
    public interface ITimeDependent
    {
        DateTime Time { get; set; }
    }
}