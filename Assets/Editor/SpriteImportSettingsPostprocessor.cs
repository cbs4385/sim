// Assets/Editor/SpriteImportSettingsPostprocessor.cs
// C# 8.0
#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

internal sealed class SpriteImportSettingsPostprocessor : AssetPostprocessor
{
    private const string SpritesRoot = "Assets/Resources/Sprites";

    void OnPreprocessTexture()
    {
        if (!IsSpriteTarget(assetPath))
            return;

        var importer = (TextureImporter)assetImporter;
        ApplySpriteSettings(importer);
    }

    private static bool IsSpriteTarget(string path)
    {
        if (string.IsNullOrEmpty(path))
            return false;

        return path.StartsWith(SpritesRoot, StringComparison.OrdinalIgnoreCase) && path.EndsWith(".png", StringComparison.OrdinalIgnoreCase);
    }

    private static void ApplySpriteSettings(TextureImporter importer)
    {
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.mipmapEnabled = false;
        importer.isReadable = true;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.filterMode = FilterMode.Point;
        importer.sRGBTexture = false;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.alphaIsTransparency = true;
    }
}
#endif
