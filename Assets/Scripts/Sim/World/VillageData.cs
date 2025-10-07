// Assets/Scripts/Sim/World/VillageData.cs
// C# 8.0
using System;
using System.Collections.Generic;

namespace Sim.World
{
    [Serializable]
    public class VillageData
    {
        public MapSection map;
        public PawnSection pawns;
        public SocialSection social;
        public Dictionary<string, LocationEntry> locations;
    }

    [Serializable]
    public class MapSection
    {
        public Dictionary<string, string> key;
        public string mapPng;
        public MapAnnotations annotations;
    }

    [Serializable]
    public class MapAnnotations
    {
        public List<MapBuilding> buildings;
        public List<MapFeature> features;
        public List<MapFarm> farms;
    }

    [Serializable]
    public class MapBuilding
    {
        public string name;
        public string location;
    }

    [Serializable]
    public class MapFeature
    {
        public string name;
        public int[] bbox;
        public int[] center;
        public float radius_px;
    }

    [Serializable]
    public class MapFarm
    {
        public int[] field_bbox;
        public string type;
        public int[] farmhouse_bbox;
        public List<int[]> drive_to_center;
    }

    [Serializable]
    public class PawnSection
    {
        public PawnMeta meta;
        public List<VillagePawn> pawns;
    }

    [Serializable]
    public class PawnMeta
    {
        public string units;
        public float padding_ft;
        public float road_width_ft;
        public float path_width_ft;
        public float plaza_side_ft;
        public float fountain_radius_ft;
    }

    [Serializable]
    public class VillagePawn
    {
        public string id;
        public string name;
        public string role;
        public PawnLocation home;
        public PawnLocation workplace;
    }

    [Serializable]
    public class PawnLocation
    {
        public string location;
        public string type;
    }

    [Serializable]
    public class SocialSection
    {
        public bool enabled;
        public List<RelationshipType> relationshipTypes;
        public List<RelationshipSeed> seeds;
    }

    [Serializable]
    public class RelationshipType
    {
        public string id;
        public string description;
        public float minValue;
        public float maxValue;
        public float defaultValue;
        public bool symmetric;
    }

    [Serializable]
    public class RelationshipSeed
    {
        public string @from;
        public string to;
        public string type;
        public float value;
        public string notes;
    }

    [Serializable]
    public class LocationEntry
    {
        public string id;
        public string type;
        public string name;
        public int[] bbox;
        public int[] center;
    }
}
