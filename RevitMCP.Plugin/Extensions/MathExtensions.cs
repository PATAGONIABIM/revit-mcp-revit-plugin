using System;

namespace RevitMCP.Plugin.Extensions
{
    public static class MathExtensions
    {
        public const double MM_TO_FEET = 0.00328084;
        public const double FEET_TO_MM = 304.8;
        public const double INCH_TO_FEET = 1.0 / 12.0;
        public const double FEET_TO_INCH = 12.0;

        /// <summary>
        /// Rounds a double value to a specified number of decimal places.
        /// </summary>
        public static double RoundToDecimals(this double value, int decimals)
        {
            return Math.Round(value, decimals);
        }

        /// <summary>
        /// Checks if two double values are almost equal within a tolerance.
        /// </summary>
        public static bool IsAlmostEqual(this double target, double value, double tolerance = 1e-9)
        {
            return Math.Abs(target - value) < tolerance;
        }

        /// <summary>
        /// Converts Millimeters to Feet.
        /// </summary>
        public static double ToFeet(this double mm)
        {
            return mm * MM_TO_FEET;
        }
        
        /// <summary>
        /// Converts Meters to Feet.
        /// </summary>
        public static double MetersToFeet(this double meters)
        {
            return meters * 3.28084;
        }

        /// <summary>
        /// Converts Feet to Millimeters.
        /// </summary>
        public static double ToMillimeters(this double feet)
        {
            return feet * FEET_TO_MM;
        }

        /// <summary>
        /// Converts Inches to Feet.
        /// </summary>
        public static double InchesToFeet(this double inches)
        {
            return inches * INCH_TO_FEET;
        }

        /// <summary>
        /// Converts Feet to Inches.
        /// </summary>
        public static double FeetToInches(this double feet)
        {
            return feet * FEET_TO_INCH;
        }

        /// <summary>
        /// Clamps a value between a minimum and maximum.
        /// </summary>
        public static double Clamp(this double val, double min, double max)
        {
            if (val.CompareTo(min) < 0) return min;
            else if (val.CompareTo(max) > 0) return max;
            else return val;
        }
    }
}
