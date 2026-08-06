using System;

namespace ActionAttribute
{
    internal static class ValueConstraintUtility
    {
        internal static double Snap(double value, double step, double origin)
        {
            if (step <= 0 || double.IsNaN(step) || double.IsInfinity(step))
                return value;
            double units = (value - origin) / step;
            return origin + Math.Round(units, MidpointRounding.AwayFromZero) * step;
        }

        internal static string Truncate(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
                return value;
            return value.Substring(0, Math.Max(0, maxLength));
        }
    }
}
