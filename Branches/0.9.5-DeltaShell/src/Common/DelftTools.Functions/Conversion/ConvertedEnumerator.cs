using System;
using System.Collections;
using System.Collections.Generic;

namespace DelftTools.Functions.Conversion
{
    public class ConvertedEnumerator<TTarget, TSource> : IEnumerator<TTarget>
    {
        private Func<TSource, TTarget> toTarget;
        private IEnumerator<TSource> sourceEnumerator;
        public ConvertedEnumerator(IEnumerator<TSource> sourceEnumerator, Func<TSource, TTarget> toTarget)
        {
            this.sourceEnumerator = sourceEnumerator;
            this.toTarget = toTarget;
        }

        public void Dispose()
        {
            sourceEnumerator.Dispose();
        }

        public bool MoveNext()
        {
            return sourceEnumerator.MoveNext();
        }

        public void Reset()
        {
            sourceEnumerator.Reset();
        }

        public TTarget Current
        {
            get
            {
                return toTarget(sourceEnumerator.Current);
            }
        }

        object IEnumerator.Current
        {
            get { return Current; }
        }
    }
}