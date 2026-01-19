using Godot;
using NanoidDotNet;

namespace Enemies.Spawners
{
    public enum SpawnerLocation
    {
        Top,
        Left,
        Right,
        Bottom,
    }

    /// <summary>
    /// Base class for configuring an EnemySpawner object.
    /// Used by a SpawnerFormationScaler and the ScaleManager to generate spawners.
    /// </summary>
    [GlobalClass]
    public partial class SpawnerConfig : Resource
    {
        protected SpawnerLocation _location = SpawnerLocation.Top;

        [Export]
        public SpawnerLocation Location
        {
            get => _location;
            set => _location = value;
        }

        public virtual void ConfigureSpawner(EnemySpawner spawner, double? waveTime = null)
        {
            // Set the spawner's position, size, and rotation based on the location.
            Vector2 position;
            float rotationDegrees;
            Curve2D curve;

            switch (_location)
            {
                default:
                case SpawnerLocation.Top:
                    position = new Vector2(50, -82);
                    rotationDegrees = 0;
                    curve = ResourceLoader.Load<Curve2D>(
                        "res://resources/curves/spawner-top-or-bottom.tres"
                    );
                    break;
                case SpawnerLocation.Left:
                    position = new Vector2(-82, 50);
                    rotationDegrees = 0;
                    curve = ResourceLoader.Load<Curve2D>(
                        "res://resources/curves/spawner-left-or-right.tres"
                    );
                    break;
                case SpawnerLocation.Right:
                    position = new Vector2(2000, 1100);
                    rotationDegrees = 180;
                    curve = ResourceLoader.Load<Curve2D>(
                        "res://resources/curves/spawner-left-or-right.tres"
                    );
                    break;
                case SpawnerLocation.Bottom:
                    position = new Vector2(1870, 1162);
                    rotationDegrees = 180;
                    curve = ResourceLoader.Load<Curve2D>(
                        "res://resources/curves/spawner-top-or-bottom.tres"
                    );
                    break;
            }

            spawner.Name = $"{spawner.GetType().Name}-{Nanoid.Generate(size: 5)}";
            spawner.Curve = curve;
            spawner.Position = position;
            spawner.RotationDegrees = rotationDegrees;
            spawner.Location = _location;
        }
    }
}
