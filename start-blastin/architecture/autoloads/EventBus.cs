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
        [Signal]
        public delegate void WaveStartedEventHandler(int wave);

        [Signal]
        public delegate void WaveTimeLeftEventHandler(float timeLeft, float totalTime);

        [Signal]
        public delegate void WaveTimerEndedEventHandler();

        [Signal]
        public delegate void WaveCompleteEventHandler();

        [Signal]
        public delegate void StartWaveButtonPressedEventHandler();

        [Signal]
        public delegate void SpawnersReadyEventHandler();
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
        /// TODO: Figure out how to get the player ID to this signal so that we can give money to the correct player in multiplayer.
        /// </summary>
        // [Signal]
        // public delegate void EnemyKilledEventHandler(int playerId, int flux, int bytes);

        public event EventHandler<EnemyKilledEventArgs> EnemyKilled;

        #endregion


        public override void _Ready()
        {
            Instance = this;
        }

        public void RaiseEnemyKilled(EnemyKilledEventArgs args)
        {
            EnemyKilled?.Invoke(this, args);
        }
    }
}
