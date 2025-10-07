// Assets/Scripts/Sim/World/VillageSpawner.cs
// C# 8.0
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Sim.Config;

namespace Sim.World
{
    public class VillageSpawner : MonoBehaviour
    {
        private const string DefaultVillageDataPath = "Assets/Data/world/village_data.json";

        public string VillageDataPath = DefaultVillageDataPath;
        public MapLoader MapLoader;
        public Transform HouseholdParent;
        public Transform JobParent;
        public Transform PointOfInterestParent;
        public Color HouseholdColor = new Color(0.95f, 0.75f, 0.2f, 1f);
        public Color JobColor = new Color(0.2f, 0.55f, 0.95f, 1f);
        public Color PointOfInterestColor = new Color(0.4f, 0.85f, 0.5f, 1f);
        public float MarkerScale = 0.6f;
        public float MarkerHeight = 0.5f;
        public IWorldLogger Logger { get; set; }

        private readonly Dictionary<Color, Material> _materialCache = new Dictionary<Color, Material>();

        private void Awake()
        {
            if (MapLoader == null)
                MapLoader = GetComponent<MapLoader>();
        }

        private void Start()
        {
            var data = ResolveVillageData();
            if (data == null)
                throw new InvalidDataException("Village data could not be loaded for spawners");

            SpawnFromData(data);
        }

        private VillageData ResolveVillageData()
        {
            if (MapLoader?.Data != null)
                return MapLoader.Data;

            var path = string.IsNullOrWhiteSpace(VillageDataPath) ? DefaultVillageDataPath : VillageDataPath;
            return ConfigLoader.LoadJson<VillageData>(path);
        }

        private void SpawnFromData(VillageData data)
        {
            WorldState.Households.Clear();
            WorldState.JobSites.Clear();
            WorldState.PointsOfInterest.Clear();

            var householdIds = SpawnHouseholds(data);
            var jobIds = SpawnJobs(data);
            SpawnPointsOfInterest(data, householdIds, jobIds);
        }

        private HashSet<string> SpawnHouseholds(VillageData data)
        {
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (data?.locations == null)
                return ids;

            var parent = EnsureParent(ref HouseholdParent, "Households");
            foreach (var entry in data.locations.Values)
            {
                if (entry == null)
                    continue;

                if (!IsHousehold(entry.type))
                    continue;

                if (string.IsNullOrWhiteSpace(entry.id))
                    continue;

                var rect = ToRect(entry.bbox, entry.id);
                var center = ToVector(entry.center, entry.id);
                var household = new Household(entry.id, entry.type, rect, center);
                WorldState.Households[household.Id] = household;
                ids.Add(entry.id);

                CreateMarker(parent, $"Household_{entry.id}", center, HouseholdColor);
            }

            Logger?.World($"Spawned {ids.Count} households from data.");
            return ids;
        }

        private HashSet<string> SpawnJobs(VillageData data)
        {
            var jobs = new Dictionary<string, JobSite>(StringComparer.OrdinalIgnoreCase);
            if (data?.pawns?.pawns != null)
            {
                foreach (var pawn in data.pawns.pawns)
                {
                    if (pawn?.workplace?.location == null)
                        continue;

                    var locationId = pawn.workplace.location;
                    if (!data.locations.TryGetValue(locationId, out var location))
                        throw new InvalidDataException($"Workplace '{locationId}' referenced by pawn '{pawn.id}' not found in locations.");

                    if (!jobs.TryGetValue(locationId, out var jobSite))
                    {
                        var rect = ToRect(location.bbox, locationId);
                        var center = ToVector(location.center, locationId);
                        jobSite = new JobSite(location.id, location.type, rect, center);
                        jobs[locationId] = jobSite;
                    }

                    if (!string.IsNullOrWhiteSpace(pawn.role) && !jobSite.Roles.Contains(pawn.role))
                        jobSite.Roles.Add(pawn.role);
                }
            }

            var parent = EnsureParent(ref JobParent, "Jobs");
            foreach (var job in jobs.Values)
            {
                WorldState.JobSites[job.Id] = job;
                CreateMarker(parent, $"Job_{job.Id}", job.Center, JobColor);
            }

            Logger?.World($"Spawned {jobs.Count} job sites from data.");
            return new HashSet<string>(jobs.Keys, StringComparer.OrdinalIgnoreCase);
        }

