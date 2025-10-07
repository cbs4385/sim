// Assets/Scripts/Sim/Logging/Log.cs
// C# 8.0
using System;
using System.IO;
using UnityEngine;

namespace Sim.Logging
{
    public static class Log
    {
        private static string _worldLogPath = "";
        public static string WorldLogPath => _worldLogPath;

        public static void InitLogs()
        {
            var logsDir = Path.Combine(Application.dataPath, "..", "logs");
            Directory.CreateDirectory(logsDir);
            Directory.CreateDirectory(Path.Combine(logsDir, "pawn"));
            _worldLogPath = Path.GetFullPath(Path.Combine(logsDir, "CozyLovecraftSim.log.txt"));
            File.AppendAllText(_worldLogPath, $"[{DateTime.Now:O}] World log initialized.{Environment.NewLine}");
        }

        public static void World(string message)
        {
            if (string.IsNullOrEmpty(_worldLogPath)) InitLogs();
            File.AppendAllText(_worldLogPath, $"[{DateTime.Now:O}] {message}{Environment.NewLine}");
        }

        public static void Pawn(string pawnId, string message)
        {
            var logsDir = Path.Combine(Application.dataPath, "..", "logs", "pawn");
            Directory.CreateDirectory(logsDir);
            var path = Path.Combine(logsDir, $"{pawnId}.log.txt");
            File.AppendAllText(path, $"[{DateTime.Now:O}] {message}{Environment.NewLine}");
        }
    }
}
