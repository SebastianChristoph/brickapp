    // Häufig genutzte Farben für Schnell-Auswahl
  
using System;
using System.Collections.Generic;

namespace Helpers
{
    public static class BrickColorHelper
    {
          public static readonly List<string> CommonColors = new() { "Blue", "Red", "Green" };
        // Mapping BrickLink-Farbnamen zu Hex/RGB (Auszug, erweiterbar)
        private static readonly Dictionary<string, string> BrickColorToHex = new(StringComparer.OrdinalIgnoreCase)
        {
            { "Aqua", "#B3D7D1" }, // rgb(179,215,209)
            { "Black", "#6e6e6eff" }, // rgb(33,33,33)
            { "Blue", "#214291" }, // rgb(33,66,145<w)
            { "Blue-Violet", "#5B3573" }, // rgb(91,53,115)
            { "Bright Green", "#4B974A" }, // rgb(75,151,74)
            { "Bright Light Blue", "#9FC3E9" }, // rgb(159,195,233)
            { "Bright Light Orange", "#F8BB3D" }, // rgb(248,187,61)
            { "Bright Light Yellow", "#FFF03A" }, // rgb(255,240,58)
            { "Bright Pink", "#FECCCF" }, // rgb(254,204,207)
            { "Brown", "#582A12" }, // rgb(88,42,18)
            { "Coral", "#FF698F" }, // rgb(255,105,143)
            { "Dark Azure", "#469BC6" }, // rgb(70,155,198)
            { "Dark Blue", "#0A3463" }, // rgb(10,52,99)
            { "Dark Bluish Gray", "#6D6E5C" }, // rgb(109,110,92)
            { "Dark Brown", "#352100" }, // rgb(53,33,0)
            { "Dark Gray", "#635F52" }, // rgb(99,95,82)
            { "Dark Green", "#184632" }, // rgb(24,70,50)
            { "Dark Orange", "#A95500" }, // rgb(169,85,0)
            { "Dark Pink", "#C870A0" }, // rgb(200,112,160)
            { "Dark Purple", "#3F3691" }, // rgb(63,54,145)
            { "Dark Red", "#720E0F" }, // rgb(114,14,15)
            { "Dark Tan", "#958A73" }, // rgb(149,138,115)
            { "Green", "#237841" }, // rgb(35,120,65)
            { "Light Bluish Gray", "#A0A5A9" }, // rgb(160,165,169)
            { "Light Gray", "#E5E4DE" }, // rgb(229,228,222)
            { "Light Pink", "#FECCCF" }, // rgb(254,204,207)
            { "Lime", "#C7D23C" }, // rgb(199,210,60)
            { "Magenta", "#923978" }, // rgb(146,57,120)
            { "Medium Azure", "#68C3E2" }, // rgb(104,195,226)
            { "Medium Blue", "#5A93DB" }, // rgb(90,147,219)
            { "Medium Lavender", "#AC78BA" }, // rgb(172,120,186)
            { "Medium Nougat", "#AA7D55" }, // rgb(170,125,85)
            { "Orange", "#FE8A18" }, // rgb(254,138,24)
            { "Pink", "#FC97AC" }, // rgb(252,151,172)
            { "Purple", "#A06EBB" }, // rgb(160,110,187)
            { "Red", "#C91A09" }, // rgb(201,26,9)
            { "Reddish Brown", "#582A12" }, // rgb(88,42,18)
            { "Sand Blue", "#6074A1" }, // rgb(96,116,161)
            { "Sand Green", "#A4BD95" }, // rgb(164,189,149)
            { "Tan", "#E4CD9E" }, // rgb(228,205,158)
            { "Trans-Clear", "#FCFCFC" }, // rgb(252,252,252)
            { "Trans-Red", "#C91A09" }, // rgb(201,26,9)
            { "White", "#FFFFFF" }, // rgb(255,255,255)
            { "Yellow", "#F2CD37" }, // rgb(242,205,55)
            // ... weitere Farben nach Bedarf
        };

        public static string? GetHexForColor(string? colorName)
        {
            if (string.IsNullOrWhiteSpace(colorName)) return null;
            return BrickColorToHex.TryGetValue(colorName, out var hex) ? hex : null;
        }
    }
}
