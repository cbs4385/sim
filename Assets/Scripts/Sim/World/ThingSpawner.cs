// Assets/Scripts/Sim/World/ThingSpawner.cs
// C# 8.0
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Sim.World
{
    public sealed class ThingSpawner : MonoBehaviour
    {
        public MapLoader MapLoader;
        public Transform ThingParent;
        public Color StationColor = new Color(0.9f, 0.4f, 0.25f, 1f);
        public Color StorageColor = new Color(0.2f, 0.6f, 0.85f, 1f);
        public Color FurnitureColor = new Color(0.85f, 0.65f, 0.25f, 1f);
        public Color DefaultColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        public float MarkerScale = 0.55f;
        public float MarkerHeight = 0.35f;
        public float MarkerDepthOffset = -0.2f;
        public IWorldLogger Logger { get; set; }

        private readonly Dictionary<string, GameObject> _spawned = new Dictionary<string, GameObject>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<Color, Material> _materialCache = new Dictionary<Color, Material>();

        public void Spawn(IEnumerable<Thing> things)
        {
            ClearExisting();

            if (things == null)
                return;

            var parent = EnsureParent();
            var tileSize = MapLoader != null ? Mathf.Max(0.01f, MapLoader.TileSize) : 1f;
            var spawnedCount = 0;

            foreach (var thing in things.Where(t => t != null))
            {
                var marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
                marker.name = $"Thing_{thing.Id}";
                marker.transform.SetParent(parent, false);

                var scale = Mathf.Max(0.01f, MarkerScale) * tileSize;
                var height = Mathf.Max(0.01f, MarkerHeight);
                marker.transform.localScale = new Vector3(scale, height, scale);
                marker.transform.localPosition = new Vector3(thing.Cell.x * tileSize, thing.Cell.y * tileSize, MarkerDepthOffset);

                if (marker.TryGetComponent<Collider>(out var collider))
                    Destroy(collider);

                if (marker.TryGetComponent<MeshRenderer>(out var renderer))
                    renderer.sharedMaterial = ResolveMaterial(thing);

                _spawned[thing.Id] = marker;
                spawnedCount++;
            }

            Logger?.World($"Spawned {spawnedCount} thing markers from data.");
        }

        private Transform EnsureParent()
        {
            if (ThingParent != null)
                return ThingParent;

            var go = new GameObject("Things");
            go.transform.SetParent(transform, false);
            ThingParent = go.transform;
            return ThingParent;
        }

        private Material ResolveMaterial(Thing thing)
        {
            var color = DetermineColor(thing);
            if (_materialCache.TryGetValue(color, out var cached))
                return cached;

            var shader = Shader.Find("Unlit/Color") ?? Shader.Find("Standard");
            var material = new Material(shader);
            if (material.HasProperty("_Color"))
                material.color = color;
            else if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);

            _materialCache[color] = material;
            return material;
        }

        private Color DetermineColor(Thing thing)
        {
            if (thing is Station)
                return StationColor;

            if (thing.HasTag("storage"))
                return StorageColor;

            if (thing.HasTag("furniture"))
                return FurnitureColor;

            return DefaultColor;
        }

        private void ClearExisting()
        {
            foreach (var kvp in _spawned)
            {
                var go = kvp.Value;
                if (go == null)
                    continue;

                if (Application.isPlaying)
                    Destroy(go);
                else
                    DestroyImmediate(go);
            }

            _spawned.Clear();
        }

        private void OnDestroy()
        {
            ClearExisting();
        }
    }
}
