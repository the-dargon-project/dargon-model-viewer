using System.Collections.Generic;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Windows;
using Device = SharpDX.Direct3D11.Device;

namespace Dargon.ModelViewer {
   public partial class ModelViewer {
      public ModelViewer() {
         indexBuffers = new List<Buffer>();
         models = new List<Model>();
         vertexBuffers = new List<Buffer>();
      }

      private RenderForm form;

      private Device device;
      private SwapChain swapChain;
      private DeviceContext immediateContext;

      private Texture2D depthBuffer;
      private DepthStencilView depthStencilView;
      private RenderTargetView backbufferRTV;

      private VertexShader vertexShader;
      private PixelShader pixelShader;

      private Buffer vertexShaderPerFrameConstantBuffer;

      private InputLayout inputLayout;

      private List<Buffer> vertexBuffers;
      private List<Buffer> indexBuffers;

      private List<Model> models;

      private SamplerState textureSampler;

      private Matrix viewProj;
   }
}
