// Assets/Scripts/Sim/World/MapLoader.cs
// C# 8.0
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Sim.Config;

namespace Sim.World
{
    public class MapLoader : MonoBehaviour
    {
        private const string DefaultVillageDataPath = "Assets/Data/world/village_data.json";
        private const string DefaultMapPng = "Assets/Data/world/village_map_1000x1000.png";

        public Texture2D SourceMap;
        public string VillageDataPath = DefaultVillageDataPath;
        public Transform TileParent;
        public float TileSize = 1f;
        public Material TileMaterial;

        private readonly Dictionary<Color32, string> _colorToTile = new Dictionary<Color32, string>();
        private readonly Dictionary<Color32, Material> _materialCache = new Dictionary<Color32, Material>();
        private VillageData _villageData;
        private bool _tilesBuilt;

        public VillageData Data => _villageData;

        private void Awake()
        {
            LoadVillageData();
            LoadTextureIfNeeded();
        }

        private void Start()
        {
            BuildTiles();
        }

        private void LoadVillageData()
        {
            var path = string.IsNullOrWhiteSpace(VillageDataPath) ? DefaultVillageDataPath : VillageDataPath;
            _villageData = ConfigLoader.LoadJson<VillageData>(path);

            if (_villageData?.map?.key == null || _villageData.map.key.Count == 0)
                throw new InvalidDataException("village_data.json missing map.key definition");

            BuildColorIndex(_villageData.map.key);
        }

        private void LoadTextureIfNeeded()
        {
            if (SourceMap != null)
                return;

            var mapPng = _villageData?.map?.mapPng;
            if (string.IsNullOrWhiteSpace(mapPng))
                mapPng = DefaultMapPng;

            if (!mapPng.StartsWith("Assets/", StringComparison.Ordinal))
                mapPng = "Assets/" + mapPng.TrimStart('/');

            var pngPath = ConfigLoader.GetAbsolutePath(mapPng);
            if (!File.Exists(pngPath))
                throw new FileNotFoundException("Map PNG not found", pngPath);

            var bytes = File.ReadAllBytes(pngPath);
            SourceMap = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
            if (!SourceMap.LoadImage(bytes, markNonReadable: false))
                throw new InvalidDataException("Failed to load map PNG bytes");
            SourceMap.filterMode = FilterMode.Point;
        }

        private void BuildColorIndex(Dictionary<string, string> key)
        {
            _colorToTile.Clear();
            foreach (var kvp in key)
            {
                var color = HexToColor32(kvp.Value);
                if (!_colorToTile.ContainsKey(color))
                {
                    _colorToTile[color] = kvp.Key;
                }
            }
        }

        private static Color32 HexToColor32(string hex)
        {
            if (string.IsNullOrWhiteSpace(hex))
                throw new ArgumentException("Hex color is required", nameof(hex));

            var trimmed = hex.Trim().TrimStart('#');
            if (trimmed.Length == 6)
                trimmed += "FF";
            if (trimmed.Length != 8)
                throw new ArgumentException($"Invalid hex color length for '{hex}'", nameof(hex));

            byte Parse(string slice) => byte.Parse(slice, System.Globalization.NumberStyles.HexNumber);

            var r = Parse(trimmed.Substring(0, 2));
            var g = Parse(trimmed.Substring(2, 2));
            var b = Parse(trimmed.Substring(4, 2));
            var a = Parse(trimmed.Substring(6, 2));
            return new Color32(r, g, b, a);
        }

        public string GetTileNameAt(int x, int y)
        {
            if (SourceMap == null)
                throw new InvalidOperationException("Source map not loaded");

            var c32 = (Color32)SourceMap.GetPixel(x, y);
            if (_colorToTile.TryGetValue(c32, out var name))
                return name;

            throw new InvalidDataException($"Color #{c32.r:X2}{c32.g:X2}{c32.b:X2}{c32.a:X2} at {x},{y} not found in tile key.");
        }

        private void BuildTiles()
        {
            if (_tilesBuilt)
                return;

            if (SourceMap == null)
                throw new InvalidOperationException("Source map not loaded");

            var parent = EnsureTileParent();
            var width = SourceMap.width;
            var height = SourceMap.height;
            var pixels = SourceMap.GetPixels32();

            if (pixels.Length != width * height)
                throw new InvalidDataException("Unexpected pixel count in source map");

            var tileSize = Mathf.Max(0.01f, TileSize);
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var index = y * width + x;
                    var color = pixels[index];
                    if (!_colorToTile.TryGetValue(color, out var tileName))
                        throw new InvalidDataException($"Color #{color.r:X2}{color.g:X2}{color.b:X2}{color.a:X2} at {x},{y} not found in tile key.");

                    var tile = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    tile.name = $"{tileName}_{x}_{y}";
                    tile.transform.SetParent(parent, false);
                    tile.transform.localPosition = new Vector3(x * tileSize, y * tileSize, 0f);
                    tile.transform.localScale = Vector3.one * tileSize;

                    var collider = tile.GetComponent<Collider>();
                    if (collider != null)
                        Destroy(collider);

                    var renderer = tile.GetComponent<MeshRenderer>();
                    renderer.sharedMaterial = ResolveMaterial(color);
                }
            }

            _tilesBuilt = true;
        }

        private Transform EnsureTileParent()
        {
            if (TileParent != null)
                return TileParent;

            var go = new GameObject("Tiles");
            go.transform.SetParent(transform, false);
            TileParent = go.transform;
            return TileParent;
        }

        private Material ResolveMaterial(Color32 color)
        {
            if (_materialCache.TryGetValue(color, out var cached))
                return cached;

            Material material;
            if (TileMaterial != null)
            {
                material = new Material(TileMaterial) { color = color };
            }
            else
            {
                var shader = Shader.Find("Unlit/Color") ?? Shader.Find("Standard");
                material = new Material(shader);
                if (material.HasProperty("_Color"))
                    material.color = color;
                else if (material.HasProperty("_BaseColor"))
                    material.SetColor("_BaseColor", color);
            }

            _materialCache[color] = material;
            return material;
        }
    }
}
