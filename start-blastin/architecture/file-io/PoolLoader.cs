using System;
using System.Collections.Generic;
using Godot;

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
            List<T> pool,
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
                    // Sort the subdirectories from the resource names.
                    List<string> rootResourceStrings = new();
                    List<string> subdirectoryStrings = new();

                    foreach (string str in directoryContents)
                    {
                        if (str.EndsWith('/'))
                        {
                            subdirectoryStrings.Add(str);
                        }
                        else
                        {
                            rootResourceStrings.Add(str);
                        }
                    }

                    foreach (string resourceName in rootResourceStrings)
                    {
                        string fullPath = rootDirectory + resourceName;
                        GD.Print(
                            $"{System.Reflection.MethodBase.GetCurrentMethod().Name}: Adding resource from {fullPath} to {nameof(pool)}..."
                        );
                        pool.Add(ResourceLoader.Load<T>(fullPath));
                    }

                    // Now get resources from any sub-directories and load those too.
                    if (recursive && subdirectoryStrings.Count > 0)
                    {
                        rootDirectory = rootDirectory.EndsWith('/')
                            ? rootDirectory
                            : rootDirectory + '/';
                        foreach (string subDir in subdirectoryStrings)
                        {
                            string subDirPath = rootDirectory + subDir;
                            string[] subDirResources = ResourceLoader.ListDirectory(subDirPath);
                            foreach (string resourceName in subDirResources)
                            {
                                string fullPath = subDirPath + resourceName;
                                GD.Print(
                                    $"{System.Reflection.MethodBase.GetCurrentMethod().Name}: Adding resource from subdirectory {fullPath} to {pool}..."
                                );
                                pool.Add(ResourceLoader.Load<T>(fullPath));
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                GD.PrintErr(e.Message);
            }
        }
    }
}
