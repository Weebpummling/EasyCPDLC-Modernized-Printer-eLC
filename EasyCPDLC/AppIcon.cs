using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace EasyCPDLC
{
    internal static class EasyCPDLCAppIcon
    {
        private static Icon cachedIcon;

        public static void Apply(Form form)
        {
            if (form == null || form.IsDisposed)
            {
                return;
            }

            Icon icon = Load();
            if (icon != null)
            {
                form.Icon = icon;
            }
        }

        public static void ApplyOpenForms()
        {
            try
            {
                foreach (Form form in Application.OpenForms)
                {
                    Apply(form);
                }
            }
            catch
            {
                // Cosmetic only.
            }
        }

        private static Icon Load()
        {
            if (cachedIcon != null)
            {
                return cachedIcon;
            }

            cachedIcon = EmbeddedAssets.LoadIcon("EasyCPDLC.ico") ?? LoadLooseIcon("EasyCPDLC.ico") ?? LoadLooseIcon("EZCPDLCIcon_64.ico");
            return cachedIcon;
        }

        private static Icon LoadLooseIcon(string fileName)
        {
            foreach (string path in CandidatePaths(fileName))
            {
                if (File.Exists(path))
                {
                    return new Icon(path);
                }
            }

            return null;
        }

        private static System.Collections.Generic.IEnumerable<string> CandidatePaths(string fileName)
        {
            yield return Path.Combine(AppContext.BaseDirectory, "Resources", fileName);
            yield return Path.Combine(Application.StartupPath, "Resources", fileName);
            yield return Path.Combine(Environment.CurrentDirectory, "Resources", fileName);
            yield return Path.Combine(AppContext.BaseDirectory, fileName);
            yield return Path.Combine(Application.StartupPath, fileName);
            yield return Path.Combine(Environment.CurrentDirectory, fileName);
        }
    }
}
