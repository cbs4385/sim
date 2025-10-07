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
                var fallbackKey = TryNormalizeDirectoryCasing(key);
                if (!string.IsNullOrEmpty(fallbackKey))
                {
                    resourcePath = $"Sprites/{fallbackKey}";
                    sprite = Resources.Load<Sprite>(resourcePath);
                    if (sprite != null)
                        key = fallbackKey;
                }
            }

            if (sprite == null)
            {
                var assetPath = $"Assets/Resources/Sprites/{key}.png";
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
                trimmed = trimmed.Substring(0, trimmed.Length - 4);

            var normalized = trimmed.Replace('\\', '/');

            if (normalized.StartsWith("Assets/Resources/", StringComparison.OrdinalIgnoreCase))
                normalized = normalized.Substring("Assets/Resources/".Length);

            if (normalized.StartsWith("Resources/", StringComparison.OrdinalIgnoreCase))
                normalized = normalized.Substring("Resources/".Length);

            if (normalized.StartsWith("Sprites/", StringComparison.OrdinalIgnoreCase))
                normalized = normalized.Substring("Sprites/".Length);

            if (normalized.StartsWith("/", StringComparison.Ordinal))
                normalized = normalized.TrimStart('/');

            return normalized;
        }

        private static string TryNormalizeDirectoryCasing(string key)
        {
            if (string.IsNullOrEmpty(key))
                return string.Empty;

            var segments = key.Split('/');
            if (segments.Length <= 1)
                return string.Empty;

            var changed = false;
            for (var i = 0; i < segments.Length - 1; i++)
            {
                var segment = segments[i];
                if (string.IsNullOrEmpty(segment))
                    continue;

                var corrected = char.ToUpperInvariant(segment[0]) + segment.Substring(1);
                if (!string.Equals(corrected, segment, StringComparison.Ordinal))
                {
                    segments[i] = corrected;
                    changed = true;
                }
            }

            if (!changed)
                return string.Empty;

            return string.Join("/", segments);
        }
    }
}
