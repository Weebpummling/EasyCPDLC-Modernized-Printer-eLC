using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Reflection;

namespace EasyCPDLC.GNS430
{
    internal sealed class Gns430PanelArtwork : IDisposable
    {
        private static readonly IReadOnlyDictionary<string, RectangleF> Bounds = new Dictionary<string, RectangleF>(StringComparer.OrdinalIgnoreCase)
        {
            ["com_flip"] = new(153, 51, 54, 70),
            ["vloc_flip"] = new(153, 132, 54, 72),
            ["range"] = new(781, 40, 151, 54),
            ["direct_to"] = new(779, 94, 76, 53),
            ["menu"] = new(856, 94, 76, 53),
            ["clear"] = new(779, 151, 76, 55),
            ["enter"] = new(856, 151, 76, 55),
            ["cdi"] = new(243, 343, 82, 50),
            ["obs"] = new(350, 343, 83, 50),
            ["msg"] = new(457, 343, 84, 50),
            ["fpl"] = new(567, 343, 84, 50),
            ["proc"] = new(674, 343, 88, 50),
            ["left_small_top"] = new(29, 25, 89, 89),
            ["left_small_bottom"] = new(29, 106, 89, 89),
            ["left_encoder"] = new(2, 248, 156, 158),
            ["right_encoder"] = new(799, 246, 158, 160),
        };

        private readonly Dictionary<string, Image> images = new(StringComparer.OrdinalIgnoreCase);

        internal Image PanelBackground { get; }

        internal Gns430PanelArtwork()
        {
            PanelBackground = Load("panel-background.png");
        }

        internal void DrawState(Graphics graphics, string control, string state)
        {
            if (graphics == null || string.IsNullOrWhiteSpace(control) || string.IsNullOrWhiteSpace(state) || !Bounds.TryGetValue(control, out RectangleF bounds))
            {
                return;
            }

            string relative = $"controls/{control}-{state}.png";
            Image image = Load(relative);
            graphics.DrawImage(image, bounds);
            DrawButtonFinish(graphics, control, state, bounds);
        }

        private static void DrawButtonFinish(Graphics graphics, string control, string state, RectangleF controlBounds)
        {
            RectangleF pressedBounds = PressedSegmentBounds(control, state, controlBounds);
            if (pressedBounds.IsEmpty)
            {
                return;
            }

            GraphicsState saved = graphics.Save();
            try
            {
                graphics.SetClip(pressedBounds);
                using LinearGradientBrush face = new(
                    pressedBounds,
                    Color.FromArgb(72, 255, 255, 255),
                    Color.FromArgb(100, 0, 0, 0),
                    LinearGradientMode.Vertical);
                face.InterpolationColors = new ColorBlend
                {
                    Colors = new[]
                    {
                        Color.FromArgb(58, 255, 255, 255),
                        Color.FromArgb(18, 255, 255, 255),
                        Color.FromArgb(42, 0, 0, 0),
                        Color.FromArgb(92, 0, 0, 0)
                    },
                    Positions = new[] { 0f, 0.28f, 0.68f, 1f }
                };
                graphics.FillRectangle(face, pressedBounds);

                using Pen upperEdge = new(Color.FromArgb(72, 255, 255, 255), 1f);
                using Pen lowerEdge = new(Color.FromArgb(118, 0, 0, 0), 1f);
                graphics.DrawLine(upperEdge, pressedBounds.Left + 2, pressedBounds.Top + 1, pressedBounds.Right - 2, pressedBounds.Top + 1);
                graphics.DrawLine(lowerEdge, pressedBounds.Left + 2, pressedBounds.Bottom - 1, pressedBounds.Right - 2, pressedBounds.Bottom - 1);
            }
            finally
            {
                graphics.Restore(saved);
            }

            if (control.Equals("range", StringComparison.OrdinalIgnoreCase))
            {
                float join = controlBounds.Left + (controlBounds.Width / 2f);
                using Pen sharedSeam = new(Color.FromArgb(155, 7, 8, 8), 1f);
                using Pen sharedHighlight = new(Color.FromArgb(55, 255, 255, 255), 1f);
                graphics.DrawLine(sharedSeam, join, controlBounds.Top + 4, join, controlBounds.Bottom - 4);
                graphics.DrawLine(sharedHighlight, join + 1, controlBounds.Top + 5, join + 1, controlBounds.Bottom - 5);
            }
        }

        internal static RectangleF PressedSegmentBounds(string control, string state, RectangleF controlBounds)
        {
            if (string.IsNullOrWhiteSpace(control) || string.IsNullOrWhiteSpace(state) || !state.Contains("pressed", StringComparison.OrdinalIgnoreCase))
            {
                return RectangleF.Empty;
            }

            if (!control.Equals("range", StringComparison.OrdinalIgnoreCase))
            {
                return controlBounds;
            }

            float halfWidth = controlBounds.Width / 2f;
            if (state.Equals("decrease-pressed", StringComparison.OrdinalIgnoreCase))
            {
                return new RectangleF(controlBounds.Left, controlBounds.Top, halfWidth, controlBounds.Height);
            }

            if (state.Equals("increase-pressed", StringComparison.OrdinalIgnoreCase))
            {
                return new RectangleF(controlBounds.Left + halfWidth, controlBounds.Top, halfWidth, controlBounds.Height);
            }

            return controlBounds;
        }

        internal static RectangleF ControlBounds(string control)
        {
            return Bounds.TryGetValue(control ?? string.Empty, out RectangleF bounds) ? bounds : RectangleF.Empty;
        }

        private Image Load(string relative)
        {
            if (images.TryGetValue(relative, out Image cached))
            {
                return cached;
            }

            Assembly assembly = typeof(Gns430PanelArtwork).Assembly;
            string suffix = ".GNS430.Assets." + relative.Replace('/', '.').Replace('\\', '.');
            string resource = assembly.GetManifestResourceNames().FirstOrDefault(name => name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));
            if (resource == null)
            {
                throw new FileNotFoundException("Embedded GNS 430 artwork was not found.", relative);
            }

            using Stream stream = assembly.GetManifestResourceStream(resource);
            using Image source = Image.FromStream(stream ?? throw new FileNotFoundException("Embedded GNS 430 artwork could not be opened.", resource));
            Image loaded = new Bitmap(source);
            images[relative] = loaded;
            return loaded;
        }

        public void Dispose()
        {
            foreach (Image image in images.Values.Distinct())
            {
                image.Dispose();
            }
            images.Clear();
        }
    }
}
