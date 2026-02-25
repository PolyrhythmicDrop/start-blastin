using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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

        /// <summary>
        /// Converts a unit Curve resource to a Curve2D object for pathing.
        /// Only works for simple, linear curves.
        /// </summary>
        /// <param name="curve">The Curve to convert.</param>
        /// <param name="targetLength">The length of the curve</param>
        /// <param name="sampleCount">The number of points to sample on the curve.</param>
        /// <returns></returns>
        public static Curve2D ConvertCurveToCurve2D(
            Curve curve,
            float targetLength = 1080,
            int sampleCount = 20
        )
        {
            Curve2D curve2D = new Curve2D();

            for (int i = 0; i <= sampleCount; i++)
            {
                // Get the X position along the unit curve for this iteration.
                float x = i / (float)sampleCount;

                // Get the Y value from the curve at the X position (the offset).
                float y = curve.Sample(x);

                // Convert the new x and y values to a Vector 2 point in space along the specified length.
                Vector2 point = new Vector2(x * targetLength, y * targetLength);

                // Add the point to the new Curve2D.
                curve2D.AddPoint(point);
            }

            return curve2D;
        }

        public static Curve2D ScaleCurve2DToLength(Curve2D originalCurve, float targetLength)
        {
            if (originalCurve == null || originalCurve.PointCount == 0)
            {
                return originalCurve;
            }

            // Get the length of the original curve.
            float originalLength = originalCurve.GetBakedLength();
            if (originalLength == 0)
            {
                return originalCurve;
            }

            // Get the scaling factor.
            float scaleFactor = targetLength / originalLength;

            Curve2D scaledCurve = new Curve2D();

            // Scale each point along the original curve.
            for (int i = 0; i < originalCurve.PointCount; i++)
            {
                Vector2 position = originalCurve.GetPointPosition(i) * scaleFactor;
                Vector2 inHandle = originalCurve.GetPointIn(i) * scaleFactor;
                Vector2 outHandle = originalCurve.GetPointOut(i) * scaleFactor;

                scaledCurve.AddPoint(position, inHandle, outHandle);
            }

            return scaledCurve;
        }

        /// <summary>
        /// Converts a Godot <see cref="SignalAwaiter"/> object (commonly returned by the <see cref="GodotObject.ToSignal"/> method) into a C# Task.
        /// </summary>
        /// <param name="signalAwaiter"></param>
        /// <returns></returns>
        public static Task SignalAwaiterToTask(SignalAwaiter signalAwaiter)
        {
            var task = Task.Run(async () => await signalAwaiter);
            return task;
        }

        /// <summary>
        /// Recursively gets all children of a specified parent node, including nested nodes.
        /// </summary>
        /// <param name="parent">The node to get all children of.</param>
        /// <returns>A List of child nodes.</returns>
        public static List<Node> GetAllChildren(Node parent)
        {
            List<Node> children = [];

            foreach (Node node in parent.GetChildren())
            {
                if (node.GetChildCount() > 0)
                {
                    children.Add(node);
                    children.AddRange(GetAllChildren(node));
                }
                else
                {
                    children.Add(node);
                }
            }

            return children;
        }

        /// <summary>
        /// Gets the duration of a specific animation of an AnimatedSprite2D.
        /// </summary>
        /// <param name="animSprite"></param>
        /// <param name="animation">The name of the animation.</param>
        /// <returns></returns>
        public static float? GetAnimationDuration(
            AnimatedSprite2D animSprite,
            string animation = "default"
        )
        {
            try
            {
                if (animSprite.SpriteFrames == null)
                {
                    throw new ArgumentException(
                        $"{animSprite.Name} has no SpriteFrames! Cannot get the length of an animation that does not exist.",
                        paramName: nameof(animSprite)
                    );
                }

                SpriteFrames sf = animSprite.SpriteFrames;

                if (!sf.HasAnimation(animation))
                {
                    throw new ArgumentException(
                        $"{animation} is not an valid animation name for {animSprite.Name}!",
                        paramName: nameof(animation)
                    );
                }

                int frameCount = animSprite.SpriteFrames.GetFrameCount(animation);
                float totalDuration = 0;

                for (int i = 0; i < frameCount; i++)
                {
                    totalDuration +=
                        sf.GetFrameDuration(animation, i)
                        / (
                            (float)sf.GetAnimationSpeed(animation)
                            * MathF.Abs(animSprite.GetPlayingSpeed())
                        );
                }

                return totalDuration;
            }
            catch (Exception e)
            {
                DebugLogger.LogMessage(e.Message, true, true);
                return null;
            }
        }

        /// <summary>
        /// Gets the progress ratio of a point near or on a Curve2D.
        /// </summary>
        /// <param name="curve">The Curve2D.</param>
        /// <param name="position">The point (in local coordinates) to calculate the progress ratio from.</param>
        /// <returns></returns>
        public static float GetCurveProgressRatio(Curve2D curve, Vector2 position)
        {
            float offset = curve.GetClosestOffset(position);
            float length = curve.GetBakedLength();
            return offset / length;
        }
    }
}
