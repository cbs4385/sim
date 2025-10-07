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
        /// Loads a Texture2D from Resources/Sprites by relative path (case-insensitive) without extension.
        /// Example: "Items/bread_loaf" -> Assets/Resources/Sprites/Items/bread_loaf.png
        /// Throws if missing.
        /// </summary>
        public static Texture2D LoadTextureFromResources(string relativePathWithoutExtension)
        {
            if (string.IsNullOrWhiteSpace(relativePathWithoutExtension))
                throw new ArgumentException("relativePathWithoutExtension required");

            // Normalize separators
            var key = relativePathWithoutExtension.Replace('\\', '/');
            var tex = Resources.Load<Texture2D>(Path.Combine("Sprites", key));
            if (tex == null)
                throw new FileNotFoundException("Sprite not found in Resources", "Assets/Resources/Sprites/" + key + ".png");
            return tex;
        }
    }
}
