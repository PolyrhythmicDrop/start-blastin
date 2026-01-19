using System;
using System.Collections.Generic;
using Godot;
using Utility;

namespace FileIO
{
    public static class PoolLoader
    {
        /// <summary>
        /// Loads all the resources of type <typeparamref name="T"/> into the cache from the passed <paramref name="rootDirectory"/>.
        /// Adds the loaded resources to the passed <paramref name="pool"/> of resources.
        /// </summary>
        /// <typeparam name="T">The type of resource to load. The type of resource also determines the directory to load from.</typeparam>
        /// <param name="pool">The resource pool to add the loaded resources to.</param>
        /// <param name="rootDirectory">The directory containing the resources of type <typeparamref name="T"/> to load.</param>
        /// <param name="recursive">Whether or not to load resources from subdirectories of the <paramref name="rootDirectory"/>.</param>
        public static void LoadResourcePool<T>(
            ICollection<T> pool,
            string rootDirectory,
            bool recursive = false
        )
            where T : Resource
        {
            try
            {
                string[] directoryContents = ResourceLoader.ListDirectory(rootDirectory);
                if (directoryContents.IsEmpty())
                {
                    throw new ArgumentException(
                        $"No subdirectories or resources of type {typeof(T)} found in {rootDirectory}!"
                    );
                }
                else
                {
                    foreach (string itemStr in directoryContents)
                    {
                        string fullPath = rootDirectory + itemStr;

                        // If our item string is a subdirectory (ends with '/') and `recursive` is true,
                        // call LoadResourcePool on the subdirectory.
                        if (itemStr.EndsWith('/') && recursive)
                        {
                            LoadResourcePool(pool, fullPath, recursive);
                        }
                        // Otherwise, it's a resource file, so load it and add it to the pool.
                        else
                        {
                            pool.Add(ResourceLoader.Load<T>(fullPath));
                        }
                    }
                }
            }
            catch (Exception e)
            {
                DebugLogger.LogMessage(e.Message, true, true);
            }
        }
    }
}
