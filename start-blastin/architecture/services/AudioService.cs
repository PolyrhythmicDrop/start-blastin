using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Enemies;
using Entities;
using FileIO;
using Godot;
using Utility;

namespace Services
{
    public partial class AudioService : Node
    {
        public record AudioPlayerData
        {
            public required AudioStreamPlayer2D Player;
            public required string Bus;
            public required Node Parent;
        }

        private Dictionary<string, AudioStreamRandomizer> _streams = new();

        private Dictionary<string, List<AudioPlayerData>> _audioPlayers = new();

        private const string STREAM_PATH = "res://resources/audio-streams/";

        public static AudioService Instance { get; private set; }

        /// <summary>
        /// Read-only dictionary of AudioStreamRandomizers, arranged according to sound name.
        /// </summary>
        public IReadOnlyDictionary<string, AudioStreamRandomizer> Streams => _streams;

        /// <summary>
        /// Read-only dictionary of AudioPlayer nodes and their parents, arranged according to sound name.
        /// </summary>
        public IReadOnlyDictionary<string, IReadOnlyList<AudioPlayerData>> AudioPlayers =>
            (IReadOnlyDictionary<string, IReadOnlyList<AudioPlayerData>>)_audioPlayers;

        public override void _Ready()
        {
            Instance = this;
            LoadRandomizersToDictionary();
        }

        /// <summary>
        /// Gets all the sound names from the _streams Dictionary.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> GetSoundStrings()
        {
            foreach (string str in _streams.Keys)
            {
                yield return str;
            }
        }

        /// <summary>
        /// Matches a passed UID to a sound name string in the dictionary.
        /// </summary>
        /// <param name="uid">The UID to match.</param>
        /// <returns>The sound name string, if found. If a matching string was not found, throws an exception and returns null.</returns>
        public string MatchUidToSoundString(string uid)
        {
            // Convert the UID to a path, trim the .tres suffix, split according to the '/' character, and get the penultimate entry in the new array.
            string pathStr = ResourceUid.UidToPath(uid).TrimSuffix(".tres").Split('/')[^1];
            try
            {
                foreach (string streamStr in GetSoundStrings())
                {
                    if (pathStr == streamStr)
                    {
                        return streamStr;
                    }
                }
                // If we couldn't find a matching string in the _streams list, throw
                throw new InvalidCastException(
                    $"Could not match the passed UID ({uid}) to a stream string! Make sure the UID belongs to an {typeof(AudioStreamRandomizer)} in the {STREAM_PATH} directory."
                );
            }
            catch (Exception e)
            {
                DebugLogger.LogMessage(e.Message, true, true);
                return null;
            }
        }

        // Loads all existing AudioStreamRandomizers into the dictionary with the appropriate key.
        private void LoadRandomizersToDictionary()
        {
            // Load all the existing AudioStreamRandomizers into a list.
            List<AudioStreamRandomizer> randos = [];
            PoolLoader.LoadResourcePool(randos, STREAM_PATH, true);

            try
            {
                foreach (AudioStreamRandomizer rando in randos)
                {
                    // Get the name from the path and file name.
                    string name = rando.ResourcePath.TrimSuffix(".tres").Split('/')[^1];

                    // Add it to the dictionary.
                    bool added = _streams.TryAdd(name, rando);
                    if (!added)
                    {
                        throw new InvalidOperationException(
                            $"Could not add {name} to {nameof(_streams)}! The key probably already exists in the dictionary."
                        );
                    }
                }
            }
            catch (Exception e)
            {
                DebugLogger.LogMessage(e.Message, true, true);
            }
        }

