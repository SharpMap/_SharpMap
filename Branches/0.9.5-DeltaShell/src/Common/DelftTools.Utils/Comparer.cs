using System;

namespace DelftTools.Utils
{
    // YAGNI: remove this methods, use when needed: "Math.Abs(a - b) < epsilon"
    public class Comparer
    {
        // AreEqual2sComplement method
        public static bool AlmostEqual2sComplement( float a, float b, int maxDeltaBits = 4)
        {
            int aInt = BitConverter.ToInt32( BitConverter.GetBytes( a ), 0 );
            if ( aInt <  0 )
                aInt = Int32.MinValue - aInt;  // Int32.MinValue = 0x80000000

            int bInt = BitConverter.ToInt32( BitConverter.GetBytes( b ), 0 );
            if ( bInt < 0 )
                bInt = Int32.MinValue - bInt;

            int intDiff = Math.Abs( aInt - bInt );
            return intDiff <= ( 1 << maxDeltaBits );
        }

        public static bool AlmostEqual2sComplement(double a, double b, int maxDeltaBits = 4 )
        {
            Int64 aInt = BitConverter.ToInt64(BitConverter.GetBytes(a), 0);
            if (aInt < 0)
                aInt = Int64.MinValue - aInt;

            Int64 bInt = BitConverter.ToInt64(BitConverter.GetBytes(b), 0);
            if (bInt < 0)
                bInt = Int64.MinValue - bInt;

            Int64 intDiff = Math.Abs(aInt - bInt);
            return intDiff <= (1 << maxDeltaBits);
        }

        /*
        public static bool AreReferencesOrValuesEqual(object source, object target)
        {
            if (source is ValueType || target is ValueType)
            {
                if(source == null || target == null)
                {
                    return source == target;
                }

                return source.Equals(target);
            }
            
            return source == target;
        }


        public static bool IsEqual(IComparable object1, IComparable object2)
        {
            return object1.CompareTo(object2) == 0;
        }

        /// <summary>
        /// Returns true if object 1 is 'bigger' then object 2. 2 > 1 returns true
        /// </summary>
        /// <param name="object1"></param>
        /// <param name="object2"></param>
        /// <returns></returns>
        public static bool IsBigger(IComparable object1,IComparable object2)
        {
            return object1.CompareTo(object2) == 1;
        }

        /// <summary>
        /// Determines whether item is between lower and upper. Excluding bounds eg. 1 is not between 1 and 3, but 3 is.
        /// </summary>
        /// <returns></returns>
        public static bool IsBetween(IComparable previous,IComparable item,IComparable nextV)
        {
            return IsBigger(item, previous) && IsBigger(nextV, item);
        }
     */ 
    }
}