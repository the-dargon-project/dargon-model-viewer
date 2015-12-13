using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ItzWarty;
using SharpDX;

namespace Dargon.Renderer {
   public static class WireframeColors {
      private static readonly Color[] wireColors =
         new[] {
            Color.Cyan,
            Color.Lime,
            Color.Magenta,
            Color.Orange,
            Color.Red,
            Color.Yellow
         }.Concat(
            Generate6BitColors()
               .Distinct()
               .Where(FilterBrightness)
               .OrderByDescending(ComputeRating)).Distinct().ToArray();
      
      private static Color[] Generate6BitColors() {
         var results = new Color[(int)Math.Pow(2, 6)];
         int i = 0;
         for (var b = 0; b < 4; b++) {
         for (var r = 0; r < 4; r++)
         for (var g = 0; g < 4; g++)
            results[i++] = new Color(r * 85, g * 85, b * 85);
         }
         return results;
      }

      private static bool FilterBrightness(Color arg) {
         var brightness = (arg.R * 0.333 + arg.G * 0.333 + arg.B * 0.333) / 255.0;
         return brightness < 0.8;
      }

      private static int ComputeRating(Color arg) {
         var components = new[] { arg.R, arg.G, arg.B }.OrderByDescending(x => x).ToArray();
         var max = components[0];
         var middle = components[1];
         var min = components[2];
         return (max - min) * (Math.Max(max, middle) - min);// * (max - average);
      }

      public static Color GetColor(int i, bool isHighlighted) {
         var color = wireColors[i % wireColors.Length];
         if (!isHighlighted) {
            color = new Color(color.R, color.G, color.B , color.A / 4);
         }
         return color;
      }
   }
}
