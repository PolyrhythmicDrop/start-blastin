using System;
using System.Text.RegularExpressions;
using Godot;

namespace Utility
{
    public static class UtilityMethods
    {
        public static float ConvertNegativeRotationRads(float radians)
        {
            return (radians % Mathf.Tau + Mathf.Tau) % Mathf.Tau;
        }

        public static float ConvertNegativeRotationDegrees(float degrees)
        {
            return (degrees % 360 + 360) % 360;
        }

        public static string SplitCamelCase(string str)
        {
            return Regex.Replace(
                Regex.Replace(str, @"(\P{Ll})(\P{Ll}\p{Ll})", "$1 $2"),
                @"(\p{Ll})(\P{Ll})",
                "$1 $2"
            );
        }
    }
}
