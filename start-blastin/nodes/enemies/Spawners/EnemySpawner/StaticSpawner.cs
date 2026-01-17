using System;
using System.Collections.Generic;
using Autoloads;
using Events;
using Godot;
using Utility;
using WaveManagement;

namespace Enemies.Spawners
{
    [GlobalClass]
    public partial class StaticSpawner : EnemySpawner
    {
        /// <summary>
        /// Sorted list of SpawnSteps that get triggered when their Timer key goes off.
        /// </summary>
        private Dictionary<Timer, List<SpawnStep>> _spawnPlan = new();

        private const int IMMEDIATE_SPAWN_ID = 9999;

        public SpawnStep[] SpawnSteps { get; set; }

        /// <summary>
        /// Builds the _spawnPlan Dictionary based on the passed steps and the amount of time the next wave takes.
        /// </summary>
        /// <param name="waveTime"></param>
        public void BuildSpawnPlan(double? waveTime = null)
        {
            // Get the time for the next wave
            double totalTime = waveTime ?? WaveManager.GetNextWaveTime();

            // Convert all the WaveTimeRatio variables into actual time.
            // Use a duple to maintain the connection between the WaveTimeRatio and the actual time.
            HashSet<(double waveRatio, double realTime)> timeSets = new();

            foreach (SpawnStep step in SpawnSteps)
            {
                // Convert the WaveTimeRatio into real time and add it to the hash set.
                // Since the HashSet can't have duplicates, this should avoid accidentally creating duplicates.
                double realTime = step.WaveTimeRatio * totalTime;
                timeSets.Add((step.WaveTimeRatio, realTime));
            }

            // Go through the created timeSets, create Timers from them, and sort the steps according to the timers.
            foreach (var (waveRatio, realTime) in timeSets)
            {
                // Create timers for each unique actual time.
                Timer timer = new()
                {
                    // Since we can't set the WaitTime to 0, set it to an arbitrary number if the enemy should spawn immediately.
                    WaitTime = realTime > 0 ? realTime : IMMEDIATE_SPAWN_ID,
                    Autostart = false,
                    OneShot = true,
                    ProcessMode = ProcessModeEnum.Pausable,
                };

                _spawnPlan[timer] = new();

                foreach (SpawnStep step in SpawnSteps)
                {
                    if (step.WaveTimeRatio == waveRatio)
                    {
                        _spawnPlan[timer].Add(step);
                    }
                }
            }

            // Set up the Timeout callbacks for each timer.
            foreach (KeyValuePair<Timer, List<SpawnStep>> kvp in _spawnPlan)
            {
                kvp.Key.Timeout += () => SpawnEnemy([.. kvp.Value]);
            }
        }

        public override void _Ready()
        {
            base._Ready();
            // Add all the Timers to the scene tree.
            foreach (Timer timer in _spawnPlan.Keys)
            {
                AddChild(timer);
            }

            ConnectSignals();
        }

        public void ConnectSignals()
        {
            EventBus.Instance.WaveStarted += OnWaveStarted;
            EventBus.Instance.WaveTimerEnded += OnWaveTimerEnded;
        }

        public void DisconnectSignals()
        {
            EventBus.Instance.WaveStarted -= OnWaveStarted;
            EventBus.Instance.WaveTimerEnded -= OnWaveTimerEnded;
        }

        protected override void OnWaveStarted(object sender, WaveStartedEventArgs args)
        {
            _waveTimerActive = true;
            _currentWave = args.Wave;
            ToggleSpawning(true);
        }

        protected override void OnWaveTimerEnded()
        {
            _waveTimerActive = false;
            ToggleSpawning(false);
        }

        public override void ToggleSpawning(bool spawn)
        {
            if (spawn)
            {
                StartSpawnTimer();
            }
            else
            {
                foreach (Timer timer in _spawnPlan.Keys)
                {
                    timer.Stop();
                }
            }
        }

        protected override void StartSpawnTimer()
        {
            foreach (Timer timer in _spawnPlan.Keys)
            {
                if (timer.WaitTime != IMMEDIATE_SPAWN_ID)
                {
                    DebugLogger.LogMessage($"Starting {timer.Name}...", true);
                    timer.Start();
                }
                // If the timer's wait time is 0, spawn all its steps immediately instead of starting the timer.
                else
                {
                    SpawnEnemy([.. _spawnPlan[timer]]);
                }
            }
        }

        public void SpawnEnemy(params SpawnStep[] steps)
        {
            // If the wave timer's not running for whatever reason, don't do anything.
            if (!_waveTimerActive)
            {
                return;
            }

            // Spawn enemies according to the current step.
            foreach (SpawnStep step in steps)
            {
                DebugLogger.LogMessage($"Spawning enemy!", true);

                for (int i = 0; i < step.Quantity; i++)
                {
                    // Duplicate the enemy resource for each new enemy.
                    EnemyResource resource = (EnemyResource)
                        step.EnemyType.DuplicateDeep(Resource.DeepDuplicateMode.All);

                    // Set the progress ratio to the step's progress ratio so the spawn point is in the correct location for spawning.
                    if (i == 0)
                    {
                        _pathFollow.ProgressRatio = step.SpawnPosition;
                    }
                    else
                    {
                        // If we're spawning more than one enemy on this step, offset each enemy's position by a small random value so they don't all spawn on top of each other.
                        float rand = (float)RNG.GetRandomDouble(-0.05f, 0.05f);
                        float newRatio = Math.Clamp(step.SpawnPosition + rand, 0, 1.0f);
                        _pathFollow.ProgressRatio = newRatio;
                    }

                    // Pass it to the base SpawnEnemy method
                    base.SpawnEnemy(resource);
                }
            }
        }

        public override void _ExitTree()
        {
            DisconnectSignals();
            base._ExitTree();
        }
    }
}
