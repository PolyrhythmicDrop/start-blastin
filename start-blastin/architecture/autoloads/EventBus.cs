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

        public event Action ShopOpened;

        public event Action ShopClosed;

        public event EventHandler<ItemBoughtEventArgs> ItemBought;

        #endregion

        #region Player Status

        public event EventHandler<PlayerMaxHealthChangedEventArgs> PlayerMaxHealthChanged;

        public event EventHandler<PlayerCurrentHealthChangedEventArgs> PlayerCurrentHealthChanged;

        public event EventHandler<PlayerPhaseTimeLeftEventArgs> PlayerPhaseTimeLeft;

        private PlayerPhaseTimeLeftEventArgs _phaseTimeLeftArgs = new();

        public event EventHandler<PlayerPhaseCooldownChangedEventArgs> PlayerPhaseCooldownChanged;

        public event EventHandler<PlayerCurrencyChangedEventArgs> PlayerCurrencyChanged;

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

        public void RaiseShopOpened()
        {
            ShopOpened?.Invoke();
        }

        public void RaiseShopClosed()
        {
            ShopClosed?.Invoke();
        }

        public void RaiseItemBought(Item item)
        {
            ItemBoughtEventArgs args = new(item);
            ItemBought?.Invoke(this, args);
        }

        public void RaisePlayerMaxHealthChanged(int playerId, float maxHealth)
        {
            PlayerMaxHealthChangedEventArgs args = new(playerId, maxHealth);
            PlayerMaxHealthChanged?.Invoke(this, args);
        }

        public void RaisePlayerCurrentHealthChanged(int playerId, float currentHealth)
        {
            PlayerCurrentHealthChangedEventArgs args = new(playerId, currentHealth);
            PlayerCurrentHealthChanged?.Invoke(this, args);
        }

        public void RaisePlayerPhaseTimeLeft(int playerId, double timeLeft)
        {
            _phaseTimeLeftArgs.PlayerId = playerId;
            _phaseTimeLeftArgs.TimeLeft = timeLeft;
            PlayerPhaseTimeLeft?.Invoke(this, _phaseTimeLeftArgs);
        }

        public void RaisePlayerPhaseCooldownChanged(int playerId, float cooldown)
        {
            PlayerPhaseCooldownChangedEventArgs args = new(playerId, cooldown);
            PlayerPhaseCooldownChanged?.Invoke(this, args);
        }

        public void RaisePlayerCurrencyChanged(int playerId, int bytes, int flux)
        {
            PlayerCurrencyChangedEventArgs args = new(playerId, bytes, flux);
            PlayerCurrencyChanged?.Invoke(this, args);
        }

        public void RaiseEnemyKilled(EnemyKilledEventArgs args)
        {
            EnemyKilled?.Invoke(this, args);
        }
    }
}
