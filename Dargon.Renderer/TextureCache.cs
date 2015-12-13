using SharpDX.Direct3D11;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;

namespace Dargon.Renderer {
   public class TextureAndSRV {
      public TextureAndSRV(Texture2D texture, ShaderResourceView srv) {
         Texture = texture;
         SRV = srv;
      }

      public Texture2D Texture;
      public ShaderResourceView SRV;
   }

   public class TextureCache {
      public TextureCache(ColorTextures colorTextures) {
         this.colorTextures = colorTextures;
         textures = new ConcurrentDictionary<string, TextureAndSRV>();
      }

      public string BasePath;
      private Device device;

      private readonly ColorTextures colorTextures;

      private ConcurrentDictionary<string, TextureAndSRV> textures;


      public void Initialize(Device device) {
         this.device = device;
      }

      public ShaderResourceView GetSRV(string textureName) {
         TextureAndSRV returnValue;
         return textures.TryGetValue(textureName, out returnValue) ? returnValue.SRV : colorTextures.Red;
      }

      public void ReplaceTexture(string textureName, string newTexturePath) {
         ReplaceTextureAsync(textureName, newTexturePath).Wait();
      }

      public async Task ReplaceTextureAsync(string textureName, string newTexturePath) {
         await Task.Yield();
         try {
            using (var fs = new FileStream(newTexturePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous)) {
               var buffer = new byte[fs.Length];
               await fs.ReadAsync(buffer, 0, buffer.Length);

               var texture = Resource.FromMemory<Texture2D>(device, buffer);
               var srv = new ShaderResourceView(device, texture);
               var newValue = new TextureAndSRV(texture, srv);

               TextureAndSRV previousTextureAndSrv = null;
               textures.AddOrUpdate(
                  textureName,
                  add => newValue,
                  (key, existingValue) => {
                     previousTextureAndSrv = existingValue;
                     return newValue;
                  });

               if (previousTextureAndSrv != null) {
                  previousTextureAndSrv.Texture.Dispose();
                  previousTextureAndSrv.SRV.Dispose();
               }
            }
         } catch (Exception e) {
            Console.Error.WriteLine(e);
         }
      }
   }
}
