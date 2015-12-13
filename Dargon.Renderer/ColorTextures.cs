using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dargon.Renderer.Properties;
using SharpDX.Direct3D11;

namespace Dargon.Renderer {
   public class ColorTextures {
      
      private Texture2D whiteTexture;
      public ShaderResourceView White { get; private set; }

      private Texture2D redTexture;
      public ShaderResourceView Red { get; private set; }

      private Texture2D yellowTexture;
      public ShaderResourceView Yellow { get; private set; }


      public void Initialize(Device device) {
         whiteTexture = Resource.FromMemory<Texture2D>(device, Resources.white);
         White = new ShaderResourceView(device, whiteTexture);

         redTexture = Resource.FromMemory<Texture2D>(device, Resources.red);
         Red = new ShaderResourceView(device, redTexture);

         yellowTexture = Resource.FromMemory<Texture2D>(device, Resources.yellow);
         Yellow = new ShaderResourceView(device, yellowTexture);
      }
   }
}
