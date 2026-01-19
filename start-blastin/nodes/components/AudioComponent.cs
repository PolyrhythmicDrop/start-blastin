using DataStructures;
using Entities;
using Godot;
using Services;

namespace Components
{
    /// <summary>
    /// Component to control a player's audio. Uses the <see cref="AudioService"/> class to play specific sounds from the Player's current <see cref="SoundSet"/> resource.
    /// </summary>
    [GlobalClass]
    public partial class AudioComponent : Node
    {
        protected Node _parent;
        protected AudioService _service;

        [Export]
        public SoundSet Sounds { get; set; }

        public void Initialize(Node parent)
        {
            _parent = parent;
            _service = AudioService.Instance;
            Sounds?.Initialize(parent);
        }

        /// <summary>
        /// Plays the current firing sound.
        /// </summary>
        public virtual void PlayFireSound()
        {
            if (Sounds?.Fire != null)
            {
                _service.PlaySound(Sounds?.Fire);
            }
        }

        public virtual void PlayHitSound()
        {
            if (Sounds?.Hit != null)
            {
                _service.PlaySound(Sounds?.Hit);
            }
        }

        public virtual void PlayDestructionSound()
        {
            if (Sounds?.Destruction != null)
            {
                _service.PlaySound(Sounds?.Destruction);
            }
        }
    }
}
