// Assets/Scripts/Sim/World/MapLoader.cs
// C# 8.0
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Sim.Config;
using Sim.Logging;

namespace Sim.World
{
    [Serializable]
    public class VillageData
    {
        public MapSection map;
        public PlacementSection placements;

        [Serializable]
        public class MapSection
        {
            public Dictionary<string, string> key; // TILE_NAME -> #RRGGBB
            public string mapPng; // optional
        }

        [Serializable]
        public class PlacementSection
        {
            public List<ThingDef> things;
        }
    }

    public class MapLoader : MonoBehaviour
    {
        public Texture2D SourceMap;
        public string VillageDataPath = "Assets/Data/village_data.json";

        private Dictionary<Color32, string> _colorToTile = new Dictionary<Color32, string>();

        void Awake()
        {
            if (SourceMap == null)
            {
                // Try to load the PNG mentioned in the JSON if provided
                var vdata = ConfigLoader.LoadJson<VillageData>(VillageDataPath);
                if (vdata.map == null || vdata.map.key == null)
                    throw new InvalidDataException("village_data.json missing map.key");
                BuildColorIndex(vdata.map.key);

                var pngPath = Path.Combine(Application.dataPath, "Data", "village_map_1000x1000.png");
                if (!File.Exists(pngPath))
                    throw new FileNotFoundException("Map PNG not found", pngPath);

                var bytes = File.ReadAllBytes(pngPath);
                SourceMap = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
                if (!SourceMap.LoadImage(bytes, markNonReadable: false))
                    throw new InvalidDataException("Failed to load map PNG bytes");
                SourceMap.filterMode = FilterMode.Point;
            }
            Log.World("MapLoader initialized.");
        }

        private static Color32 HexToColor32(string hex)
        {
            // Accept "#RRGGBB" (no alpha) or "#RRGGBBAA"
            if (string.IsNullOrEmpty(hex)) throw new ArgumentException("hex required");
            var h = hex.Trim().TrimStart('#');
            if (h.Length == 6) h += "FF";
            byte r = byte.Parse(h.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
            byte g = byte.Parse(h.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
            byte b = byte.Parse(h.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
            byte a = byte.Parse(h.Substring(6, 2), System.Globalization.NumberStyles.HexNumber);
            return new Color32(r, g, b, a);
        }

        private void BuildColorIndex(Dictionary<string, string> key)
        {
            _colorToTile.Clear();
            foreach (var kvp in key)
            {
                var c = HexToColor32(kvp.Value);
                _colorToTile[c] = kvp.Key;
            }
        }

        public string GetTileNameAt(int x, int y)
        {
            var c = SourceMap.GetPixel(x, y);
            var c32 = (Color32)c;
            if (_colorToTile.TryGetValue(c32, out var name)) return name;
            throw new InvalidDataException($"Color #{c32.r:X2}{c32.g:X2}{c32.b:X2}{c32.a:X2} at {x},{y} not found in tile key.");
        }
    }
}