        /// <summary>
        /// Adds one or more AudioStreams to the <see cref="Streams"/> dictionary by name.
        /// </summary>
        /// <param name="name">The name of the sound. If an entry by this name already exists in dictionary, adds the new AudioStream(s) to the key.</param>
        /// <param name="weight">Weight of the new streams in the AudioStreamRandomizer.</param>
        /// <param name="streams">The AudioStreams to add to the selected AudioStreamRandomizer pool.</param>
        public void AddStreamToRandomizer(
            string name,
            float weight = 1,
            params AudioStream[] streams
        )
        {
            // Attempt to find the sound name in the current dictionary.
            bool exists = _streams.TryGetValue(name, out AudioStreamRandomizer rando);

            // If the sound already exists, add the new stream(s) to the existing randomizer pool for that sound name.
            if (exists)
            {
                foreach (AudioStream stream in streams)
                {
                    rando.AddStream(-1, stream, weight);
                }
            }
            // Otherwise, create a new entry in the streams dictionary and add the new streams to it.
            else
            {
                _streams[name] = new AudioStreamRandomizer();

                foreach (AudioStream stream in streams)
                {
                    _streams[name].AddStream(-1, stream, weight);
                }
            }
        }

        /// <summary>
        /// Creates a new AudioStreamPlayer2D node using the passed values.
        /// Adds the new set of AudioPlayerData to the Dictionary of existing players.
        /// </summary>
        /// <param name="soundName">The name of the audio stream that the AudioStreamPlayer2D can play.</param>
        /// <param name="bus">The bus the AudioStreamPlayer outputs sound to.</param>
        /// <param name="parent">The parent node of the AudioStreamPlayer. If null, the parent node is the <see cref="AudioService"/> node.</param>
        /// <param name="globalPosition">The GlobalPosition of the AudioStreamPlayer. If null, uses the center of the viewport instead.</param>
        /// <param name="maxPolyphony">The maximum number of voices for the AudioStreamPlayer.</param>
        /// <returns>The new AudioStreamPlayer2D node, or null if an error occurred.</returns>
        private AudioStreamPlayer2D AddNewAudioStreamPlayer(
            string soundName,
            string bus = "Master",
            Node parent = null,
            int maxPolyphony = 5
        )
        {
            try
            {
                // Try to get the correct audio stream based on the passed sound name.
                bool streamFound = _streams.TryGetValue(
                    soundName,
                    out AudioStreamRandomizer stream
                );

                if (!streamFound)
                {
                    throw new ArgumentException(
                        $"Could not find the passed {nameof(soundName)} ({soundName}) in the _streams dictionary!",
                        paramName: soundName
                    );
                }

                // Create a new AudioStreamPlayer using the passed values.
                AudioStreamPlayer2D player = new()
                {
                    Bus = bus,
                    MaxPolyphony = maxPolyphony,
                    Stream = stream,
                };

                // Create a new set of AudioPlayerData
                AudioPlayerData data = new()
                {
                    Player = player,
                    Bus = bus,
                    Parent = parent ?? this,
                };

                // Set the name of the AudioStreamPlayer
                player.Name = $"{data.Parent.Name}-{soundName}";

                // Add the player as a child of its parent.
                data.Parent?.AddChild(player);

                // If the audioPlayers dictionary already contains the sound, add the new audio player data to the existing list.
                if (_audioPlayers.ContainsKey(soundName))
                {
                    _audioPlayers[soundName].Add(data);
                }
                // Otherwise, add a new entry to the audioPlayers dictionary with the data.
                else
                {
                    _audioPlayers[soundName] = [data];
                }

                // Set up a callback so the audio player is removed when its parent exits the tree.
                data.Parent.TreeExiting += () => RemoveAudioStreamPlayer(player);

                return player;
            }
            catch (Exception e)
            {
                DebugLogger.LogMessage(e.Message, true, true);
                return null;
            }
        }

