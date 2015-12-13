using SharpDX.Direct3D11;
using System.Collections.Concurrent;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Color = SharpDX.Color;
using SDColor = System.Drawing.Color;
using Device = SharpDX.Direct3D11.Device;
using PixelFormat = System.Drawing.Imaging.PixelFormat;
using Resource = SharpDX.Direct3D11.Resource;

namespace Dargon.Renderer {
   public class ColorTextures {
      private readonly ConcurrentDictionary<Color, Texture2D> texturesByColor = new ConcurrentDictionary<Color, Texture2D>();
      private readonly ConcurrentDictionary<Color, ShaderResourceView> texturesViewsByColor = new ConcurrentDictionary<Color, ShaderResourceView>();
      private Device device;

      public void Initialize(Device device) {
         this.device = device;
      }

      public Texture2D GetTextureOfColor(Color color) {
         return texturesByColor.GetOrAdd(
            color,
            add => CreateColoredTexture(color));
      }

      public ShaderResourceView GetTextureViewOfColor(Color color) {
         return texturesViewsByColor.GetOrAdd(
            color,
            add => {
               var texture = GetTextureOfColor(color);
               return new ShaderResourceView(device, texture);
            });
      }

      public Texture2D CreateColoredTexture(Color color) {
         using (var bitmap = new Bitmap(1, 1, PixelFormat.Format32bppArgb)) {
            using (var g = Graphics.FromImage(bitmap)) {
               g.Clear(SDColor.FromArgb(color.A, color.R, color.G, color.B));
            }
            using (var ms = new MemoryStream()) {
               bitmap.Save(ms, ImageFormat.Png);
               ms.Position = 0;
               return Resource.FromMemory<Texture2D>(device, ms.GetBuffer());
            }
         }
      }
   }
}
