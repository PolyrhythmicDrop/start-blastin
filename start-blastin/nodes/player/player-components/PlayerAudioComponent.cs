using System;
using Components;
using DataStructures;
using Entities;
using Godot;

namespace PlayerComponents
{
    [GlobalClass]
    public partial class PlayerAudioComponent : AudioComponent
    {
        // public new PlayerSoundSet Sounds
        // {
        //     get => this.GetSounds();
        //     set;
        // }

        /// <summary>
        /// Sets the fire sound to a specific audio stream.
        /// </summary>
        /// <param name="stream">The path of the stream to set the fire sound to.</param>
        /// <remarks>
        /// Called by the player's WeaponComponent when a new weapon plugin is equipped.
        /// </remarks>
        public void SetFireSound(AudioData audioData)
        {
            if (_parent is Player player)
            {
                audioData.Source = player;
            }
            Sounds.Fire = audioData;
        }

        /// <summary>
        /// Plays the current phase sound.
        /// </summary>
        public void PlayPhaseStartSound()
        {
            // _service.PlaySound(Sounds?.PhaseStart, _player, 1);
            if (Sounds is PlayerSoundSet playerSounds)
            {
                _service.PlaySound(playerSounds?.PhaseStart);
            }
        }

        public void PlayPhaseReadySound()
        {
            // _service.PlaySound(Sounds?.PhaseReady, _player, 1, volume: -6);
            if (Sounds is PlayerSoundSet playerSounds)
            {
                _service.PlaySound(playerSounds?.PhaseReady);
            }
        }
    }
}
