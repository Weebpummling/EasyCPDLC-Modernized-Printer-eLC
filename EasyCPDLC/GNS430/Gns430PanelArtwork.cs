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

        // The photographed knobs protrude from the faceplate, so their mechanical
        // axes sit slightly below the geometric centers of their rectangular crops.
        private static readonly IReadOnlyDictionary<string, PointF> EncoderPivots = new Dictionary<string, PointF>(StringComparer.OrdinalIgnoreCase)
        {
            ["left_encoder"] = new(78f, 82f),
            ["right_encoder"] = new(79f, 83f),
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

            if (EncoderPivots.ContainsKey(control) && !state.Equals("normal", StringComparison.OrdinalIgnoreCase))
            {
                DrawEncoderState(graphics, control, state, bounds);
                return;
            }

            string visualState = AssetStateFor(state);
            string relative = $"controls/{control}-{visualState}.png";
            Image image = Load(relative);
            graphics.DrawImage(image, bounds);
            DrawButtonFinish(graphics, control, state, bounds);
        }

        private void DrawEncoderState(Graphics graphics, string control, string state, RectangleF bounds)
        {
            Image normal = Load($"controls/{control}-normal.png");
            graphics.DrawImage(normal, bounds);

            PointF pivot = EncoderPivots[control];
            if (state.Equals("pushed", StringComparison.OrdinalIgnoreCase))
            {
                RectangleF cap = new(bounds.X + pivot.X - 41, bounds.Y + pivot.Y - 41, 82, 82);
                using LinearGradientBrush pushShade = new(
                    cap,
                    Color.FromArgb(20, 255, 255, 255),
                    Color.FromArgb(108, 0, 0, 0),
                    LinearGradientMode.Vertical);
                graphics.FillEllipse(pushShade, cap);
                return;
            }

            bool large = state.StartsWith("large-", StringComparison.OrdinalIgnoreCase);
            bool small = state.StartsWith("small-", StringComparison.OrdinalIgnoreCase);
            bool clockwise = state.EndsWith("-cw", StringComparison.OrdinalIgnoreCase);
            bool counterClockwise = state.EndsWith("-ccw", StringComparison.OrdinalIgnoreCase);
            if ((!large && !small) || (!clockwise && !counterClockwise))
            {
                return;
            }

            string layerName = large ? "large" : "small";
            Image layer = Load($"controls/{control}-{layerName}.png");
            float angle = clockwise ? 12f : -12f;
            float pivotX = bounds.X + pivot.X;
            float pivotY = bounds.Y + pivot.Y;
            GraphicsState saved = graphics.Save();
            try
            {
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                graphics.TranslateTransform(pivotX, pivotY);
                graphics.RotateTransform(angle);
                graphics.TranslateTransform(-pivotX, -pivotY);
                graphics.DrawImage(layer, bounds);
            }
            finally
            {
                graphics.Restore(saved);
            }
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
                RectangleF faceBounds = ButtonFaceBounds(controlBounds);
                using GraphicsPath faceShape = RoundedRectangle(faceBounds, Math.Min(7f, faceBounds.Height / 3f));
                graphics.SetClip(faceShape);
                if (!control.Equals("range", StringComparison.OrdinalIgnoreCase))
                {
                    graphics.IntersectClip(pressedBounds);
                }
                bool connectedRange = control.Equals("range", StringComparison.OrdinalIgnoreCase);
                RectangleF gradientBounds = connectedRange ? faceBounds : pressedBounds;
                using LinearGradientBrush face = new(
                    gradientBounds,
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
                graphics.FillRectangle(face, gradientBounds);

                using Pen upperEdge = new(Color.FromArgb(72, 255, 255, 255), 1f);
                using Pen lowerEdge = new(Color.FromArgb(118, 0, 0, 0), 1f);
                RectangleF edgeBounds = connectedRange ? faceBounds : pressedBounds;
                graphics.DrawLine(upperEdge, edgeBounds.Left + 2, edgeBounds.Top + 1, edgeBounds.Right - 2, edgeBounds.Top + 1);
                graphics.DrawLine(lowerEdge, edgeBounds.Left + 2, edgeBounds.Bottom - 1, edgeBounds.Right - 2, edgeBounds.Bottom - 1);

                if (connectedRange)
                {
                    DrawRangeSideShade(graphics, faceBounds, state);
                }
            }
            finally
            {
                graphics.Restore(saved);
            }

        }

        private static void DrawRangeSideShade(Graphics graphics, RectangleF faceBounds, string state)
        {
            bool decrease = state.Equals("decrease-pressed", StringComparison.OrdinalIgnoreCase);
            bool increase = state.Equals("increase-pressed", StringComparison.OrdinalIgnoreCase);
            if (!decrease && !increase)
            {
                return;
            }

            using LinearGradientBrush sideShade = new(
                faceBounds,
                Color.Transparent,
                Color.Transparent,
                LinearGradientMode.Horizontal);
            sideShade.InterpolationColors = new ColorBlend
            {
                Colors = decrease
                    ? new[] { Color.FromArgb(58, 0, 0, 0), Color.FromArgb(28, 0, 0, 0), Color.Transparent, Color.Transparent }
                    : new[] { Color.Transparent, Color.Transparent, Color.FromArgb(28, 0, 0, 0), Color.FromArgb(58, 0, 0, 0) },
                Positions = new[] { 0f, 0.38f, 0.58f, 1f }
            };
            graphics.FillRectangle(sideShade, faceBounds);
        }

        internal static string AssetStateFor(string state)
        {
            return !string.IsNullOrWhiteSpace(state) && state.Contains("pressed", StringComparison.OrdinalIgnoreCase) ? "normal" : state;
        }

        internal static RectangleF PressedSegmentBounds(string control, string state, RectangleF controlBounds)
        {
            if (string.IsNullOrWhiteSpace(control) || string.IsNullOrWhiteSpace(state) || !state.Contains("pressed", StringComparison.OrdinalIgnoreCase))
            {
                return RectangleF.Empty;
            }

            RectangleF faceBounds = ButtonFaceBounds(controlBounds);
            if (!control.Equals("range", StringComparison.OrdinalIgnoreCase))
            {
                return faceBounds;
            }

            float join = controlBounds.Left + (controlBounds.Width / 2f);
            if (state.Equals("decrease-pressed", StringComparison.OrdinalIgnoreCase))
            {
                return new RectangleF(faceBounds.Left, faceBounds.Top, join - faceBounds.Left, faceBounds.Height);
            }

            if (state.Equals("increase-pressed", StringComparison.OrdinalIgnoreCase))
            {
                return new RectangleF(join, faceBounds.Top, faceBounds.Right - join, faceBounds.Height);
            }

            return faceBounds;
        }

        private static RectangleF ButtonFaceBounds(RectangleF controlBounds)
        {
            float horizontalInset = Math.Max(3f, controlBounds.Width * 0.04f);
            float topInset = Math.Max(4f, controlBounds.Height * 0.08f);
            float bottomInset = Math.Max(5f, controlBounds.Height * 0.12f);
            return new RectangleF(
                controlBounds.Left + horizontalInset,
                controlBounds.Top + topInset,
                controlBounds.Width - (horizontalInset * 2f),
                controlBounds.Height - topInset - bottomInset);
        }

        private static GraphicsPath RoundedRectangle(RectangleF bounds, float radius)
        {
            float diameter = radius * 2f;
            GraphicsPath path = new();
            path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
            path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
            path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }

        internal static RectangleF ControlBounds(string control)
        {
            return Bounds.TryGetValue(control ?? string.Empty, out RectangleF bounds) ? bounds : RectangleF.Empty;
        }

        internal static PointF EncoderPivot(string control)
        {
            return EncoderPivots.TryGetValue(control ?? string.Empty, out PointF pivot) ? pivot : PointF.Empty;
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