        private void SpawnPointsOfInterest(VillageData data, HashSet<string> householdIds, HashSet<string> jobIds)
        {
            var parent = EnsureParent(ref PointOfInterestParent, "PointsOfInterest");
            var usedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (householdIds != null)
                usedIds.UnionWith(householdIds);
            if (jobIds != null)
                usedIds.UnionWith(jobIds);

            if (data?.locations != null)
            {
                foreach (var entry in data.locations.Values)
                {
                    if (entry == null || string.IsNullOrWhiteSpace(entry.id))
                        continue;

                    if (usedIds.Contains(entry.id))
                        continue;

                    var rect = ToRect(entry.bbox, entry.id);
                    var center = ToVector(entry.center, entry.id);
                    var poi = new PointOfInterest(entry.id, entry.name ?? entry.id, rect, center);
                    WorldState.PointsOfInterest[poi.Id] = poi;
                    CreateMarker(parent, $"POI_{entry.id}", center, PointOfInterestColor);
                }
            }

            if (data?.map?.annotations?.features != null)
            {
                foreach (var feature in data.map.annotations.features)
                {
                    if (feature == null || string.IsNullOrWhiteSpace(feature.name))
                        continue;

                    var rect = ToRect(feature.bbox, feature.name);
                    var center = new Vector2Int(rect.xMin + rect.width / 2, rect.yMin + rect.height / 2);
                    var id = $"feature:{feature.name}";
                    var poi = new PointOfInterest(id, feature.name, rect, center);
                    WorldState.PointsOfInterest[id] = poi;
                    CreateMarker(parent, $"POI_{feature.name}", center, PointOfInterestColor);
                }
            }

            Logger?.World($"Spawned {WorldState.PointsOfInterest.Count} points of interest from data.");
        }

        private static bool IsHousehold(string type)
        {
            if (string.IsNullOrWhiteSpace(type))
                return false;

            var lowered = type.ToLowerInvariant();
            return lowered.Contains("home") || lowered.Contains("house");
        }

        private Transform EnsureParent(ref Transform parent, string name)
        {
            if (parent != null)
                return parent;

            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            parent = go.transform;
            return parent;
        }

        private void CreateMarker(Transform parent, string name, Vector2Int center, Color color)
        {
            var marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker.name = name;
            marker.transform.SetParent(parent, false);

            var tileSize = MapLoader != null ? Mathf.Max(0.01f, MapLoader.TileSize) : 1f;
            var scale = Mathf.Max(0.01f, MarkerScale) * tileSize;
            marker.transform.localScale = new Vector3(scale, MarkerHeight, scale);
            marker.transform.localPosition = new Vector3(center.x * tileSize, center.y * tileSize, -0.1f);

            var collider = marker.GetComponent<Collider>();
            if (collider != null)
                Destroy(collider);

            var renderer = marker.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = ResolveMaterial(color);
        }

        private Material ResolveMaterial(Color color)
        {
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

        private static RectInt ToRect(int[] bbox, string id)
        {
            if (bbox == null || bbox.Length != 4)
                throw new InvalidDataException($"Location '{id}' missing bbox");

            var xMin = bbox[0];
            var yMin = bbox[1];
            var xMax = bbox[2];
            var yMax = bbox[3];
            if (xMax < xMin || yMax < yMin)
                throw new InvalidDataException($"Location '{id}' has invalid bbox");

            var width = Math.Max(1, xMax - xMin);
            var height = Math.Max(1, yMax - yMin);
            return new RectInt(xMin, yMin, width, height);
        }

        private static Vector2Int ToVector(int[] center, string id)
        {
            if (center == null || center.Length < 2)
                throw new InvalidDataException($"Location '{id}' missing center");

            return new Vector2Int(center[0], center[1]);
        }
    }
}
