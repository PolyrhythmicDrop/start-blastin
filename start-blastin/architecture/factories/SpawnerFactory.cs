using System;
using Enemies.Spawners;
using Godot;

namespace Factories
{
    public static class SpawnerFactory
    {
        private static PackedScene _randomSpawnerScene = GD.Load<PackedScene>(
            "uid://b5ki3kbchln2j"
        );

        public static T CreateSpawner<T>()
            where T : EnemySpawner
        {
            // TODO: Add StaticSpawner once you create that.
            EnemySpawner spawner = typeof(T) switch
            {
                Type t when t == typeof(RandomSpawner) =>
                    _randomSpawnerScene.Instantiate<RandomSpawner>(),
                _ => _randomSpawnerScene.Instantiate<RandomSpawner>(),
            };

            return (T)spawner;
        }
    }
}
