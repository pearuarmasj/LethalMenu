using Newtonsoft.Json;
using System;
using System.Globalization;
using UnityEngine;

namespace LethalMenu.Util
{
    public class RGBAColor
    {
        public float r, g, b, a;

        public static RGBAColor Default = new RGBAColor(1f, 1f, 1f, 1f);

        public RGBAColor(int r, int g, int b, float a = 1f)
        {
            this.r = r / 255f;
            this.g = g / 255f;
            this.b = b / 255f;
            this.a = a;
        }

        public RGBAColor(string hex)
        {
            if (string.IsNullOrWhiteSpace(hex))
                hex = "FFFFFFFF";

            hex = hex.Trim().TrimStart('#');
            if (hex.Length == 6)
                hex += "FF";

            if (hex.Length != 8)
                throw new ArgumentException("Hex color must be RRGGBB or RRGGBBAA.", nameof(hex));

            r = int.Parse(hex.Substring(0, 2), NumberStyles.HexNumber) / 255f;
            g = int.Parse(hex.Substring(2, 2), NumberStyles.HexNumber) / 255f;
            b = int.Parse(hex.Substring(4, 2), NumberStyles.HexNumber) / 255f;
            a = int.Parse(hex.Substring(6, 2), NumberStyles.HexNumber) / 255f;
        }

        [JsonConstructor]
        public RGBAColor(float r, float g, float b, float a)
        {
            this.r = r; this.g = g; this.b = b; this.a = a;
        }

        public Color GetColor() => new Color(r, g, b, a);

        public string GetHexCode()
        {
            int red = Mathf.Clamp((int)(r * 255), 0, 255);
            int green = Mathf.Clamp((int)(g * 255), 0, 255);
            int blue = Mathf.Clamp((int)(b * 255), 0, 255);
            return ((red << 16) | (green << 8) | blue).ToString("X6");
        }

        public string GetHexCodeAlpha()
        {
            int alpha = Mathf.Clamp((int)(a * 255), 0, 255);
            return $"{GetHexCode()}{alpha:X2}";
        }
    }
}
