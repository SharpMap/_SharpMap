/*
 *  The attached / following is part of SharpMap.Presentation.AspNet
 *  SharpMap.Presentation.AspNet is free software © 2008 Newgrove Consultants Limited, 
 *  www.newgrove.com; you can redistribute it and/or modify it under the terms 
 *  of the current GNU Lesser General Public License (LGPL) as published by and 
 *  available from the Free Software Foundation, Inc., 
 *  59 Temple Place, Suite 330, Boston, MA 02111-1307 USA: http://fsf.org/    
 *  This program is distributed without any warranty; 
 *  without even the implied warranty of merchantability or fitness for purpose.  
 *  See the GNU Lesser General Public License for the full details. 
 *  
 *  Author: John Diss 2008
 * 
 */

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using SharpMap.Styles;

namespace SharpMap.Presentation.AspNet.Demo
{

    /// <summary>
    /// Silly class used to randomize symbology. 
    /// </summary>
    public class RandomStyle
    {
        readonly List<string> _symbols = new List<string>();

        public RandomStyle(string symbolsDir, string symbolSearchPattern)
        {

            if (Directory.Exists(symbolsDir))
                _symbols.AddRange(Directory.GetFiles(symbolsDir, symbolSearchPattern));
        }

        private static readonly Random random = new Random(DateTime.Now.Millisecond);

        /// <summary>
        /// returns  a Random VectorStyle with no symbols.
        /// 
        /// </summary>
        /// <returns></returns>
        public static VectorStyle RandomVectorStyleNoSymbols()
        {
            VectorStyle vs = new VectorStyle();

            vs.EnableOutline = random.Next(0, 2) == 1;

            vs.Fill = RandomBrush();
            vs.Line = RandomPen();
            vs.Outline = RandomPen();



            return vs;
        }
        /// <summary>
        /// returns a random VectorStyle with symbols.
        /// </summary>
        /// <returns></returns>
        public VectorStyle RandomVectorStyle()
        {
            VectorStyle vs = RandomStyle.RandomVectorStyleNoSymbols();
            vs.Symbol = RandomSymbol();
            return vs;
        }

        public Bitmap RandomSymbol()
        {
            if (_symbols.Count == 0)
                throw new InvalidOperationException();


            throw new NotImplementedException();
            //return new Symbol2D(new FileStream(_symbols[random.Next(0, _symbols.Count - 1)], FileMode.Open, FileAccess.Read, FileShare.Read), RandomSize());

        }

        private Size RandomSize()
        {
            return new Size(random.Next(10, 25), random.Next(10, 25));
        }

        public static Pen RandomPen()
        {
            return new Pen(RandomColor(), random.Next(1, 3));
        }

        public static Brush RandomBrush()
        {
            return new SolidBrush(RandomColor());
        }

        public static Color RandomColor()
        {
            return Color.FromArgb(random.Next(60, 255), random.Next(0, 255), random.Next(0, 255), random.Next(0, 255));
        }
    }
}
