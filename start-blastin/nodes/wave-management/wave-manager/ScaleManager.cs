using System;
using System.Collections.Generic;
using System.Reflection;
using Godot;

namespace WaveManagement
{
    [GlobalClass]
    public abstract partial class ScaleManager : Node
    {
        protected WaveManager _waveManager;

        public override void _Ready()
        {
            LoadResourcePools();
        }

        public virtual void Initialize(WaveManager waveManager)
        {
            _waveManager = waveManager;
        }

        /// <summary>
        /// Loads a set of resource pools for scalers.
        /// </summary>
        /// <remarks>
        /// Override this method and call <see cref="LoadResourcePool{T}"/> for all resource pools you need to load.
        protected virtual void LoadResourcePools() { }

        /// <summary>
        /// Loads all the resources of type <typeparamref name="T"/> into the cache from the correct directory.
        /// Adds the loaded resources to the passed <paramref name="pool"/> of resources.
        /// </summary>
        /// <typeparam name="T">The type of resource to load. The type of resource also determines the directory to load from.</typeparam>
        /// <param name="pool">The scaler resource pool to add the loaded resource to.</param>
        protected void LoadResourcePool<T>(List<T> pool)
            where T : WaveScaler
        {
            string directory = "";
            try
            {
                if (typeof(T) == typeof(SpawnerScaler))
                {
                    directory = "res://resources/wave-scalers/spawner-scalers/";
                }
                else if (typeof(T) == typeof(SpawnerFormationScaler))
                {
                    directory = "res://resources/wave-scalers/spawner-formations/";
                }
                else if (typeof(T) == typeof(EnemyScaler))
                {
                    directory = "res://resources/wave-scalers/enemy-scalers/";
                }

                if (directory == "")
                {
                    throw new InvalidCastException(
                        $"Type {typeof(T).Name} does not have a valid resource pool for this object!"
                    );
                }

                string[] resourceStrings = ResourceLoader.ListDirectory(directory);
                foreach (string resourceName in resourceStrings)
                {
                    string fullPath = directory + resourceName;
                    GD.Print(
                        $"{MethodBase.GetCurrentMethod().Name}: Adding resource from {fullPath} to {pool}..."
                    );
                    pool.Add(ResourceLoader.Load<T>(fullPath));
                }
            }
            catch (Exception e)
            {
                GD.PrintErr(e.Message);
            }
        }

        /// <summary>
        /// Sets the current wave scaler to a scaler from a pool.
        /// </summary>
        /// <remarks>
        /// Override this method and call <see cref="SelectScaler{T}"/> for all resource pools you need to load.
        public virtual void SetCurrentScalers(int wave) { }

        /// <summary>
        /// Selects a scaler resource of type <typeparamref name="T"/> from the passed pool and returns it.
        /// </summary>
        /// <typeparam name="T">The type of scaler resource to find and return.</typeparam>
        /// <param name="pool">The pool to select a <typeparamref name="T"/> from.</param>
        /// <param name="wave">The current wave. Used to select an appropriate scaler.</param>
        /// <param name="defaultPath">File path of the default scaler to use in case an appropriate scaler isn't found in the pool.</param>
        /// <returns></returns>
        protected T SelectScaler<T>(List<T> pool, int wave, string defaultPath)
            where T : WaveScaler
        {
            GD.Print(
                $"{MethodBase.GetCurrentMethod().ReflectedType}.{MethodBase.GetCurrentMethod().Name}: Selecting {typeof(T).Name} for wave {wave}..."
            );
            try
            {
                List<T> matchingConfigs = pool.FindAll(config =>
                    (config.MinWave <= wave || config.MinWave == -1)
                    && (config.MaxWave >= wave || config.MaxWave == -1)
                );
                if (matchingConfigs.Count <= 0)
                {
                    throw new InvalidOperationException(
                        $"Could not find a {typeof(T).Name} that fits wave {wave} or that is set to infinite! Loading default config path..."
                    );
                }

                int selection = GD.RandRange(0, matchingConfigs.Count - 1);
                GD.Print(
                    $"Returning {matchingConfigs[selection].ResourceName} as the selected scaler!"
                );
                return matchingConfigs[selection];
            }
            catch (Exception e)
            {
                GD.PrintErr(e.Message);
                return ResourceLoader.Load<T>(defaultPath);
            }
        }
    }
}
