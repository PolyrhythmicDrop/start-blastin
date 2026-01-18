using Godot;
using SafeResourcePicker;
using Utility;

namespace Enemies.Spawners
{
    /// <summary>
    /// Resource for spawning specific enemies at specific times. Used as a component a SpawnPlan in a StaticSpawner.
    /// </summary>
    [GlobalClass]
    [Tool]
    public partial class SpawnStep : Resource
    {
        public SpawnStep()
        {
            _squadChangeCallable = Callable.From(OnSquadronChanged);
            DebugLogger.LogMessage($"Calling spawn step constructor!");
        }

        /// <summary>
        /// The percent of the time through the wave when the <see cref="EnemyType"/> should spawn.
        /// For example, if <see cref="WaveTimeRatio"/> is set to 0.5, the enemy spawns halfway through the wave.
        /// </summary>
        [Export(PropertyHint.Range, "0,1.0")]
        public float WaveTimeRatio { get; set; } = 0;

        /// <summary>
        /// The position along the <see cref="EnemySpawner" /> path where the enemy should spawn.
        /// For example, if <see cref="SpawnPosition" /> is set to 0.5, the enemy spawns in the middle of the EnemySpawner's path.
        /// </summary>
        /// <remarks>
        /// This is tied directly to the <see cref="EnemySpawner" />'s ProgressRatio along its parent PathFollow2D node.
        /// </remarks>
        [Export(PropertyHint.Range, "0,1.0")]
        public float SpawnPosition { get; set; } = 0;

        /// <summary>
        /// Type of enemy to spawn at this step.
        /// </summary>
        [Export(SRP_HINT.RESOURCE_PATH, "EnemyResource")]
        public string EnemyType { get; set; }

        private int _quantity = 1;

        /// <summary>
        /// The number of the <see cref="EnemyType"/> to spawn.
        /// </summary>
        [Export(PropertyHint.Range, "1,10,or_greater")]
        public int Quantity
        {
            get => _quantity;
            set
            {
                _quantity = value;
                if (Engine.IsEditorHint())
                {
                    OnQuantityChanged();
                }
            }
        }

        private bool _squadEnabled = false;

        [ExportGroup("Squadron Enabled")]
        [Export(PropertyHint.GroupEnable)]
        public bool SquadEnabled
        {
            get => _squadEnabled;
            set
            {
                _squadEnabled = value;
                if (Engine.IsEditorHint())
                {
                    OnQuantityChanged();
                }
            }
        }

        private Callable _squadChangeCallable;

        private SquadronLayout _squadron;

        [Export]
        public SquadronLayout Squadron
        {
            get => _squadron;
            set
            {
                if (Engine.IsEditorHint())
                {
                    if (_squadron != null)
                    {
                        if (
                            _squadron.IsConnected(Resource.SignalName.Changed, _squadChangeCallable)
                        )
                        {
                            DebugLogger.LogMessage($"Disconnecting...");
                            _squadron.Disconnect(Resource.SignalName.Changed, _squadChangeCallable);
                        }
                    }
                }
                _squadron = value;
                if (Engine.IsEditorHint())
                {
                    if (_squadron != null)
                    {
                        DebugLogger.LogMessage($"Checking for existing connection...");
                        if (
                            !_squadron.IsConnected(
                                Resource.SignalName.Changed,
                                _squadChangeCallable
                            )
                        )
                        {
                            DebugLogger.LogMessage($"Connecting...");
                            _squadron.Connect(Resource.SignalName.Changed, _squadChangeCallable);
                        }
                    }
                }
            }
        }

        private void OnQuantityChanged()
        {
            if (_quantity > 1 && _squadEnabled == false)
            {
                _squadEnabled = true;
            }
            else if (_quantity <= 1 && _squadEnabled)
            {
                _squadEnabled = false;
            }

            if (_squadEnabled && _squadron == null)
            {
                Squadron = new();
                if (Squadron.Offsets == null)
                {
                    Squadron.Offsets = new();
                }
                for (int i = 0; i < _quantity; i++)
                {
                    Squadron.Offsets.Add(Vector2.Zero);
                }
            }

            if (_squadron != null && _squadron.Offsets != null)
            {
                int count = _squadron.Offsets.Count;
                if (_quantity > count)
                {
                    DebugLogger.LogMessage($"_quantity: {_quantity} | Count: {count}");
                    for (int i = count; i < _quantity; i++)
                    {
                        Squadron.Offsets?.Add(Vector2.Zero);
                        Squadron.EmitChanged();
                    }
                }
                else if (_quantity < count)
                {
                    for (int i = count; i > _quantity; i--)
                    {
                        Squadron.Offsets?.RemoveAt(i - 1);
                        Squadron.EmitChanged();
                    }
                }
            }
        }

        private void OnSquadronChanged()
        {
            DebugLogger.LogMessage($"Squadron changed!");
            if (Engine.IsEditorHint())
            {
                if (_squadron != null)
                {
                    if (_squadron.Offsets != null)
                    {
                        int count = _squadron.Offsets.Count;

                        if (count != Quantity)
                        {
                            Quantity = count;
                        }
                    }
                }
            }
        }
    }
}
