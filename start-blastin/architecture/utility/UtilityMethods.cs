using System;
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
    }
}
