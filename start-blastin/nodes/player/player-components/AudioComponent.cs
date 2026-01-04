using DataStructures;
using Entities;
using Godot;
using Services;

namespace PlayerComponents
{
    /// <summary>
    /// Component to control a player's audio. Uses the <see cref="AudioService"/> class to play specific sounds from the Player's current <see cref="SoundSet"/> resource.
    /// </summary>
    [GlobalClass]
    public partial class AudioComponent : Node
    {
        private Player _player;
        private AudioService _service;

        [Export]
        public PlayerSoundSet Sounds { get; set; }

        public void Initialize(Player player)
        {
            _player = player;
            _service = AudioService.Instance;
        }

        /// <summary>
        /// Sets the fire sound to a specific audio stream.
        /// </summary>
        /// <param name="stream">The path of the stream to set the fire sound to.</param>
        /// <remarks>
        /// Called by the player's WeaponComponent when a new weapon plugin is equipped.
        /// </remarks>
        public void SetFireSound(string stream)
        {
            Sounds.Fire = stream;
        }

        /// <summary>
        /// Plays the current firing sound.
        /// </summary>
        public void PlayFireSound()
        {
            _service.PlaySound(Sounds?.Fire, _player, 1, volume: -6);
        }

        /// <summary>
        /// Plays the current phase sound.
        /// </summary>
        public void PlayPhaseStartSound()
        {
            _service.PlaySound(Sounds?.PhaseStart, _player, 1);
        }

        public void PlayPhaseReadySound()
        {
            _service.PlaySound(Sounds?.PhaseReady, _player, 1, volume: -6);
        }
    }
}
