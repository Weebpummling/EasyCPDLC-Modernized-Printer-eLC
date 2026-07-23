using System;
using System.Drawing;
using System.IO;
using System.Text.Json;

namespace EasyCPDLC.GNS430
{
    internal sealed class Gns430Preferences
    {
        public bool CompanionModuleEnabled { get; set; }
        public bool DcduCompanionMode { get; set; }
        public int Left { get; set; } = -1;
        public int Top { get; set; } = -1;
        public int Width { get; set; } = 960;
        public int Height { get; set; } = 455;

        private static string SettingsPath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "EasyCPDLC",
            "gns430.json");

        internal static Gns430Preferences Load()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    return JsonSerializer.Deserialize<Gns430Preferences>(File.ReadAllText(SettingsPath))
                        ?? new Gns430Preferences();
                }
            }
            catch
            {
                // A damaged personal preference file must never prevent EasyCPDLC from starting.
            }

            return new Gns430Preferences();
        }

        internal void Save(Rectangle bounds)
        {
            Left = bounds.Left;
            Top = bounds.Top;
            Width = bounds.Width;
            Height = bounds.Height;

            try
            {
                string directory = Path.GetDirectoryName(SettingsPath);
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(SettingsPath, JsonSerializer.Serialize(this, new JsonSerializerOptions
                {
                    WriteIndented = true
                }));
            }
            catch
            {
                // Preferences are optional; the panel remains usable with defaults.
            }
        }
    }
}
