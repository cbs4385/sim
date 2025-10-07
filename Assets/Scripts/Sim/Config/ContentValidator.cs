// Assets/Scripts/Sim/Config/ContentValidator.cs
// C# 8.0
using System;
using System.IO;
using UnityEngine;
using Sim.World;

namespace Sim.Config
{
    /// <summary>
    /// Validates that all configuration JSON and sprites required by the build are present
    /// and syntactically valid. Any missing or malformed asset will raise immediately so
    /// the game fails fast on startup.
    /// </summary>
    public static class ContentValidator
    {
        private static readonly string[] ConfigDirectories =
        {
            "Assets/Data/config",
            "Assets/Data/goap",
            "Assets/Data/world"
        };

        private const string SpritesRoot = "Assets/Resources/Sprites";

        public static void ValidateAll()
        {
            ValidateConfigDirectories();
            ValidateSprites();
        }

        private static void ValidateConfigDirectories()
        {
            foreach (var directory in ConfigDirectories)
            {
                ValidateJsonDirectory(directory);
            }
        }

        private static void ValidateJsonDirectory(string assetsRelativeDirectory)
        {
            var absoluteDirectory = ResolveAbsoluteDirectory(assetsRelativeDirectory);
            if (!Directory.Exists(absoluteDirectory))
                throw new DirectoryNotFoundException($"Required config directory missing: {assetsRelativeDirectory}");

            foreach (var file in Directory.EnumerateFiles(absoluteDirectory, "*.json", SearchOption.TopDirectoryOnly))
            {
                var relative = BuildAssetsRelativePath(file);
                ConfigLoader.LoadJToken(relative);
            }
        }

        private static void ValidateSprites()
        {
            var absoluteDirectory = ResolveAbsoluteDirectory(SpritesRoot);
            if (!Directory.Exists(absoluteDirectory))
                throw new DirectoryNotFoundException($"Required sprite directory missing: {SpritesRoot}");

            foreach (var file in Directory.EnumerateFiles(absoluteDirectory, "*.png", SearchOption.AllDirectories))
            {
                var spriteKey = BuildSpriteKey(file);
                SpriteResolver.EnsureSpriteExists(spriteKey);
            }
        }

        private static string ResolveAbsoluteDirectory(string assetsRelativeDirectory)
        {
            if (string.IsNullOrWhiteSpace(assetsRelativeDirectory))
                throw new ArgumentException("Directory path is required.", nameof(assetsRelativeDirectory));

            var normalized = NormalizeAssetsPath(assetsRelativeDirectory);
            return ConfigLoader.GetAbsolutePath(normalized);
        }

        private static string NormalizeAssetsPath(string path)
        {
            var trimmed = path.Trim();
            var normalized = trimmed.Replace('\\', '/');
            if (!normalized.StartsWith("Assets/", StringComparison.Ordinal))
                normalized = "Assets/" + normalized.TrimStart('/');
            return normalized;
        }

        private static string BuildAssetsRelativePath(string absolutePath)
        {
            var normalized = absolutePath.Replace('\\', '/');
            var dataPath = Application.dataPath.Replace('\\', '/');
            if (!normalized.StartsWith(dataPath, StringComparison.Ordinal))
                throw new InvalidDataException($"Path is not inside Assets: {absolutePath}");

            var relative = normalized.Substring(dataPath.Length).TrimStart('/');
            return $"Assets/{relative}";
        }

        private static string BuildSpriteKey(string absolutePath)
        {
            var normalized = absolutePath.Replace('\\', '/');
            var resourcesRoot = (Application.dataPath + "/Resources/").Replace('\\', '/');
            if (!normalized.StartsWith(resourcesRoot, StringComparison.Ordinal))
                throw new InvalidDataException($"Sprite is not under Resources: {absolutePath}");

            var resourceRelative = normalized.Substring(resourcesRoot.Length);
            if (!resourceRelative.StartsWith("Sprites/", StringComparison.Ordinal))
                throw new InvalidDataException($"Sprite is not under Resources/Sprites: {absolutePath}");

            var withoutPrefix = resourceRelative.Substring("Sprites/".Length);
            if (withoutPrefix.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                withoutPrefix = withoutPrefix.Substring(0, withoutPrefix.Length - 4);

            return withoutPrefix;
        }
    }
}
