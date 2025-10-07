// Assets/Scripts/Sim/World/Models.cs
// C# 8.0
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Sim.World
{
    [Serializable]
    public class ThingDef
    {
        public string id;
        public string type;
        public List<string> tags;
        public int x;
        public int y;
        public Dictionary<string, float> attributes;

        [JsonExtensionData]
        public IDictionary<string, JToken> Extra { get; set; }
    }

    [Serializable]
    public class ItemStack
    {
        public string itemId;
        public int quantity;
    }

    public class Inventory
    {
        public int Capacity { get; private set; }
        public readonly List<ItemStack> Items = new List<ItemStack>();

        public Inventory(int capacity)
        {
            if (capacity <= 0) throw new ArgumentException("Inventory capacity must be > 0");
            Capacity = capacity;
        }
    }

    public class Thing
    {
        public string Id { get; private set; }
        public string Type { get; private set; }
        public Vector2Int Cell { get; private set; }
        public Dictionary<string, float> Attr { get; private set; }
        public Inventory Inventory { get; set; }

        public Thing(string id, string type, Vector2Int cell, Dictionary<string, float> attr)
        {
            Id = id;
            Type = type;
            Cell = cell;
            Attr = attr ?? new Dictionary<string, float>();
        }

        public int GetIntAttr(string name, int fallback = 0)
        {
            if (Attr != null && Attr.TryGetValue(name, out var v)) return Mathf.RoundToInt(v);
            return fallback;
        }
    }

    public class Household
    {
        public string Id { get; }
        public string Type { get; }
        public RectInt Bounds { get; }
        public Vector2Int Center { get; }

        public Household(string id, string type, RectInt bounds, Vector2Int center)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Id is required", nameof(id));
            Id = id;
            Type = type ?? string.Empty;
            Bounds = bounds;
            Center = center;
        }
    }

    public class JobSite
    {
        public string Id { get; }
        public string Type { get; }
        public RectInt Bounds { get; }
        public Vector2Int Center { get; }
        public List<string> Roles { get; } = new List<string>();

        public JobSite(string id, string type, RectInt bounds, Vector2Int center)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Id is required", nameof(id));
            Id = id;
            Type = type ?? string.Empty;
            Bounds = bounds;
            Center = center;
        }
    }

    public class PointOfInterest
    {
        public string Id { get; }
        public string Name { get; }
        public RectInt Bounds { get; }
        public Vector2Int Center { get; }

        public PointOfInterest(string id, string name, RectInt bounds, Vector2Int center)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Id is required", nameof(id));
            Id = id;
            Name = string.IsNullOrWhiteSpace(name) ? id : name;
            Bounds = bounds;
            Center = center;
        }
    }

    public class CalendarState
    {
        public int TotalTicks { get; internal set; }
        public int TicksPerDay { get; internal set; }
        public int DayOfYear { get; internal set; }
        public int DayOfSeason { get; internal set; }
        public string Season { get; internal set; } = string.Empty;
        public bool IsDaylight { get; internal set; }
        public float NormalizedTimeOfDay { get; internal set; }
        public string LastHoliday { get; internal set; } = string.Empty;
    }

    public static class WorldState
    {
        public static readonly Dictionary<string, Thing> Things = new Dictionary<string, Thing>();
        public static readonly Dictionary<string, Household> Households = new Dictionary<string, Household>();
        public static readonly Dictionary<string, JobSite> JobSites = new Dictionary<string, JobSite>();
        public static readonly Dictionary<string, PointOfInterest> PointsOfInterest = new Dictionary<string, PointOfInterest>();
        public static readonly CalendarState Calendar = new CalendarState();
        public static Thing Selected;
    }
}
