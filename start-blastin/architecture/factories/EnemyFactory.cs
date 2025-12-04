using System;
using System.Reflection;
using Enemies;
using Godot;
using NanoidDotNet;
using Utility;

namespace Factories
{
    public class EnemyFactory
    {
        public static EnemyNode CreateEnemy(EnemyResource enemyResource)
        {
            EnemyNode enemy = null;

            try
            {
                // Instantiate the enemy scene.
                enemy = GD.Load<PackedScene>(enemyResource.ScenePath).Instantiate<EnemyNode>();

                if (enemy == null)
                {
                    throw new ArgumentException(
                        $"Could not instantiate scene based on the scene path of {enemyResource}!",
                        paramName: nameof(enemyResource)
                    );
                }
                else if (enemy is EnemyNode enemyNode)
                {
                    enemyNode.Name = $"{enemyNode.GetType().Name}-{Nanoid.Generate(size: 8)}";
                    enemyNode.Initialize(enemyResource);

                    return enemyNode;
                }
                else
                {
                    throw new InvalidOperationException(
                        $"Instantiated Enemy object is not an EnemyNode!"
                    );
                }
            }
            catch (Exception e)
            {
                DebugLogger.LogMessage($"{e.Source}: {e.Message}", true, true);
                return enemy;
            }
        }
    }
}
