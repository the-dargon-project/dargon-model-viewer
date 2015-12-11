using System.Collections.Generic;
using System.IO;
using SharpDX.Direct3D11;

namespace Dargon.Renderer {
   public class TextureCache {
      public TextureCache() {
         textures = new Dictionary<string, TextureAndSRV>();
      }

      public string BasePath;

      private class TextureAndSRV {
         public TextureAndSRV(Texture2D texture, ShaderResourceView srv) {
            Texture = texture;
            SRV = srv;
         }

         public Texture2D Texture;
         public ShaderResourceView SRV;
      }

      private Dictionary<string, TextureAndSRV> textures;  

      public ShaderResourceView GetSRV(Device device, string textureName) {
         var fullPath = Path.Combine(BasePath, textureName);
         if (textures.ContainsKey(fullPath)) {
            return textures[fullPath].SRV;
         }

         // Create new texture and SRV
         var texture = Resource.FromMemory<Texture2D>(device, File.ReadAllBytes(fullPath));
         var srv = new ShaderResourceView(device, texture);

         textures.Add(fullPath, new TextureAndSRV(texture, srv));

         return srv;
      }
   }
}
