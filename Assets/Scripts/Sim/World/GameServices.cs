// Assets/Scripts/Sim/World/GameServices.cs
// C# 8.0
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Reflection;
using Sim.Config;
using Sim.Logging;

namespace Sim.World
{
    public interface IWorldLogger
    {
        void Initialize();
        void World(string message);
    }

    public sealed class WorldLogger : IWorldLogger
    {
        public void Initialize()
        {
            Log.InitLogs();
        }

        public void World(string message)
        {
            if (string.IsNullOrEmpty(message))
                return;

            Log.World(message);
        }
    }

    public interface IContentValidationService
    {
        void ValidateAll();
    }

    public sealed class ContentValidationService : IContentValidationService
    {
        public void ValidateAll()
        {
            ContentValidator.ValidateAll();
        }
    }

    public interface IPanelSettingsProvider
    {
        PanelSettings GetOrCreate();
    }

    public sealed class ResourcesPanelSettingsProvider : IPanelSettingsProvider
    {
        private PanelSettings _cached;

        public PanelSettings GetOrCreate()
        {
            if (_cached != null)
                return _cached;

            _cached = Resources.Load<PanelSettings>("PanelSettings/DefaultPanelSettings");
            if (_cached == null)
            {
                _cached = ScriptableObject.CreateInstance<PanelSettings>();
                _cached.name = "RuntimePanelSettings";
            }

            if (_cached.themeStyleSheet == null)
            {
                var theme = Resources.Load<ThemeStyleSheet>("PanelSettings/DefaultTheme");
                if (theme == null)
                    theme = ScriptableObject.CreateInstance<ThemeStyleSheet>();

                _cached.themeStyleSheet = theme;

                var themeList = _cached.themeStyleSheets;
                if (themeList == null)
                {
                    themeList = new List<ThemeStyleSheet>();
                    var field = typeof(PanelSettings).GetField("m_ThemeStyleSheets", BindingFlags.Instance | BindingFlags.NonPublic);
                    field?.SetValue(_cached, themeList);
                }

                if (themeList != null && !themeList.Contains(theme))
                {
                    themeList.Clear();
                    themeList.Add(theme);
                }
            }

            return _cached;
        }
    }

    public interface IWorldLoader
    {
        Thing LoadDemoWorld(string demoSettingsPath);
    }

    public sealed class PantryWorldLoader : IWorldLoader
    {
        private readonly IWorldLogger _logger;

        public PantryWorldLoader(IWorldLogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Thing LoadDemoWorld(string demoSettingsPath)
        {
            if (string.IsNullOrWhiteSpace(demoSettingsPath))
                throw new ArgumentException("Demo settings path is required.", nameof(demoSettingsPath));

            _logger.World("LoadWorld() begin.");

            var root = ConfigLoader.LoadJson<TempRoot>(demoSettingsPath);
            if (root?.world?.things == null)
                throw new InvalidDataException("demo.settings.json missing world.things");

            WorldState.Things.Clear();

            foreach (var td in root.world.things)
            {
                if (td == null || string.IsNullOrEmpty(td.type))
                    continue;

                if (!string.Equals(td.type, "pantry", StringComparison.OrdinalIgnoreCase))
                    continue;

                var id = td.id;
                if (string.IsNullOrWhiteSpace(id))
                    id = Guid.NewGuid().ToString("N");

                var thing = new Thing(id, td.type, new Vector2Int(td.x, td.y), td.attributes);
                var capacity = thing.GetIntAttr("inventory_capacity", 0);
                if (capacity <= 0)
                    throw new InvalidDataException("Pantry missing attributes.inventory_capacity in demo.settings.json");

                thing.Inventory = new Inventory(capacity);
                WorldState.Things[thing.Id] = thing;
            }

            if (WorldState.Things.Count == 0)
                throw new InvalidDataException("No pantry found in demo.settings.json (type='pantry').");

            WorldState.Selected = WorldState.Things.Values.First();
            _logger.World($"Selected pantry {WorldState.Selected.Id} capacity={WorldState.Selected.Inventory.Capacity}");
            return WorldState.Selected;
        }

        [Serializable]
        private sealed class TempRoot
        {
            public TempWorld world;
        }

        [Serializable]
        private sealed class TempWorld
        {
            public List<ThingDef> things;
            [JsonExtensionData]
            public IDictionary<string, JToken> Extra;
        }
    }

    public sealed class GameServices : IDisposable
    {
        public IWorldLogger WorldLogger { get; }
        public IContentValidationService ContentValidationService { get; }
        public IPanelSettingsProvider PanelSettingsProvider { get; }
        public IWorldLoader WorldLoader { get; }
        public InventoryGridPresenter InventoryGridPresenter { get; }

        public GameServices()
        {
            WorldLogger = new WorldLogger();
            ContentValidationService = new ContentValidationService();
            PanelSettingsProvider = new ResourcesPanelSettingsProvider();
            WorldLoader = new PantryWorldLoader(WorldLogger);
            InventoryGridPresenter = new InventoryGridPresenter();
        }

        public void Dispose()
        {
            if (PanelSettingsProvider is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}
