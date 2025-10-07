// Assets/Scripts/Sim/World/SpriteResolver.cs
// C# 8.0
using System;
using System.IO;
using UnityEngine;

namespace Sim.World
{
    public static class SpriteResolver
    {
        /// <summary>
        /// Loads a Sprite from Resources/Sprites using an exact relative path without an extension.
        /// Example: "Items/bread_loaf" -> Assets/Resources/Sprites/Items/bread_loaf.png.
        /// Throws if the sprite cannot be found.
        /// </summary>
        public static Sprite LoadSpriteFromResources(string relativePathWithoutExtension)
        {
            if (string.IsNullOrWhiteSpace(relativePathWithoutExtension))
                throw new ArgumentException("relativePathWithoutExtension required", nameof(relativePathWithoutExtension));

            var key = Normalize(relativePathWithoutExtension);
            var resourcePath = $"Sprites/{key}";
            var sprite = Resources.Load<Sprite>(resourcePath);
            if (sprite == null)
            {
                var assetPath = $"Assets/Resources/{resourcePath}.png";
                throw new FileNotFoundException($"Sprite not found in Resources: {resourcePath}", assetPath);
            }

            return sprite;
        }

        public static Texture2D LoadTextureFromResources(string relativePathWithoutExtension)
        {
            var sprite = LoadSpriteFromResources(relativePathWithoutExtension);
            if (sprite.texture == null)
                throw new InvalidDataException($"Sprite '{relativePathWithoutExtension}' does not have an associated texture.");
            return sprite.texture;
        }

        public static void EnsureSpriteExists(string relativePathWithoutExtension)
        {
            var sprite = LoadSpriteFromResources(relativePathWithoutExtension);
            Resources.UnloadAsset(sprite);
        }

        private static string Normalize(string path)
        {
            var trimmed = path.Trim();
            if (trimmed.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Path should not include file extensions.", nameof(path));

            var normalized = trimmed.Replace('\\', '/');
            if (normalized.StartsWith("Sprites/", StringComparison.Ordinal))
                normalized = normalized.Substring("Sprites/".Length);

            if (normalized.StartsWith("/", StringComparison.Ordinal))
                normalized = normalized.TrimStart('/');

            return normalized;
        }
    }
}
