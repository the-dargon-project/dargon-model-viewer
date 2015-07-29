using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX.Direct3D11;
using Buffer = SharpDX.Direct3D11.Buffer;

namespace Dargon.Renderer {
   public enum TextureAddressMode {
      Wrap,
      Border
   }

   public class Model {
      public Buffer vertexBuffer { get; set; }
      public int vertexOffset { get; set; }
      public int vertexCount { get; set; }
      public Buffer indexBuffer { get; set; }
      public int indexOffset { get; set; }
      public int indexCount { get; set; }
      public ShaderResourceView textureSRV { get; set; }
      public TextureAddressMode textureAddressMode { get; set; }
   }
}
