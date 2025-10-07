// Assets/Scripts/Sim/World/GameBootstrap.cs
// C# 8.0
using System;
using UnityEngine;
using UnityEngine.UIElements;
using Microsoft.Extensions.DependencyInjection;

namespace Sim.World
{
    public class GameBootstrap : MonoBehaviour
    {
        private VisualElement _root;
        private InventoryGridPresenter _inventoryUI;
        private IServiceProvider _serviceProvider;

        public string DemoSettingsPath = "Assets/Data/demo.settings.json";
        public string ItemsPath = "Assets/Data/goap/items.json";

        void Awake()
        {
            Application.targetFrameRate = 60;
            _serviceProvider = BuildServiceProvider();

            var logger = _serviceProvider.GetRequiredService<IWorldLogger>();
            logger.Initialize();
            logger.World("GameBootstrap.Awake()");

            var validator = _serviceProvider.GetRequiredService<IContentValidationService>();
            validator.ValidateAll();

            // Create a UI Document at runtime so no inspector references are required.
            var uiDoc = gameObject.AddComponent<UIDocument>();
            var panelSettingsProvider = _serviceProvider.GetRequiredService<IPanelSettingsProvider>();
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
            _inventoryUI = _serviceProvider.GetRequiredService<InventoryGridPresenter>();
            _root.Add(_inventoryUI.Root);

            var worldLoader = _serviceProvider.GetRequiredService<IWorldLoader>();
            var selected = worldLoader.LoadDemoWorld(DemoSettingsPath);
            if (selected?.Inventory != null)
            {
                _inventoryUI.Bind(selected.Inventory);
                logger.World($"Inventory UI bound to {selected.Id}");
            }
        }

        private IServiceProvider BuildServiceProvider()
        {
            var services = new ServiceCollection();
            services.AddGameServices();
            return services.BuildServiceProvider();
        }

        void OnDestroy()
        {
            if (_serviceProvider is IDisposable disposable)
            {
                disposable.Dispose();
                _serviceProvider = null;
            }
        }
    }
}
