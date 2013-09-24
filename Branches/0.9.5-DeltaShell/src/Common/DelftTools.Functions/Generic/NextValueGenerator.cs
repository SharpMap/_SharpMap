using System;

namespace DelftTools.Functions.Generic
{
    public abstract class NextValueGenerator<T>
    {
        public abstract T GetNextValue();
    }
    
    public class FuncNextValueGenerator<T>:NextValueGenerator<T>
    {
        private readonly Func<T> nextValueFunc;

        public FuncNextValueGenerator(Func<T> nextValueFunc)
        {
            this.nextValueFunc = nextValueFunc;
        }
        public override T GetNextValue()
        {
            return nextValueFunc();
        }
    }
}