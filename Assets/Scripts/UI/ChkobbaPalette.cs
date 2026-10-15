using UnityEngine;

namespace Chkobba
{
    /// Design tokens for the "Chkobba Café" palette (from the approved V1 prototype).
    public static class ChkobbaPalette
    {
        public static readonly Color Cream = Hex(0xF4F1E8);
        public static readonly Color Paper = Hex(0xF7F2E6);
        public static readonly Color Paper2 = Hex(0xFFFDF7);
        public static readonly Color Cobalt = Hex(0x1B4F9C);
        public static readonly Color CobaltDeep = Hex(0x123A73);
        public static readonly Color Sky = Hex(0x5B9BD5);
        public static readonly Color Red = Hex(0xB22222);
        public static readonly Color Ink = Hex(0x1E2A38);
        public static readonly Color Brass = Hex(0xC89B6C);
        public static readonly Color Olive = Hex(0x2F6B4F);
        public static readonly Color MeltTop = Hex(0xBE9B69);
        public static readonly Color MeltEnd = Hex(0xD7C398);

        public static Color Hex(int rgb, float alpha = 1f)
        {
            float r = ((rgb >> 16) & 0xFF) / 255f;
            float g = ((rgb >> 8) & 0xFF) / 255f;
            float b = (rgb & 0xFF) / 255f;
            return new Color(r, g, b, alpha);
        }

        public static Color WithAlpha(Color c, float a) { c.a = a; return c; }
    }
}