        /// <summary>
        /// Finds an <see cref="AudioStreamPlayer2D"/> in the _audioPlayers list and attempts to remove it.
        /// </summary>
        /// <param name="audioPlayer">The audio player to remove.</param>
        public void RemoveAudioStreamPlayer(AudioStreamPlayer2D audioPlayer)
        {
            AudioPlayerData dataToRemove = null;
            string soundName = null;
            bool playerFound = false;

            // Attempt to find the passed player in the list.
            foreach (KeyValuePair<string, List<AudioPlayerData>> kvp in _audioPlayers)
            {
                foreach (AudioPlayerData data in kvp.Value)
                {
                    if (data.Player.Equals(audioPlayer))
                    {
                        playerFound = true;
                        dataToRemove = data;
                        soundName = kvp.Key;
                        data.Player = null;
                        break;
                    }
                }
                if (playerFound)
                {
                    break;
                }
            }

            if (dataToRemove != null && soundName != null)
            {
                _audioPlayers[soundName].Remove(dataToRemove);
                audioPlayer.QueueFree();
            }
        }

        /// <summary>
        /// Basic wrapper method for playing a sound. Pass a source to enable automatic bus detection.
        /// </summary>
        /// <param name="soundName">The name of the sound to play.</param>
        /// <param name="source">The source of the sound. Determines which bus plays the sound and the position of the sound.</param>
        public void PlaySound(string soundName, Node source = null, int maxPolyphony = 5)
        {
            if (string.IsNullOrEmpty(soundName))
            {
                return;
            }

            // Figure out the correct bus from the source
            string bus = source switch
            {
                EnemyNode => "Enemies",
                Player => "Player",
                Control => "UI",
                _ => "Master",
            };

            PlaySound(soundName, bus, source);
        }

        /// <summary>
        /// Plays a sound with all the details explicitly specified. Use this directly if you want to override default sound playing behavior.
        /// </summary>
        /// <param name="soundName">The name of the sound to play. This must be an existing entry in the sound list.</param>
        public void PlaySound(
            string soundName,
            string bus = "Master",
            Node parent = null,
            int maxPolyphony = 5
        )
        {
            try
            {
                // If the passed sound name is a UID, attempt to match it to an existing sound string.
                if (soundName.Contains("uid://"))
                {
                    soundName = MatchUidToSoundString(soundName);
                }

                if (soundName == null || !_streams.ContainsKey(soundName))
                {
                    throw new ArgumentException(
                        $"Could not find {soundName} in the list of audio streams! Make sure it exists and that you didn't typo somewhere.",
                        paramName: soundName
                    );
                }

                // Attempt to find any players that already have the sound loaded.
                bool playersFound = _audioPlayers.TryGetValue(
                    soundName,
                    out List<AudioPlayerData> playerDataList
                );

                // If we found an existing set of players, find one in the list that has the correct bus.
                if (playersFound)
                {
                    // Set up the predicate in order of importance: parent, position, bus.
                    Predicate<AudioPlayerData> predicate;
                    if (parent != null)
                    {
                        predicate = data =>
                        {
                            return data.Parent == parent;
                        };
                    }
                    else
                    {
                        predicate = data =>
                        {
                            return data.Bus == bus;
                        };
                    }

                    AudioPlayerData playerData = playerDataList.Find(predicate);
                    // If we found a set of data with the correct bus, play the sound and return. We're done!
                    if (playerData != null)
                    {
                        playerData.Player.Play();
                        return;
                    }
                }

                // If we didn't find an existing set of players for this sound or if we couldn't find a player with the correct bus,
                // create a new player, add it to the list, and play its sound.
                AudioStreamPlayer2D newPlayer = AddNewAudioStreamPlayer(
                    soundName,
                    bus,
                    parent,
                    maxPolyphony
                );
                newPlayer?.Play();
            }
            catch (Exception e)
            {
                DebugLogger.LogMessage(e.Message, true, true);
            }
        }

        public void PlaySoundFromData(string soundName, AudioPlayerData data, int maxPolyphony = 5)
        {
            if (string.IsNullOrEmpty(soundName))
            {
                return;
            }

            PlaySound(soundName, data.Bus, data.Parent, maxPolyphony);
        }
    }
}
