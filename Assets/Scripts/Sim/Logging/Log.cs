// Assets/Scripts/Sim/Logging/Log.cs
// C# 8.0
using System;
using UnityEngine;

namespace Sim.Logging
{
    public static class Log
    {
        private const string WorldLogPrefix = "[World]";
        private const string PawnLogPrefix = "[Pawn]";
        private static bool _initialized;

        public static void InitLogs()
        {
            if (_initialized) return;

            _initialized = true;
            Debug.Log($"{WorldLogPrefix} Logging initialized.");
        }

        public static void World(string message)
        {
            if (!_initialized) InitLogs();

            Debug.Log($"{WorldLogPrefix} {message}");
        }

        public static void Pawn(string pawnId, string message)
        {
            if (string.IsNullOrWhiteSpace(pawnId))
                throw new ArgumentException("Pawn id is required for pawn logs.", nameof(pawnId));

            Debug.Log($"{PawnLogPrefix} [{pawnId}] {message}");
        }
    }
}
