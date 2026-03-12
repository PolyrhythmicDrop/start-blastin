using System;
using System.Linq;
using Entities;

namespace Events
{
    public class GameInitializedEventArgs : EventArgs
    {
        /// <summary>
        /// The number of players in the game.
        /// </summary>
        public int PlayerCount { get; }

        /// <summary>
        /// The Player objects for this game.
        /// </summary>
        public Player[] Players { get; }

        /// <summary>
        /// The starting wave number for this game.
        /// </summary>
        public int StartingWave { get; }

        /// <summary>
        /// The amount of time for the first wave.
        /// </summary>
        public double WaveTime { get; }

        // Consider adding save game data here once you implement that.

        public GameInitializedEventArgs(Player[] players, int startingWave, double waveTime)
        {
            Players = players;
            PlayerCount = players.Count();
            StartingWave = startingWave;
            WaveTime = waveTime;
        }
    }
}
