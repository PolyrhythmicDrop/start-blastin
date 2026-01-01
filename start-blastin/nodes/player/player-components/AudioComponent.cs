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
        public SoundSet Sounds { get; set; }

        public void Initialize(Player player)
        {
            _player = player;
            _service = AudioService.Instance;
        }

        /// <summary>
        /// Plays the current firing sound.
        /// </summary>
        public void PlayFireSound()
        {
            _service.PlaySound(Sounds?.Fire, _player, 1);
        }
    }
}
