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

        #endregion


        public override void _Ready()
        {
            Instance = this;
        }
    }
}
