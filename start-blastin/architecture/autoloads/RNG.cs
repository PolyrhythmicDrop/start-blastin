using System;
using System.Linq;
using System.Text;
using Godot;
using NanoidDotNet;
using Utility;

namespace Autoloads
{
    public static class RNG
    {
        private static string _seedString;
        private static Random _random;

        public static string Seed => _seedString;

        private static string GenerateRandomSeedString()
        {
            return Nanoid.Generate(size: 12);
        }

        private static void SetSeedString(string seed = null)
        {
            if (seed == null)
            {
                _seedString = GenerateRandomSeedString();
            }
            else
            {
                _seedString = seed;
            }
        }

        private static int StringToIntSeed(string str)
        {
            int sum = 0;
            int prevNum = 0;
            foreach (char c in str)
            {
                int intChar = c;
                DebugLogger.LogMessage($"char = {c} | prevNum = {prevNum} | intChar = {intChar}");
                intChar += (prevNum + 1) / 2;
                sum += intChar;
                prevNum = intChar;
            }
            DebugLogger.LogMessage($"original string = {str} | sum = {sum}");
            return sum;
        }

        public static void InitializeRNG(string seed = null)
        {
            SetSeedString(seed);
            DebugLogger.LogMessage($"Initializing RNG with seed {_seedString}...");
            int seedInt = StringToIntSeed(_seedString);
            _random = new(seedInt);
        }

        public static Random GetRandom()
        {
            return _random;
        }

        public static int GetRandomInt()
        {
            return _random.Next();
        }

        public static int GetRandomInt(int min = 0, int max = 1)
        {
            return _random.Next(min, max);
        }

        public static double GetRandomDouble(float min = 0, float max = 1)
        {
            return _random.NextDouble() * (min - max) + min;
        }

        public static double GetRandomDouble()
        {
            return _random.NextDouble();
        }
    }
}
