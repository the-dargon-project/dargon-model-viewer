using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Runtime;
using Dargon.Renderer.Properties;
using SharpDX.Direct3D11;

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
         var texture = Resource.FromFile<Texture2D>(device, newTexturePath);
         var srv = new ShaderResourceView(device, texture);
         var newValue = new TextureAndSRV(texture, srv);

         textures.AddOrUpdate(textureName, newValue,
            (key, existingValue) => {
               existingValue.Texture.Dispose();
               existingValue.SRV.Dispose();

               return newValue;
            });
      }
   }
}
