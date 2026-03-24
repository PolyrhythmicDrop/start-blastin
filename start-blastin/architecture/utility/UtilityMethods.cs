using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

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
        /// <param name="animSprite">The AnimatedSprite2D object to get the animation from.</param>
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
                bool playing = animSprite.IsPlaying();
                float playingSpeed = playing
                    ? MathF.Abs(animSprite.GetPlayingSpeed())
                    : animSprite.SpeedScale;

                for (int i = 0; i < frameCount; i++)
                {
                    totalDuration +=
                        sf.GetFrameDuration(animation, i)
                        / ((float)sf.GetAnimationSpeed(animation) * playingSpeed);
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

        /// <summary>
        /// Waits for the passed Tween to finish and returns a <see cref="Task"/> when complete.
        /// Pass a <paramref name="token"/> to allow the tween to be cancelled.
        /// </summary>
        /// <param name="tween">The <see cref="Tween"/> to wait for.</param>
        /// <param name="token">A cancellation token from a <see cref="CancellationTokenSource"/>. Pass something to this argument to allow the Tween to be cancelled.</param>
        /// <returns></returns>
        /// <remarks>Use this instead of a SignalAwaiter to convert the Tween Finished signal to a Task.</remarks>
        public static Task AwaitTweenFinished(Tween tween, CancellationToken token = default)
        {
            // If there's already a cancellation pending on the token, return a cancelled task using the token.
            if (token.IsCancellationRequested)
            {
                return Task.FromCanceled(token);
            }

            // Create a manual task to manage completion of the tween instead of relying on Godot's SignalAwaiter.
            // This task is "pending" until you manually complete or cancel it.
            TaskCompletionSource tcs = new();

            // Connect the tween's Finished signal to the task completion callback.
            // If the Tween finishes successfully without being cancelled/killed, the TaskCompletionSource's Task status is set to RanToCompletion.
            tween.Finished += () => tcs.TrySetResult();

            // Set up cancellation if the passed token can be cancelled.
            if (token.CanBeCanceled)
            {
                // Register the cancellation token to a callback.
                // The callback fires on cancel, and cancels the TCS task as well.
                CancellationTokenRegistration registered = token.Register(() =>
                {
                    // Kill the tween.
                    tween.Kill();
                    // Cancel the manual task using the token.
                    tcs.TrySetCanceled(token);
                });
            }

            // Return the completed or cancelled TCS.
            return tcs.Task;
        }

        /// <summary>
        /// Converts a Godot Signal to a <see cref="Task"/> so you can do Task-y things with it.
        /// Use this instead of a SignalAwaiter when you need the more robust Task functionality.
        /// Currently only accepts signals that do not pass arguments.
        /// </summary>
        /// <param name="source">The source of the signal.</param>
        /// <param name="signal">The signal to convert to a Task.</param>
        /// <param name="token">Optional cancellation token. Pass this if you want to be able to cancel the Task.</param>
        /// <returns>A <see cref="Task"/> that completes when the <paramref name="signal"/> is emitted.</returns>
        public static Task GodotSignalToTask(
            GodotObject source,
            StringName signal,
            CancellationToken token = default
        )
        {
            // If there's already a cancellation pending on the token, return a cancelled task using the token.
            if (token.IsCancellationRequested)
            {
                return Task.FromCanceled(token);
            }

            // Create a manual task to manage completion of the Task instead of relying on Godot's SignalAwaiter.
            // This task is "pending" until you manually complete or cancel it.
            TaskCompletionSource tcs = new();
            Callable resultCallable = Callable.From(tcs.TrySetResult);

            if (source.HasSignal(signal))
            {
                DebugLogger.LogMessage($"Signal {signal} found!");
                source.Connect(
                    signal,
                    Callable.From(() =>
                    {
                        DebugLogger.LogMessage($"Ready callback called on {source}!", true);
                        // Call TrySetResult().
                        tcs.TrySetResult();
                    }),
                    flags: 4
                );
            }

            // Connect the tween's Finished signal to the task completion callback.
            // If the Tween finishes successfully without being cancelled/killed, the TaskCompletionSource's Task status is set to RanToCompletion.
            // source.signalName += () => tcs.TrySetResult();

            // Set up cancellation if the passed token can be cancelled.
            if (token.CanBeCanceled)
            {
                // Register the cancellation token to a callback.
                // The callback fires on cancel, and cancels the TCS task as well.
                CancellationTokenRegistration registered = token.Register(() =>
                {
                    // Cancel the manual task using the token.
                    tcs.TrySetCanceled(token);
                });
            }

            // Return the completed or cancelled TCS.
            return tcs.Task;
        }
    }
}
