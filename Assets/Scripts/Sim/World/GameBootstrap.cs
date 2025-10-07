// Assets/Scripts/Sim/World/GameBootstrap.cs
// C# 8.0
using UnityEngine;
using UnityEngine.UIElements;

namespace Sim.World
{
    public class GameBootstrap : MonoBehaviour
    {
        private VisualElement _root;
        private InventoryGridPresenter _inventoryUI;
        private GameServices _services;
        private WorldTimeSystem _timeSystem;
        private MapLoader _mapLoader;
        private VillageSpawner _villageSpawner;

        public string DemoSettingsPath = "Assets/Data/demo.settings.json";
        public string ItemsPath = "Assets/Data/demo.items.json";

        void Awake()
        {
            Application.targetFrameRate = 60;
            _services = new GameServices();

            var logger = _services.WorldLogger;
            logger.Initialize();
            logger.World("GameBootstrap.Awake()");

            var itemDatabase = _services.ItemDatabase;
            itemDatabase.Load(ItemsPath);

            var validator = _services.ContentValidationService;
            validator.ValidateAll();

            // Create a UI Document at runtime so no inspector references are required.
            var uiDoc = gameObject.AddComponent<UIDocument>();
            var panelSettingsProvider = _services.PanelSettingsProvider;
            uiDoc.panelSettings = panelSettingsProvider.GetOrCreate();

            // Build UI tree in code
            _root = new VisualElement();
            _root.style.flexDirection = FlexDirection.Column;
            _root.style.paddingLeft = 8;
            _root.style.paddingTop = 8;
            uiDoc.rootVisualElement.Add(_root);

            var header = new Label("Inventory");
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.style.color = Color.white;
            header.style.marginBottom = 6;
            _root.Add(header);

            // Inventory grid presenter
            _inventoryUI = _services.InventoryGridPresenter;
            _root.Add(_inventoryUI.Root);

            _timeSystem = gameObject.AddComponent<WorldTimeSystem>();
            _timeSystem.CalendarPath = "Assets/Data/world/calendar.json";
            _timeSystem.DayChanged += OnWorldDayChanged;
            _timeSystem.SeasonChanged += OnWorldSeasonChanged;
            _timeSystem.HolidayStarted += OnWorldHolidayStarted;

            OnWorldSeasonChanged(_timeSystem.CurrentSeason);
            OnWorldDayChanged(_timeSystem.DayOfYear);

            var worldRoot = CreateChildTransform("World");
            var tileRoot = CreateChildTransform("Tiles", worldRoot);

            _mapLoader = gameObject.AddComponent<MapLoader>();
            _mapLoader.VillageDataPath = "Assets/Data/world/village_data.json";
            _mapLoader.TileParent = tileRoot;

            _villageSpawner = gameObject.AddComponent<VillageSpawner>();
            _villageSpawner.MapLoader = _mapLoader;
            _villageSpawner.Logger = logger;
            _villageSpawner.VillageDataPath = _mapLoader.VillageDataPath;

            var markerRoot = CreateChildTransform("Markers", worldRoot);
            _villageSpawner.HouseholdParent = CreateChildTransform("Households", markerRoot);
            _villageSpawner.JobParent = CreateChildTransform("Jobs", markerRoot);
            _villageSpawner.PointOfInterestParent = CreateChildTransform("PointsOfInterest", markerRoot);

            var worldLoader = _services.WorldLoader;
            var selectedThing = worldLoader.LoadDemoWorld(DemoSettingsPath);
            if (selectedThing is Station station && station.Inventory != null)
            {
                _inventoryUI.Bind(station.Inventory);
                logger.World($"Inventory UI bound to {station.Id}");
            }
        }

        private Transform CreateChildTransform(string name, Transform parent = null)
        {
            var go = new GameObject(string.IsNullOrWhiteSpace(name) ? "Node" : name);
            go.transform.SetParent(parent == null ? transform : parent, false);
            return go.transform;
        }

        private void OnWorldDayChanged(int day)
        {
            var logger = _services?.WorldLogger;
            if (logger == null)
                return;

            var seasonName = _timeSystem?.CurrentSeason?.Definition?.name ?? "Unknown";
            logger.World($"Day {day} of {seasonName} has begun.");
        }

        private void OnWorldSeasonChanged(CalendarSeasonState season)
        {
            var logger = _services?.WorldLogger;
            if (logger == null)
                return;

            var name = season?.Definition?.name ?? "Unknown";
            logger.World($"Season changed to {name}.");
        }

        private void OnWorldHolidayStarted(CalendarHoliday holiday)
        {
            var logger = _services?.WorldLogger;
            if (logger == null)
                return;

            var name = holiday?.name ?? "Unnamed Holiday";
            logger.World($"Holiday {name} begins.");
        }


        void OnDestroy()
        {
            if (_timeSystem != null)
            {
                _timeSystem.DayChanged -= OnWorldDayChanged;
                _timeSystem.SeasonChanged -= OnWorldSeasonChanged;
                _timeSystem.HolidayStarted -= OnWorldHolidayStarted;
            }

            _services?.Dispose();
            _services = null;
        }
    }
}
