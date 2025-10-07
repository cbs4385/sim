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

        public string DemoSettingsPath = "Assets/Data/demo.settings.json";
        public string ItemsPath = "Assets/Data/goap/items.json";

        void Awake()
        {
            Application.targetFrameRate = 60;
            _services = new GameServices();

            var logger = _services.WorldLogger;
            logger.Initialize();
            logger.World("GameBootstrap.Awake()");

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
            _root.Add(header);

            // Inventory grid presenter
            _inventoryUI = _services.InventoryGridPresenter;
            _root.Add(_inventoryUI.Root);

            var worldLoader = _services.WorldLoader;
            var selected = worldLoader.LoadDemoWorld(DemoSettingsPath);
            if (selected?.Inventory != null)
            {
                _inventoryUI.Bind(selected.Inventory);
                logger.World($"Inventory UI bound to {selected.Id}");
            }
        }

        void OnDestroy()
        {
            _services?.Dispose();
            _services = null;
        }
    }
}
