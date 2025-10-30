using System;
using Events;
using Godot;
using Items;

namespace Autoloads
{
    public partial class EventBus : Node
    {
        public static EventBus Instance { get; private set; }

        #region Waves

        public event EventHandler<WaveStartedEventArgs> WaveStarted;

        public event EventHandler<WaveTimeLeftEventArgs> WaveTimeLeft;

        private readonly WaveTimeLeftEventArgs _waveTimeLeftArgs = new();

        public event Action WaveTimerEnded;

        public event Action WaveComplete;

        public event Action StartWaveButtonPressed;

        public event Action SpawnersReady;

        #endregion

        #region Shop and Items
        [Signal]
        public delegate void ShopOpenedEventHandler();

        [Signal]
        public delegate void ShopClosedEventHandler();

        [Signal]
        public delegate void ShopItemBoughtEventHandler(Item item);

        #endregion

        #region Player Status

        [Signal]
        public delegate void PlayerMaxHealthChangedEventHandler(int playerId, float maxHealth);

        [Signal]
        public delegate void PlayerCurrentHealthChangedEventHandler(
            int playerId,
            float currentHealth
        );

        [Signal]
        public delegate void PlayerPhaseTimeLeftEventHandler(int playerId, float timeLeft);

        [Signal]
        public delegate void PlayerPhaseTotalCooldownChangedEventHandler(
            int playerId,
            float cooldownTime
        );

        [Signal]
        public delegate void PlayerFluxChangeEventHandler(int playerId, int flux);

        [Signal]
        public delegate void PlayerBytesChangeEventHandler(int playerId, int bytes);

        #endregion

        #region Enemies

        /// <summary>
        /// Enemy was killed by player.
        ///</summary>
        public event EventHandler<EnemyKilledEventArgs> EnemyKilled;

        #endregion


        public override void _Ready()
        {
            Instance = this;
        }

        public void RaiseWaveStarted(int wave)
        {
            WaveStartedEventArgs args = new(wave);
            WaveStarted?.Invoke(this, args);
        }

        public void RaiseWaveTimeLeft(double timeLeft, double totalTime)
        {
            _waveTimeLeftArgs.TimeLeft = timeLeft;
            _waveTimeLeftArgs.TotalTime = totalTime;
            WaveTimeLeft?.Invoke(this, _waveTimeLeftArgs);
        }

        public void RaiseWaveTimerEnded()
        {
            WaveTimerEnded?.Invoke();
        }

        public void RaiseWaveComplete()
        {
            WaveComplete?.Invoke();
        }

        public void RaiseStartWaveButtonPressed()
        {
            StartWaveButtonPressed?.Invoke();
        }

        public void RaiseSpawnersReady()
        {
            SpawnersReady?.Invoke();
        }

        public void RaiseEnemyKilled(EnemyKilledEventArgs args)
        {
            EnemyKilled?.Invoke(this, args);
        }
    }
}
