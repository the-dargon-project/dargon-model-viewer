﻿using SharpDX.Direct3D11;

namespace Dargon.ModelViewer {
   public class Model {
      public int vertexBufferIndex;
      public int vertexOffset;
      public int vertexCount;

      public int indexBufferIndex;
      public int indexOffset;
      public int indexCount;

      public int textureIndex;
   }

   internal class InternalModel {
      public int vertexBufferIndex;
      public int vertexOffset;
      public int vertexCount;

      public int indexBufferIndex;
      public int indexOffset;
      public int indexCount;

      public ShaderResourceView textureSRV;
   }
}