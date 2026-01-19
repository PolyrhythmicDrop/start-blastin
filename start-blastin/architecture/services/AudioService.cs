using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DataStructures;
using Enemies;
using Entities;
using Godot;
using Utility;

namespace Services
{
    public partial class AudioService : Node
    {
        public record AudioPlayerData
        {
            public required AudioStreamPlayer2D AudioPlayer;
            public required string Bus;
            public required Node Parent;
        }

        private Dictionary<string, List<AudioPlayerData>> _audioPlayers = new();

        private Dictionary<
            AudioStreamPlayer2D,
            (string audioId, AudioPlayerData data)
        > _audioPlayerLookup = new();

        private HashSet<string> _busNames = new();

        public static AudioService Instance { get; private set; }

        public override void _Ready()
        {
            Instance = this;
            // Cache bus names.
            for (int i = 0; i < AudioServer.BusCount; i++)
            {
                _busNames.Add(AudioServer.GetBusName(i));
            }
        }

        /// <summary>
        /// Finds an <see cref="AudioStreamPlayer2D"/> in the _audioPlayers list and attempts to remove it.
        /// Also clears the AudioID entry from the _audioStreams cache if no more players have the stream.
        /// </summary>
        /// <param name="audioPlayer">The audio player to remove.</param>
        public async Task RemoveAudioStreamPlayer(AudioStreamPlayer2D audioPlayer)
        {
            if (!_audioPlayerLookup.TryGetValue(audioPlayer, out var lookup))
            {
                DebugLogger.LogMessage(
                    $"Could not find {audioPlayer.Name} in reverse lookup dictionary for removal! Did you forget to add it?",
                    true,
                    true
                );
                return;
            }

            // Remove from both player dictionaries
            _audioPlayers[lookup.audioId].Remove(lookup.data);
            _audioPlayerLookup.Remove(audioPlayer);

            // Free the audio player immediately if it's not playing.
            if (!audioPlayer.Playing)
            {
                audioPlayer.QueueFree();
            }
            // Otherwise, wait until it's done playing audio, then free it.
            else
            {
                await ToSignal(audioPlayer, AudioStreamPlayer2D.SignalName.Finished);
                audioPlayer.QueueFree();
            }

            // Remove the stream from the stream cache and player cache if there are no more players to play the stream.
            if (_audioPlayers[lookup.audioId].Count <= 0)
            {
                _audioPlayers.Remove(lookup.audioId);
            }
        }

        private bool CheckBusExists(string bus)
        {
            return _busNames.Contains(bus);
        }

        /// <summary>
        /// Attempts to find and return an AudioPlayerData object containing the matching sound.
        /// </summary>
        /// <param name="sound">The sound to match.</param>
        /// <returns></returns>
        private AudioPlayerData TryGetMatchingAudioPlayerData(AudioData audioData, string bus)
        {
            // Attempt to find any players that already have the sound loaded.
            bool playersFound = _audioPlayers.TryGetValue(
                audioData.AudioID,
                out List<AudioPlayerData> playerDataList
            );

            // If we found an existing set of players, find one in the list that matches our data closest.
            if (!playersFound)
            {
                return null;
            }
            else
            {
                if (audioData.Source != null)
                {
                    foreach (AudioPlayerData data in playerDataList)
                    {
                        if (data.Parent.Equals(audioData.Source))
                        {
                            return data;
                        }
                    }
                }

                // Search for the bus if you can't find a matching parent or if Source is null.
                foreach (AudioPlayerData data in playerDataList)
                {
                    if (data.Bus == bus)
                    {
                        return data;
                    }
                }

                return null;
            }
        }

        private AudioStreamPlayer2D AddNewAudioStreamPlayer(
            AudioStream stream,
            AudioData audioData,
            string bus
        )
        {
            try
            {
                // Create a new AudioStreamPlayer using the passed values.
                AudioStreamPlayer2D audioPlayer = new()
                {
                    Bus = bus,
                    MaxPolyphony = audioData.MaxPolyphony,
                    Stream = stream,
                    Attenuation = audioData.Attenuation,
                    VolumeDb = audioData.Volume,
                };

                // Figure out the source node
                Node parent;
                if (audioData.Source != null && audioData.Positional == true)
                {
                    parent = audioData.Source;
                }
                else
                {
                    parent = this;
                }

                // Create a new set of AudioPlayerData
                AudioPlayerData playerData = new()
                {
                    AudioPlayer = audioPlayer,
                    Bus = bus,
                    Parent = parent,
                };

                // Set the name of the AudioStreamPlayer
                audioPlayer.Name = $"{playerData.Parent.Name}-{audioData.AudioID}";

                // Add the player as a child of its parent.
                playerData.Parent?.AddChild(audioPlayer);

                // Add the player to the reverse lookup dictionary.
                _audioPlayerLookup[audioPlayer] = (audioData.AudioID, playerData);

                // If the audioPlayers dictionary already contains the sound, add the new audio player data to the existing list.
                if (_audioPlayers.ContainsKey(audioData.AudioID))
                {
                    _audioPlayers[audioData.AudioID].Add(playerData);
                }
                // Otherwise, add a new entry to the audioPlayers dictionary with the data.
                else
                {
                    _audioPlayers[audioData.AudioID] = [playerData];
                }

                // Set up a callback so the audio player is removed when its parent exits the tree.
                playerData.Parent.TreeExited += async () =>
                {
                    await RemoveAudioStreamPlayer(audioPlayer);
                };

                return audioPlayer;
            }
            catch (Exception e)
            {
                DebugLogger.LogMessage(e.Message, true, true);
                return null;
            }
        }

        /// <summary>
        /// Plays a sound from <see cref="AudioData"/>.
        /// </summary>
        /// <param name="data">Data container for the sound to play and its properties.</param>
        public void PlaySound(AudioData data)
        {
            // Check if there's an audio stream to load.
            if (string.IsNullOrEmpty(data.Sound))
            {
                DebugLogger.LogMessage(
                    $"Passed {nameof(data)} {data} contains no sound to play!",
                    true,
                    true
                );
                return;
            }

            // Figure out the correct bus from the source
            string bus = data.Source switch
            {
                EnemyNode => "Enemies",
                Player => "Player",
                Control => "UI",
                _ => "Master",
            };

            // If the bus doesn't match an existing bus in the layout, make a new bus with the selected name.
            if (!CheckBusExists(bus))
            {
                AudioServer.AddBus(-1);
                AudioServer.SetBusName(-1, bus);
                _busNames.Add(bus);
            }

            // Retrieve the audio stream from the cache or generate a new audio stream based on the passed data.
            AudioStream stream = data.GenerateAudioStream();

            // Attempt to get a matching audio player from the audio players list.
            AudioPlayerData playerData = TryGetMatchingAudioPlayerData(data, bus);

            if (playerData == null)
            {
                // Create a new audio player from the stream and add it to the player cache.
                AudioStreamPlayer2D player2D = AddNewAudioStreamPlayer(stream, data, bus);
                player2D?.Play();
            }
            else
            {
                // Play the sound from the found player
                playerData.AudioPlayer?.Play();
            }
        }
    }
}
