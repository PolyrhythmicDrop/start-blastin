using System;

namespace Events
{
    /// <summary>
    /// Event args that only supply a player ID. Generally used to verify that the correct player's action happened.
    /// </summary>
    public class PlayerIdEventArgs : EventArgs
    {
        private int _playerId;

        public int PlayerId => _playerId;

        public PlayerIdEventArgs(int id)
        {
            _playerId = id;
        }
    }
}
