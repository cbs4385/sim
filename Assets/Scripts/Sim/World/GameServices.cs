// Assets/Scripts/Sim/World/GameServices.cs
// C# 8.0
using System;
using System.Collections;
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
                _cached = RuntimePanelSettings.CreateInstance();
                _cached.name = "RuntimePanelSettings";
            }

            PanelSettingsThemeUtility.EnsureThemeAssigned(_cached);

            return _cached;
        }
    }

    internal static class PanelSettingsThemeUtility
    {
        public static void EnsureThemeAssigned(PanelSettings settings)
        {
            if (settings == null)
                return;

            var theme = Resources.Load<ThemeStyleSheet>("PanelSettings/DefaultTheme");
            if (theme == null)
                theme = ScriptableObject.CreateInstance<ThemeStyleSheet>();

            if (settings.themeStyleSheet != theme)
                settings.themeStyleSheet = theme;

            if (TryPopulateThemeList(settings, theme))
                return;

            // Older Unity versions stored the theme list in a private field. Keep the
            // previous reflection-based fallback for maximum compatibility.
            var field = typeof(PanelSettings).GetField("m_ThemeStyleSheets", BindingFlags.Instance | BindingFlags.NonPublic);
            var list = field?.GetValue(settings) as IList;
            if (list == null)
                return;

            ReplaceThemeListContents(list, theme);
        }

        private static bool TryPopulateThemeList(PanelSettings settings, ThemeStyleSheet theme)
        {
            var property = typeof(PanelSettings).GetProperty("themeStyleSheets", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (property == null)
                return false;

            if (!(property.GetValue(settings) is IList list) || list.IsReadOnly)
                return false;

            ReplaceThemeListContents(list, theme);
            return true;
        }

        private static void ReplaceThemeListContents(IList list, ThemeStyleSheet theme)
        {
            if (list == null)
                return;

            var containsTheme = false;
            foreach (var item in list)
            {
                if (ReferenceEquals(item, theme))
                {
                    containsTheme = true;
                    break;
                }
            }

            if (!containsTheme)
            {
                list.Clear();
                list.Add(theme);
            }
        }
    }

    public interface IWorldLoader
    {
        Thing LoadDemoWorld(string demoSettingsPath);
    }

    public sealed class PantryWorldLoader : IWorldLoader
    {
        private readonly IWorldLogger _logger;
        private readonly IItemDatabase _itemDatabase;

        public PantryWorldLoader(IWorldLogger logger, IItemDatabase itemDatabase)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _itemDatabase = itemDatabase ?? throw new ArgumentNullException(nameof(itemDatabase));
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

            var worldWidth = Math.Max(1, root.world.width);
            var worldHeight = Math.Max(1, root.world.height);
            var bounds = new RectInt(0, 0, worldWidth, worldHeight);

            foreach (var td in root.world.things)
            {
                if (td == null || string.IsNullOrEmpty(td.type))
                    continue;

                if (!string.Equals(td.type, "pantry", StringComparison.OrdinalIgnoreCase))
                    continue;

                var id = td.id;
                if (string.IsNullOrWhiteSpace(id))
                    id = Guid.NewGuid().ToString("N");

                var inventory = BuildInventory(td);
                var pantry = new Station(id, td.type, new Vector2Int(td.x, td.y), td.tags, td.attributes, inventory);
                inventory.Changed += (sender, args) => OnPantryInventoryChanged(pantry, args);

                LogPantryInfo(pantry);
                SeedInventory(pantry, td.container?.inventory);

                WorldState.Things[pantry.Id] = pantry;
            }

            if (WorldState.Things.Count == 0)
                throw new InvalidDataException("No pantry found in demo.settings.json (type='pantry').");

            var selected = WorldState.Things.Values.OfType<Station>().FirstOrDefault();
            if (selected == null)
                throw new InvalidDataException("No pantry station could be created from demo.settings.json.");

            WorldState.Selected = selected;

            var path = GridPathfinder.FindPath(bounds, Vector2Int.zero, WorldState.Selected.Cell, _ => true);
            if (path.Count > 0)
                _logger.World($"Computed path to {WorldState.Selected.Id} with {path.Count} steps.");
            else
                _logger.World($"No path could be computed to {WorldState.Selected.Id}.");

            return WorldState.Selected;
        }

        private Inventory BuildInventory(ThingDef thing)
        {
            if (thing?.container?.inventory == null)
                throw new InvalidDataException($"Pantry '{thing?.id ?? "unknown"}' missing container.inventory definition.");

            var inv = thing.container.inventory;
            if (inv.slots <= 0)
                throw new InvalidDataException($"Pantry '{thing.id}' missing container.inventory.slots.");
            if (inv.stackSize <= 0)
                throw new InvalidDataException($"Pantry '{thing.id}' missing container.inventory.stackSize.");

            return new Inventory(inv.slots, inv.stackSize);
        }

        private void SeedInventory(Station station, ThingInventoryDef inventoryDef)
        {
            if (station == null || inventoryDef?.contents == null)
                return;

            foreach (var entry in inventoryDef.contents)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.item))
                    throw new InvalidDataException($"Pantry '{station.Id}' inventory entry missing item id.");
                if (entry.quantity <= 0)
                    continue;

                if (!_itemDatabase.TryGet(entry.item, out var item))
                    throw new InvalidDataException($"Pantry '{station.Id}' references unknown item '{entry.item}'.");

                if (!station.Inventory.TryAdd(item, entry.quantity, out var remainder) || remainder > 0)
                    throw new InvalidDataException($"Pantry '{station.Id}' cannot store {entry.quantity}x '{entry.item}'.");
            }
        }

        private void OnPantryInventoryChanged(Station station, InventoryChangedEventArgs args)
        {
            if (station == null || args == null)
                return;

            var verb = args.QuantityDelta >= 0 ? "added" : "removed";
            var amount = Math.Abs(args.QuantityDelta);
            var itemId = string.IsNullOrWhiteSpace(args.ItemId) ? "unknown" : args.ItemId;
            var total = station.Inventory.GetQuantity(itemId);
            _logger.World($"Pantry {station.Id} {verb} {amount}x {itemId} (total={total}).");
        }

        private void LogPantryInfo(Station station)
        {
            if (station?.Inventory == null)
                return;

            _logger.World($"Pantry {station.Id} capacity={station.Inventory.Capacity} stackLimit={station.Inventory.StackSizeLimit}.");
        }

        [Serializable]
        private sealed class TempRoot
        {
            public TempWorld world;
        }

        [Serializable]
        private sealed class TempWorld
        {
            // The demo settings JSON includes a large number of fields under "world".
            // We only need "things" for now, but because ConfigLoader is configured to
            // fail on unknown members we must explicitly surface any known top-level
            // fields that appear in the file.  Everything else is captured via
            // JsonExtensionData so future additions continue to deserialize without
            // changes here.
            public int width;
            public int height;
            public float blockedChance;
            public int shards;
            public int rngSeed;
            public JToken map;
            public JToken facts;
            public List<ThingDef> things;

            [JsonExtensionData]
            public IDictionary<string, JToken> Extra { get; set; }
        }
    }

    public sealed class GameServices : IDisposable
    {
        public IWorldLogger WorldLogger { get; }
        public IContentValidationService ContentValidationService { get; }
        public IPanelSettingsProvider PanelSettingsProvider { get; }
        public IWorldLoader WorldLoader { get; }
        public IItemDatabase ItemDatabase { get; }
        public InventoryGridPresenter InventoryGridPresenter { get; }

        public GameServices()
        {
            WorldLogger = new WorldLogger();
            ContentValidationService = new ContentValidationService();
            PanelSettingsProvider = new ResourcesPanelSettingsProvider();
            ItemDatabase = new ItemDatabase(WorldLogger);
            WorldLoader = new PantryWorldLoader(WorldLogger, ItemDatabase);
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
