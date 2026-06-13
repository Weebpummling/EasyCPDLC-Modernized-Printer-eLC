using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Media;
using System.Reflection;

namespace EasyCPDLC
{
    internal static class EmbeddedAssets
    {
        private static readonly Assembly Assembly = typeof(EmbeddedAssets).Assembly;

        public static Stream Open(string folder, string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return null;
            }

            string normalizedFolder = Normalize(folder);
            string normalizedFile = Normalize(fileName);

            string resourceName = Assembly.GetManifestResourceNames()
                .FirstOrDefault(name =>
                {
                    string normalizedName = Normalize(name);
                    return normalizedName.EndsWith("." + normalizedFile, StringComparison.OrdinalIgnoreCase) &&
                           (string.IsNullOrWhiteSpace(normalizedFolder) || normalizedName.Contains("." + normalizedFolder + ".", StringComparison.OrdinalIgnoreCase));
                });

            if (resourceName == null)
            {
                return null;
            }

            Stream stream = Assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                return null;
            }

            // Return a MemoryStream so consumers can keep/use it independently of the manifest stream.
            MemoryStream copy = new MemoryStream();
            stream.CopyTo(copy);
            stream.Dispose();
            copy.Position = 0;
            return copy;
        }

        public static Image LoadImage(string folder, string fileName)
        {
            using Stream stream = Open(folder, fileName);
            if (stream == null)
            {
                return null;
            }

            using Image image = Image.FromStream(stream);
            return new Bitmap(image);
        }

        public static Icon LoadIcon(string fileName)
        {
            Stream stream = Open("Resources", fileName) ?? Open(string.Empty, fileName);
            return stream == null ? null : new Icon(stream);
        }

        public static bool ConfigureSoundPlayer(SoundPlayer soundPlayer, string fileName)
        {
            if (soundPlayer == null)
            {
                return false;
            }

            Stream stream = Open("Sounds", fileName) ?? Open("Resources", fileName);
            if (stream == null)
            {
                return false;
            }

            try
            {
                if (soundPlayer.Stream != null)
                {
                    soundPlayer.Stream.Dispose();
                }
            }
            catch
            {
                // Ignore cleanup issues; we are replacing the stream anyway.
            }

            soundPlayer.SoundLocation = string.Empty;
            soundPlayer.Stream = stream;
            soundPlayer.LoadAsync();
            return true;
        }

        private static string Normalize(string value)
        {
            return (value ?? string.Empty)
                .Replace('\\', '.')
                .Replace('/', '.')
                .Replace(' ', '_')
                .Trim('.');
        }
    }
}
