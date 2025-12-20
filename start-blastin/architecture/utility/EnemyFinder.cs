using System;
using System.Collections.Generic;
using System.Linq;
using Enemies;
using Godot;
using Utility;

namespace Utility
{
    public static class EnemyFinder
    {
        private const string ENEMYGROUP = "enemies";
        private static HashSet<EnemyNode> _enemies;
        private static bool _initialized = false;

        public static void Initialize()
        {
            if (_initialized)
            {
                return;
            }
            _initialized = true;
            _enemies = new();
        }

        public static void CleanUp()
        {
            if (!_initialized)
            {
                return;
            }
            _enemies.Clear();
            _initialized = false;
        }

        public static void RefreshEnemyCache(SceneTree tree)
        {
            _enemies.Clear();

            IEnumerable<EnemyNode> enemies = tree.GetNodesInGroup(ENEMYGROUP).OfType<EnemyNode>();
            foreach (EnemyNode enemy in enemies)
            {
                _enemies.Add(enemy);
            }
        }

        public static IEnumerable<EnemyNode> GetAllEnemies()
        {
            foreach (EnemyNode enemy in _enemies)
            {
                yield return enemy;
            }
        }

        public static void AddEnemy(EnemyNode enemy)
        {
            if (enemy != null)
            {
                _enemies.Add(enemy);
                enemy.TreeExiting += () => OnEnemyExitingTree(enemy);
            }
        }

        private static void OnEnemyExitingTree(EnemyNode enemy)
        {
            if (enemy != null && _enemies.Contains(enemy))
            {
                _enemies.Remove(enemy);
            }
        }

        /// <summary>
        /// Get the closest enemy to the <paramref name="origin"/> point.
        /// </summary>
        /// <param name="origin">The point (in global coordinates) to search from.</param>
        public static EnemyNode GetClosestEnemy(Vector2 origin)
        {
            EnemyNode closest = null;
            float closestDistance = float.MaxValue;

            foreach (EnemyNode enemy in _enemies)
            {
                // If the enemy is not a valid object (i.e. it's queued for deletion or otherwise inactive)
                // continue on to the next one.
                if (!GodotObject.IsInstanceValid(enemy))
                {
                    continue;
                }

                // Get the distance from the enemy to the origin point.
                float distanceTo = origin.DistanceSquaredTo(enemy.GlobalPosition);

                // If the distance to the enemy is less than the current closest distance, set that enemy and distance as the closest.
                if (distanceTo < closestDistance)
                {
                    closest = enemy;
                    closestDistance = distanceTo;
                }
            }

            return closest;
        }
    }
}
