using SharpDX.Direct3D11;

namespace Dargon.ModelViewer {
   public enum TextureAddressMode {
      Wrap,
      Border
   }

   public class Model {
      public int vertexBufferIndex;
      public int vertexOffset;
      public int vertexCount;

      public int indexBufferIndex;
      public int indexOffset;
      public int indexCount;

      public int textureIndex;
      public TextureAddressMode textureAddressMode;
   }

   internal class InternalModel {
      public int vertexBufferIndex;
      public int vertexOffset;
      public int vertexCount;

      public int indexBufferIndex;
      public int indexOffset;
      public int indexCount;

      public ShaderResourceView textureSRV;
      public TextureAddressMode textureAddressMode;
   }
}
