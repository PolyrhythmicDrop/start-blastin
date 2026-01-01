using Entities;
using Godot;
using Interfaces;

namespace PlayerComponents
{
    [GlobalClass]
    public partial class PlayerStateComponent : Node, IPlayerComponent
    {
        private Player _player;

        public bool Phasing = false;
        public bool PhaseReady = true;
        public bool Dying = false;
        public bool DeflectActive = false;

        public void Initialize(Player player)
        {
            _player = player;
        }

        /// <summary>
        /// Checks if the player is able to phase.
        /// </summary>
        /// <returns>True if phase is not on cooldown, the player is not currently phasing, and the player is not dying or dead.</returns>
        public bool CanPhase()
        {
            return !Phasing && !Dying && PhaseReady;
        }
    }
}
