using System.Collections.Generic;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Windows;
using Device = SharpDX.Direct3D11.Device;

namespace Dargon.ModelViewer {
   public partial class ModelViewerWindow {
      public ModelViewerWindow() {
         vertexBuffers = new List<Buffer>();
         indexBuffers = new List<Buffer>();
         textures = new List<Texture2D>();
         models = new List<InternalModel>();
         mouseLastLocation = new Point();
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

      private List<Texture2D> textures;
      private List<InternalModel> models;

      private SamplerState textureSampler;

      private Camera camera;
      private float cameraPanScale;
      private float cameraScrollScale;
      private Point mouseLastLocation;
   }
}
