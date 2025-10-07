// Assets/Scripts/Sim/World/RuntimePanelSettings.cs
// C# 8.0
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;

namespace Sim.World
{
    /// <summary>
    /// Utility for creating <see cref="PanelSettings"/> instances that have a theme
    /// assigned immediately after creation. Unity no longer allows inheriting from
    /// <see cref="PanelSettings"/>, so we emulate the previous behaviour by assigning the
    /// theme and re-invoking the internal <c>OnEnable</c> method after instantiation.
    /// </summary>
    public static class RuntimePanelSettings
    {
        private static readonly MethodInfo s_OnEnableMethod = typeof(PanelSettings)
            .GetMethod("OnEnable", BindingFlags.Instance | BindingFlags.NonPublic);

        public static PanelSettings CreateInstance()
        {
            var settings = ScriptableObject.CreateInstance<PanelSettings>();

            PanelSettingsThemeUtility.EnsureThemeAssigned(settings);
            s_OnEnableMethod?.Invoke(settings, null);

            return settings;
        }
    }
}

