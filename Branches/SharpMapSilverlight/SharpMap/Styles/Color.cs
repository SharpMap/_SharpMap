﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpMap.Styles
{
    public class Color
    {
        public static Color FromArgb(int a, int r, int g, int b)
        {
            return new Color { A = a, R = r, G = g, B = b };
        }

        public int A { get; set; }
        public int R { get; set; }
        public int G { get; set; }
        public int B { get; set; }

        public static Color Black { get { return new Color() { A = 255, R = 0, G = 0, B = 0 }; } }
        public static Color White { get { return new Color() { A = 255, R = 255, G = 255, B = 255 }; } }

        public static Color Red { get { return new Color() { A = 255, R = 255, G = 0, B = 0 }; } }
        public static Color Yellow { get { return new Color() { A = 255, R = 255, G = 255, B = 0 }; } }
        public static Color Green { get { return new Color() { A = 255, R = 0, G = 128, B = 0 }; } }
        public static Color Cyan { get { return new Color() { A = 255, R = 0, G = 255, B = 255 }; } }
        public static Color Blue { get { return new Color() { A = 255, R = 0, G = 0, B = 255 }; } }

        //do i need the rest?
        public static Color DarkGreen { get { return new Color() { A = 255, R = 0, G = 128, B = 0 }; } }
        public static Color DarkBlue { get { return new Color() { A = 255, R = 0, G = 0, B = 128 }; } }
        public static Color Orange { get { return new Color() { A = 255, R = 255, G = 128, B = 0 }; } }
        public static Color Violet { get { return new Color() { A = 238, G = 130, B = 238 }; } }
        public static Color Indigo { get { return new Color() { A = 255, R = 75, G = 0, B = 130 }; } }
        public static Color DarkCyan { get { return new Color() { A = 255, R = 0, G = 128, B = 128 }; } }
    }
}
