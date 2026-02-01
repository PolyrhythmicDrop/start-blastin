using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autoloads;
using Components;
using Enemies.Spawners;
using Entities;
using Events;
using Factories;
using Godot;
using Interfaces;
using Stats;
using UI;
using Utility;
using WaveManagement;
using Weapons;

namespace Enemies
{
    [GlobalClass]
    public abstract partial class EnemyNode
        : AnimatableBody2D,
            IDie,
            IHealthful,
            IVelocityProvider,
            IWeaponOwner,
            IStats,
            IListener,
            IDeflector
    {
        #region Nodes
        protected StatManager _stats;

        protected WeaponNode _weapon;

        protected CollisionShape2D _shape;

        public VisibleOnScreenNotifier2D VisibleNotifier;

        /// <summary>
        /// The main path the enemy follows. When they reach the end of this path, the enemy despawns.
        /// </summary>
        protected EntityPath _followPath;

        protected AudioComponent _audioComponent;

        protected OverheadHealthBar _healthBar;

        public WeaponNode Weapon => _weapon;
        public EntityPath Path => _followPath;

        #endregion


        #region Position and Velocity
        protected Vector2 _currentGlobalPosition;
        protected Vector2 _lastGlobalPosition;
        protected Vector2 _motion => _currentGlobalPosition - _lastGlobalPosition;
        protected Vector2 _currentVelocity = Vector2.Zero;
        protected Tween _followTween;
        #endregion

        #region Stats


        /// <summary>
        /// The speed at which this enemy follows its assigned path.
        /// </summary>
        protected float _followSpeed => _stats.GetStat(StatType.Speed).CurrentValue;

        protected float _crashDamage => _stats.GetStat(StatType.CrashDamage).CurrentValue;

        protected Vector2 _terminalTrajectory = Vector2.Zero;

        // Base stats
        protected float _baseSpeed;
        protected float _baseCrashDamage;
        protected float _baseMaxHealth;
        protected float _baseFireRate;
        protected float _baseWeaponDamage;
        protected int _fluxReward;
        protected int _byteReward;

        #endregion

        #region State
        protected bool _alive = true;

        protected bool _atPathEnd = false;

        private Callable _screenExitCallable;

        public bool DeflectActive { get; set; }

        /// <summary>
        /// Whether or not the enemy is spawning. Set to false automatically after the enemy leaves the OOB area for the first time.
        /// </summary>
        public bool Spawning { get; set; } = true;

        /// <summary>
        /// Whether or not the enemy is part of a squadron. Enables squadron behavior, like tweening out of the formation at a set time.
        /// </summary>
        public bool InSquadron { get; set; } = false;

        /// <summary>
        /// If the enemy is part of a squadron, this is the final relative position of the enemy after splitting.
        /// </summary>
        public Vector2? SquadronPosition { get; set; }

        /// <summary>
        /// Whether or not the enemy is currently visible on the screen.
        /// </summary>
        public bool OnScreen { get; set; }

        /// <summary>
        /// The point in the enemy's path at which they split off from the main squadron point into their <see cref="SquadronPosition"/>.
        /// </summary>
        public float SplitPoint;

        /// <summary>
        /// If part of a squadron, whether or not the enemy has split from the squadron.
        /// </summary>
        private bool _split = false;

        #endregion


        #region Health

        // current stats
        protected float _currentHealth;
        protected float _maxHealth => _stats.GetStat(StatType.MaxHealth).CurrentValue;
        public float CurrentHealth
        {
            get => _currentHealth;
            private set
            {
                _currentHealth = value;
                _healthBar?.SetValues(_maxHealth, _currentHealth);
            }
        }

        public float MaxHealth
        {
            get => _maxHealth;
            set
            {
                _stats.UpdateStat(StatType.MaxHealth, Mathf.Max(1, value));
                _healthBar?.SetValues(_maxHealth, _currentHealth);
            }
        }

        #region Constants

        protected const float MIN_FOLLOW_TWEEN_DURATION = 0.1f;
        protected const float SQUADRON_TWEEN_DURATION = 1.5f;
        protected const float DAMAGE_ANIM_DURATION = 0.5f;

        #endregion

        /// <summary>
        /// Sets the position of the health bar based on the enemy's current position.
        /// </summary>
        protected virtual void SetHealthBarPosition()
        {
            _healthBar.SetPosition(_currentGlobalPosition);
        }

        /// <summary>
        /// Sets the size of the enemy's health bar based on the size of the enemy's sprite. Override in derived classes.
        /// </summary>
        protected virtual void SetHealthBarSize()
        {
            AnimatedSprite2D sprite = GetPrimarySprite();

            // Get size of the base sprite
            SpriteFrames frames = sprite.SpriteFrames ?? null;
            if (sprite != null)
            {
                Rect2I usedRect = frames.GetFrameTexture("default", 0).GetImage().GetUsedRect();
                _healthBar.SetSizeAndOffset(usedRect.Size);
            }
        }

        protected virtual AnimatedSprite2D GetPrimarySprite() => null;

        /// <summary>
        /// Turn the health bar on and off.
        /// </summary>
        public virtual void ToggleHealthBarActive()
        {
            _healthBar.ToggleActive();
        }

        #endregion

        #region Init

        /// <summary>
        /// Initializes the enemy node from an enemy resource.
        /// Called from the EnemyFactory before the enemy is added to the scene tree, before _Ready().
        /// </summary>
        /// <param name="enemyResource">The resource used to create the enemy.</param>
        public virtual void Initialize(EnemyResource enemyResource)
        {
            // Health
            _baseMaxHealth = enemyResource.MaxHealth;
            _currentHealth = _baseMaxHealth;

            // Currency
            _fluxReward = enemyResource.FluxReward;
            _byteReward = enemyResource.ByteReward;

            // Weapon initialization
            _weapon = WeaponFactory.CreateWeapon(
                enemyResource.WeaponStats,
                velocityProvider: this,
                owner: this
            );
            _baseFireRate = enemyResource.WeaponStats.FireRate;
            _baseWeaponDamage = enemyResource.WeaponStats.Damage;
            _baseSpeed = enemyResource.Speed;
            _baseCrashDamage = enemyResource.CrashDamage;

            // Sound initialization
            _audioComponent = new() { Sounds = enemyResource.Sounds };
            _audioComponent.Initialize(this);

            InitializeStatManager();
        }

        /// <summary>
        /// Initializes the enemy's stat manager and base stats.
        /// Called after <see cref="Initialize"/>, before the enemy is added to the scene tree.
        /// </summary>
        public virtual void InitializeStatManager()
        {
            _stats = new();
            _stats.AddStat(StatType.CrashDamage, _baseCrashDamage);
            _stats.AddStat(StatType.Speed, _baseSpeed);
            _stats.AddStat(StatType.FireRate, _baseFireRate);
            _stats.AddStat(StatType.MaxHealth, _baseMaxHealth);
            _stats.AddStat(StatType.Damage, _baseWeaponDamage);
        }

        public override void _Ready()
        {
            base._Ready();
            AddToGroup("enemies");

            _shape = GetNode<CollisionShape2D>("%CollisionShape2D");
            InitVisibleNotifier();

            AddChild(_weapon);

            // Start the weapon fire timer to fire on a set interval.
            _weapon.FireTimer.Timeout += FireWeapon;

            // Set an initial firing delay
            double delay = RNG.GetRandomDouble(max: _weapon.Stats.FireRate);
            _weapon.FireTimer.Start(delay);

            // Initialize position tracking
            _currentGlobalPosition = GlobalPosition;
            _lastGlobalPosition = _currentGlobalPosition;

            // Initialize the health bar.
            _healthBar = GetNode<OverheadHealthBar>("%OverheadHealthBar");
            _healthBar.Initialize(this);

            ConnectSignals();

            // Call the derived class's Ready steps.
            OnBaseReadyComplete();

            SetHealthBarSize();

            FollowPath(_followSpeed);
        }

        /// <summary>
        /// Setup logic for derived classes. Called during <see cref="_Ready"/>, after base initialization, but before <see cref="SetHealthBarSize"/> and <see cref="FollowPath"/>.
        /// </summary>
        protected virtual void OnBaseReadyComplete() { }

        protected void InitVisibleNotifier()
        {
            VisibleNotifier = new() { Rect = _shape.Shape.GetRect() };
            AddChild(VisibleNotifier);
        }

        public void TweenSquadronPosition(Vector2 offset)
        {
            Tween tween = _followPath.CreateTween();
            Vector2 finalPos = _followPath.Position + offset;

            tween.SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
            tween.TweenProperty(_followPath, "position", finalPos, SQUADRON_TWEEN_DURATION);
        }

        public virtual void ConnectSignals()
        {
            _screenExitCallable = Callable.From(OnScreenExit);

            if (_stats != null)
            {
                _stats.StatUpdated += OnStatUpdated;
            }

            // Connect path end signal
            if (_followPath != null)
            {
                _followPath.PathComplete += OnPathComplete;
            }

            if (VisibleNotifier != null)
            {
                VisibleNotifier.ScreenEntered += OnScreenEntered;
                VisibleNotifier.ScreenExited += OnScreenExited;
            }
        }

        public virtual void DisconnectSignals()
        {
            if (_stats != null)
            {
                _stats.StatUpdated -= OnStatUpdated;
            }
            if (_followPath != null)
            {
                _followPath.PathComplete -= OnPathComplete;
            }
        }

        public virtual void OnScreenEntered()
        {
            OnScreen = true;
        }

        public virtual void OnScreenExited()
        {
            OnScreen = false;
        }

        public void SetPath(EntityPath path)
        {
            _followPath = path;
        }

        public virtual void OnPathComplete()
        {
            _atPathEnd = true;
            if (!VisibleNotifier.IsOnScreen())
            {
                FreeEnemy();
            }
        }

        #endregion

        #region Stats and Scaling

        public StatManager GetStatManager()
        {
            return _stats;
        }

        /// <summary>
        /// Sets a stat value based on a passed StatType.
        /// Used for Effects and other objects so you can use the correct getters/setters instead of accessing the StatManager directly.
        /// </summary>
        /// <param name="type">The stat type to set.</param>
        /// <param name="value">The new value for the stat type.</param>
        public virtual void SetStat(StatType type, float value)
        {
            _stats.UpdateStat(type, value);
        }

        public virtual void OnStatUpdated(object source, StatUpdatedEventArgs args)
        {
            switch (args.StatType)
            {
                case StatType.FireRate:
                case StatType.Damage:
                case StatType.ProjectileSpeed:
                    Weapon.UpdateWeaponStats(args.StatType, args.Stat);
                    return;
                case StatType.Speed:
                    if (_followTween != null && _followTween.IsValid())
                    {
                        AdjustFollowSpeed();
                    }
                    else
                    {
                        FollowPath(_followSpeed);
                    }
                    return;
                default:
                    return;
            }
        }

        /// <summary>
        /// Adjusts the speed at which the enemy follows a path based on the enemy's speed.
        /// </summary>
        protected virtual void AdjustFollowSpeed()
        {
            if (_followPath?.PathFollow == null || _followTween == null)
            {
                return;
            }

            // Get the current progress and calculate the remaining distance
            float currentProgress = _followPath.PathFollow.ProgressRatio;
            float pathLength = _followPath.Curve.GetBakedLength();
            float remainingDistance = pathLength * (1.0f - currentProgress);

            // Calculate the new duration based on the current _followSpeed.
            float duration = Math.Max(remainingDistance / _followSpeed, MIN_FOLLOW_TWEEN_DURATION);

            // Store whether the current tween was paused so we can re-pause it after creating the new one.
            bool wasPaused = !_followTween.IsRunning();

            // Kill and recreate the existing tween
            _followTween.Kill();

            _followTween = CreateTween();
            _followTween.TweenProperty(_followPath.PathFollow, "progress_ratio", 1.0, duration);

            // Pause the new tween if the original tween was paused.
            if (wasPaused)
            {
                _followTween.Pause();
            }
        }

        /// <summary>
        /// Adjusts the enemy's stats based on the current wave and the selected enemy scaler.
        /// </summary>
        /// <param name="scaler">The EnemyScaler object to scale using.</param>
        /// <param name="wave">The current wave.</param>
        /// <remarks>
        /// Called from the <see cref="EnemySpawner"/> after the enemy has been built by the <see cref="EnemyFactory"/> and initialized.
        /// </remarks>
        public virtual void ApplyWaveScaling(EnemyScaler scaler, int wave)
        {
            // Don't apply scaling on the first wave.
            if (wave == 1)
            {
                return;
            }

            float waveLogMultiplier = Mathf.Log(1 + wave);
            float waveSqrtMultiplier = Mathf.Sqrt(wave) * 0.1f;

            float newMaxHealth =
                _baseMaxHealth * (1 + (scaler.MaxHealthModifier * waveLogMultiplier));
            SetStat(StatType.MaxHealth, newMaxHealth);

            // Fill the enemy's health.
            CurrentHealth = MaxHealth;

            float newCrashDamage =
                _baseCrashDamage * (1 + (scaler.CrashDamageModifier * waveLogMultiplier));
            SetStat(StatType.CrashDamage, newCrashDamage);

            float newFollowSpeed = _baseSpeed * (1 + (scaler.SpeedModifier * waveSqrtMultiplier));
            SetStat(StatType.Speed, newFollowSpeed);

            float newDamage =
                _baseWeaponDamage * (1 + (scaler.WeaponDamageModifier * waveSqrtMultiplier));
            SetStat(StatType.Damage, newDamage);

            float waveExpoMultiplier = Mathf.Pow(0.95f, wave * scaler.FireRateModifier);
            // Fire rate should be decreased, since lower fire rates result in faster firing.
            float newFireRate = Mathf.Max(0.1f, _baseFireRate * waveExpoMultiplier);
            SetStat(StatType.FireRate, newFireRate);
        }

        #endregion

        #region Actions

        public Vector2 GetCurrentVelocity()
        {
            return _currentVelocity;
        }

        /// <summary>
        /// Causes this enemy node to take damage.
        /// </summary>
        /// <param name="damage">The amount of damage to take.</param>
        /// <param name="playerId">If a player caused the damage, the <see cref="Player.PlayerId"/> of the damaging player.</param>
        public void TakeDamage(float damage, int? playerId = null)
        {
            if (_alive)
            {
                // Play the hit sound
                // AudioService.Instance.PlaySound(_sounds?.Hit, this, volume: -6);
                _audioComponent.PlayHitSound();

                if (damage != 0)
                {
                    PlayDamageAnimation();
                    IndicatorFactory.CreateTextIndicator(
                        (MathF.Round(damage, 1) * -1).ToString(),
                        GlobalPosition,
                        parent: this
                    );
                    CurrentHealth -= damage;
                }

                if (_currentHealth <= 0)
                {
                    CurrentHealth = 0;
                    Die(playerId);
                }
            }
        }

        public void Heal(float healAmount)
        {
            // Don't do anything if current health is greater than max health
            if (_currentHealth >= _maxHealth)
            {
                return;
            }
            CurrentHealth = Mathf.Min(_currentHealth + healAmount, MaxHealth);
        }

        /// <summary>
        /// Fires the enemy's weapon if the enemy is on the screen.
        /// </summary>
        protected virtual void FireWeapon()
        {
            // Don't fire if we're dead or not on screen.
            if (!_alive || !VisibleNotifier.IsOnScreen())
            {
                return;
            }

            _audioComponent.PlayFireSound();
            PlayFireAnimation();
            _weapon.Fire();
        }

        /// <summary>
        /// Plays the enemy's fire animation. Must be implemented by the derived class.
        /// </summary>
        protected abstract void PlayFireAnimation();

        /// <summary>
        /// Kills the enemy. Raises the <see cref="EventBus.EnemyKilled"/> event if a Player killed the enemy.
        /// </summary>
        /// <param name="playerId">The ID of the player that killed the enemy, if any. This player gets the rewards for killing the enemy.</param>
        /// <remarks>
        /// If <paramref name="playerId"/> is null, the <see cref="EventBus.EnemyKilled"/> event is not raised.
        public virtual async void Die(int? playerId = null)
        {
            // Don't die again if you're already dead.
            if (!_alive)
            {
                return;
            }

            // Standardized death stuff
            _alive = false;
            _weapon.FireTimer.Stop();
            _shape.Disabled = true;

            // Derived class death stuff
            await PlayDeathSequence();

            // Raise the enemy killed event
            if (playerId != null)
            {
                EnemyKilledEventArgs args = new(
                    (int)playerId,
                    _fluxReward,
                    _byteReward,
                    GlobalPosition
                );
                EventBus.Instance.RaiseEnemyKilled(args);
            }

            // Free the enemy from memory
            FreeEnemy();
        }

        /// <summary>
        /// Handles all class-specific death logic, including playing death animations and sounds, freeing related objects, and waiting for animations to complete.
        /// Must be implemented by derived class.
        /// </summary>
        /// <returns></returns>
        protected abstract Task PlayDeathSequence();

        /// <summary>
        /// Callback for when the enemy exits the screen after it has reached the end of its path.
        /// </summary>
        private void OnScreenExit()
        {
            FreeEnemy();
        }

        /// <summary>
        /// Frees the enemy from memory after all its associated audio is finished playing and all its projectiles are disabled.
        /// </summary>
        public async void FreeEnemy()
        {
            _healthBar.ToggleBarVisibility(false);
            // Queue free after all child projectiles die and all child audio nodes stop playing
            await WaitForAudioEnd();
            bool projectilesDisabled = await _weapon.WaitForAllProjectilesDisabled();

            if (projectilesDisabled && !IsQueuedForDeletion())
            {
                QueueFree();
            }
        }

        /// <summary>
        /// Plays the enemy's damage animation using the damage shader.
        /// </summary>
        public virtual void PlayDamageAnimation()
        {
            string mixRatioPath = "mix_ratio";
            string currentFramePath = "current_frame";

            if (Material is ShaderMaterial shaderMaterial)
            {
                shaderMaterial.SetShaderParameter(mixRatioPath, 1.0);

                Tween tween = CreateTween();
                tween.TweenMethod(
                    Callable.From(
                        (int currentFrame) =>
                            shaderMaterial.SetShaderParameter(currentFramePath, currentFrame)
                    ),
                    0,
                    30,
                    DAMAGE_ANIM_DURATION
                );
                tween.TweenCallback(
                    Callable.From(() => shaderMaterial.SetShaderParameter(mixRatioPath, 0))
                );
            }
        }

        public virtual void OnCrash(KinematicCollision2D collision)
        {
            if (collision.GetCollider() is Player player && _alive)
            {
                player.TakeDamage(_crashDamage);
                Die();
            }
        }

        public override void _PhysicsProcess(double delta)
        {
            base._PhysicsProcess(delta);

            // Update position tracking
            _lastGlobalPosition = _currentGlobalPosition;
            _currentGlobalPosition = GlobalPosition;

            if (delta > 0)
            {
                _currentVelocity = (_currentGlobalPosition - _lastGlobalPosition) / (float)delta;
            }
        }

        public override void _Process(double delta)
        {
            if (_motion != Vector2.Zero)
            {
                SetHealthBarPosition();
            }

            // Pre-processing for derived classes
            OnProcessUpdate(delta);

            // Collision detection
            KinematicCollision2D collision = MoveAndCollide(_motion, true);

            if (collision != null)
            {
                OnCrash(collision);
            }

            // Keep going along current motion of at path end and not dead
            if (_atPathEnd && _alive && OnScreen)
            {
                // Connect to the screen exited signal for freeing if you're not already connected to it.
                if (
                    !VisibleNotifier.IsConnected(
                        VisibleOnScreenNotifier2D.SignalName.ScreenExited,
                        _screenExitCallable
                    )
                )
                {
                    ConnectPathEndScreenExited();
                }
                // Set your exiting trajectory if it's not already set.
                if (_terminalTrajectory == Vector2.Zero)
                {
                    SetTerminalTrajectory(delta);
                }

                // Move along the terminal trajectory until you're off the screen.
                MoveAndCollide(_terminalTrajectory);
            }

            // If you've already split from the squadron (or not in a squadron), return.
            if (_split)
            {
                return;
            }

            // Split off from the squadron if we're part of one.
            if (InSquadron && SquadronPosition != null)
            {
                if (_followPath.PathFollow.ProgressRatio > SplitPoint)
                {
                    _split = true;
                    TweenSquadronPosition((Vector2)SquadronPosition);
                }
            }
        }

        /// <summary>
        /// Called during the base <see cref="_Process(double)"/> function. Implemented by derived classes for class-specific behavior during _Process.
        /// </summary>
        /// <param name="delta">The time between frames, retrieved from <see cref="_Process(double)"/>.</param>
        protected virtual void OnProcessUpdate(double delta) { }

        private void ConnectPathEndScreenExited()
        {
            VisibleNotifier.Connect(
                VisibleOnScreenNotifier2D.SignalName.ScreenExited,
                _screenExitCallable
            );
        }

        private void SetTerminalTrajectory(double delta)
        {
            _terminalTrajectory =
                Vector2.Right.Rotated(GlobalRotation) * (_followSpeed * (float)delta);
        }

        /// <summary>
        /// Follows an EntityPath at a set speed.
        /// </summary>
        protected virtual void FollowPath(float speed)
        {
            float pathLength = _followPath.Curve.GetBakedLength();
            float duration = Mathf.Max(pathLength / speed, MIN_FOLLOW_TWEEN_DURATION);

            if (_followTween != null)
            {
                _followTween.Kill();
            }

            _followTween = CreateTween();
            _followTween.TweenProperty(_followPath, "FollowRatio", 1.0, duration);
        }

        /// <summary>
        /// Finds any AudioStreamPlayer2D nodes in the enemy's scene tree and waits for their playback to finish before returning.
        /// </summary>
        /// <returns></returns>
        private async Task WaitForAudioEnd()
        {
            var audioSignals = FindChildren("*", "AudioStreamPlayer2D", owned: false)
                .OfType<AudioStreamPlayer2D>()
                .Where(audio => audio.Playing)
                .Select(audio => ToSignal(audio, AudioStreamPlayer2D.SignalName.Finished));

            List<Task> audioTasks = new();
            foreach (SignalAwaiter awaiter in audioSignals)
            {
                audioTasks.Add(UtilityMethods.SignalAwaiterToTask(awaiter));
            }

            await Task.WhenAll(audioTasks);
        }

        public override void _ExitTree()
        {
            DisconnectSignals();
            base._ExitTree();
        }

        #endregion
    }
}
