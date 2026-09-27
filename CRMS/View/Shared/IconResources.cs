using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Reflection;

namespace CRMS.winforms.Shared
{
    /// <summary>
    /// Loads the flat line-art action icons (edit/archive/approve/lock) that are
    /// embedded into the assembly from Resources/Icons — used instead of emoji
    /// for the grid action buttons across Branch Management, Pending Customers,
    /// Promotions, and User Management — plus the CRMS "logo" brand mark used on
    /// Login, the sidebar header, and the window/taskbar icon. Each image is
    /// loaded once and cached.
    /// </summary>
    public static class IconResources
    {
        private static readonly Dictionary<string, Image> _cache = new();

        public static Image Get(string name)
        {
            if (_cache.TryGetValue(name, out var cached)) return cached;

            var assembly = Assembly.GetExecutingAssembly();
            using var stream = assembly.GetManifestResourceStream($"CRMS.winforms.Icons.{name}.png")
                ?? throw new InvalidOperationException($"Missing embedded icon resource: {name}");

            var image = Image.FromStream(stream);
            _cache[name] = image;
            return image;
        }

        private static Icon? _appIcon;

        /// <summary>The CRMS logo as a window/taskbar Icon — set once on each
        /// MainForm and on Login so every window shares the same brand mark.</summary>
        public static Icon AppIcon
        {
            get
            {
                if (_appIcon is not null) return _appIcon;
                using var bitmap = new Bitmap(Get("logo"));
                _appIcon = Icon.FromHandle(bitmap.GetHicon());
                return _appIcon;
            }
        }

        /// <summary>Draws one icon centered in a grid cell — the shared bit of
        /// every icon-button CellPainting handler across the app.</summary>
        public static void DrawCentered(Graphics graphics, string iconName, Rectangle cellBounds, int size = 22)
        {
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

            var rect = new Rectangle(
                cellBounds.X + (cellBounds.Width - size) / 2,
                cellBounds.Y + (cellBounds.Height - size) / 2,
                size, size);

            graphics.DrawImage(Get(iconName), rect);
        }
    }
}
