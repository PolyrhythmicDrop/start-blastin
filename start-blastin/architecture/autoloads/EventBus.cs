using System;
using System.Collections.Generic;
using Enemies;
using Events;
using Godot;
using Items;
using Projectiles;

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

        #region Items and UI

        public event Action ShopOpened;

        public event Action ShopClosed;

        public event EventHandler<PlayerIdEventArgs> InventoryOpened;
        public event EventHandler<PlayerIdEventArgs> InventoryClosed;

        public event EventHandler<ItemBoughtEventArgs> ItemBought;

        public event EventHandler<ItemScrappedEventArgs> ItemScrapped;

        #endregion

        #region Player Status

        public event EventHandler<PlayerMaxHealthChangedEventArgs> PlayerMaxHealthChanged;

        public event EventHandler<PlayerCurrentHealthChangedEventArgs> PlayerCurrentHealthChanged;

        public event EventHandler<PlayerIdEventArgs> PhaseStarted;

        public event EventHandler<PlayerIdEventArgs> PhaseEnded;

        public event EventHandler<PlayerPhaseCooldownTimeLeftEventArgs> PlayerPhaseCooldownTimeLeft;

        private PlayerPhaseCooldownTimeLeftEventArgs _phaseCooldownTimeLeftArgs = new();

        public event EventHandler<PlayerPhaseCooldownChangedEventArgs> PlayerPhaseCooldownChanged;

        public event EventHandler<PlayerCurrencyChangedEventArgs> PlayerCurrencyChanged;

        public event EventHandler<PlayerPluginsChangedEventArgs> PlayerPluginsChanged;

        public event EventHandler<PlayerWeaponChangedEventArgs> PlayerWeaponChanged;

        public event EventHandler<PlayerPluginEquippedEventArgs> PlayerPluginEquipped;

        public event EventHandler<PlayerItemRemovedEventArgs> PlayerItemRemoved;

        #endregion

        #region Player Actions

        public event EventHandler<PlayerHitByProjectileEventArgs> PlayerHitByProjectile;

        public event EventHandler<PlayerTakeDamageEventArgs> PlayerTakeDamage;

        #endregion

        #region Enemies

        /// <summary>
        /// Enemy was killed by player.
        ///</summary>
        public event EventHandler<EnemyKilledEventArgs> EnemyKilled;

        public event EventHandler<EnemyHitEventArgs> EnemyHit;

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

        public void RaiseInventoryOpened(int id)
        {
            PlayerIdEventArgs args = new(id);
            InventoryOpened?.Invoke(this, args);
        }

        public void RaiseInventoryClosed(int id)
        {
            PlayerIdEventArgs args = new(id);
            InventoryClosed?.Invoke(this, args);
        }

        public void RaiseItemBought(Item item)
        {
            ItemBoughtEventArgs args = new(item);
            ItemBought?.Invoke(this, args);
        }

        public void RaiseItemScrapped(Item item)
        {
            ItemScrappedEventArgs args = new(item);
            ItemScrapped?.Invoke(this, args);
        }

        public void RaisePlayerMaxHealthChanged(int playerId, float maxHealth)
        {
            PlayerMaxHealthChangedEventArgs args = new(playerId, maxHealth);
            PlayerMaxHealthChanged?.Invoke(this, args);
        }

        public void RaisePlayerCurrentHealthChanged(int playerId, float currentHealth, float diff)
        {
            PlayerCurrentHealthChangedEventArgs args = new(playerId, currentHealth, diff);
            PlayerCurrentHealthChanged?.Invoke(this, args);
        }

        public void RaisePhaseStarted(int playerId)
        {
            PlayerIdEventArgs args = new(playerId);
            PhaseStarted?.Invoke(this, args);
        }

        public void RaisePhaseEnded(int playerId)
        {
            PlayerIdEventArgs args = new(playerId);
            PhaseEnded?.Invoke(this, args);
        }

        public void RaisePlayerPhaseCooldownTimeLeft(
            int playerId,
            double timeLeft,
            double totalTime
        )
        {
            _phaseCooldownTimeLeftArgs.PlayerId = playerId;
            _phaseCooldownTimeLeftArgs.TimeLeft = timeLeft;
            _phaseCooldownTimeLeftArgs.TotalTime = totalTime;
            PlayerPhaseCooldownTimeLeft?.Invoke(this, _phaseCooldownTimeLeftArgs);
        }

        public void RaisePlayerPhaseCooldownChanged(
            int playerId,
            float newCooldown,
            float origCooldown
        )
        {
            PlayerPhaseCooldownChangedEventArgs args = new(playerId, newCooldown, origCooldown);
            PlayerPhaseCooldownChanged?.Invoke(this, args);
        }

        public void RaisePlayerCurrencyChanged(
            int playerId,
            int totalBytes,
            int totalFlux,
            int bytesChange = 0,
            int fluxChange = 0
        )
        {
            PlayerCurrencyChangedEventArgs args = new(
                playerId,
                totalBytes,
                totalFlux,
                bytesChange,
                fluxChange
            );
            PlayerCurrencyChanged?.Invoke(this, args);
        }

        public void RaisePlayerPluginsChanged(int playerId, List<Plugin> plugins)
        {
            PlayerPluginsChangedEventArgs args = new(playerId, plugins);
            PlayerPluginsChanged?.Invoke(this, args);
        }

        public void RaisePlayerWeaponChanged(int playerId, WeaponPlugin plugin)
        {
            PlayerWeaponChangedEventArgs args = new(playerId, plugin);
            PlayerWeaponChanged?.Invoke(this, args);
        }

        public void RaisePlayerPluginEquipped(int playerId, Plugin newPlugin)
        {
            PlayerPluginEquippedEventArgs args = new(playerId, newPlugin);
            PlayerPluginEquipped?.Invoke(this, args);
        }

        public void RaisePlayerItemRemoved(int playerId, Item item)
        {
            PlayerItemRemovedEventArgs args = new(playerId, item);
            PlayerItemRemoved?.Invoke(this, args);
        }

        public void RaisePlayerHitByProjectile(int playerId, Projectile projectile)
        {
            PlayerHitByProjectileEventArgs args = new(playerId, projectile);
            PlayerHitByProjectile?.Invoke(this, args);
        }

        public void RaisePlayerTakeDamage(int playerId, float damage, object source = null)
        {
            PlayerTakeDamageEventArgs args = new(playerId, damage);
            PlayerTakeDamage?.Invoke(source ?? this, args);
        }

        public void RaiseEnemyKilled(EnemyKilledEventArgs args)
        {
            EnemyKilled?.Invoke(this, args);
        }

        public void RaiseEnemyHit(int playerId, EnemyNode enemy)
        {
            EnemyHitEventArgs args = new(playerId, enemy);
            EnemyHit?.Invoke(this, args);
        }
    }
}
