using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Dargon.League.WGEO;
using Dargon.Renderer.Properties;
using Dargon.Scene.Api;
using ItzWarty;
using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Windows;
using Color = SharpDX.Color;
using Device = SharpDX.Direct3D11.Device;
using Point = System.Drawing.Point;
using Resource = SharpDX.Direct3D11.Resource;

namespace Dargon.Renderer {
   public class RenderWindow {
      public RenderWindow() {
         vertexBuffers = new List<Buffer>();
         indexBuffers = new List<Buffer>();
         textures = new List<Texture2D>();
         mouseLastLocation = new Point();
      }

      private RenderForm form;

      private Device device;
      private SwapChain swapChain;
      private DeviceContext immediateContext;

      private Texture2D depthBuffer;
      private DepthStencilView depthStencilView;
      private RenderTargetView backbufferRTV;

      private DepthStencilState depthStencilState;
      private RasterizerState rasterizerState;
      private BlendState blendState;

      private VertexShader vertexShader;
      private PixelShader pixelShader;

      private Buffer vertexShaderPerFrameConstantBuffer;

      private InputLayout inputLayout;

      private List<Buffer> vertexBuffers;
      private List<Buffer> indexBuffers;

      private List<Texture2D> textures;

      private SamplerState textureSamplerWrap;
      private SamplerState textureSamplerBorder;

      private Camera camera;
      private float cameraPanScale;
      private float cameraScrollScale;
      private Point mouseLastLocation;


      private List<Model> sceneElementsInternalView;


      public void Initialize(Size clientSize, Camera camera, float cameraPanScale, float cameraScrollScale, List<SceneElement> sceneElements) {
         form = new RenderForm("The Dargon Project Model Viewer") {
            ClientSize = clientSize
         };

         form.MouseDown += MouseDown;
         form.MouseUp += MouseUp;
         form.MouseMove += MouseMove;
         form.MouseWheel += MouseWheel;

         // SwapChain description
         var desc = new SwapChainDescription {
            BufferCount = 1,
            ModeDescription = new ModeDescription(form.ClientSize.Width, form.ClientSize.Height,
               new Rational(60, 1), Format.R8G8B8A8_UNorm),
            IsWindowed = true,
            OutputHandle = form.Handle,
            SampleDescription = new SampleDescription(1, 0),
            SwapEffect = SwapEffect.Discard,
            Usage = Usage.RenderTargetOutput
         };


         // Create Device and SwapChain
         Device.CreateWithSwapChain(DriverType.Hardware, DeviceCreationFlags.Debug, desc, out device, out swapChain);
         immediateContext = device.ImmediateContext;

         // Ignore all windows events
         var factory = swapChain.GetParent<Factory>();
         factory.MakeWindowAssociation(form.Handle, WindowAssociationFlags.IgnoreAll);
         factory.Dispose();

         // Create a RenderTargetView from the backbuffer
         var backBuffer = Resource.FromSwapChain<Texture2D>(swapChain, 0);
         backbufferRTV = new RenderTargetView(device, backBuffer);
         backBuffer.Dispose();

         // Load the Vertex and Pixel shaders
         ShaderBytecode vertexShaderByteCode;
         using (var stream = new MemoryStream(Resources.vs)) {
            vertexShaderByteCode = ShaderBytecode.FromStream(stream);
            vertexShader = new VertexShader(device, vertexShaderByteCode);
         }

         ShaderBytecode pixelShaderByteCode;
         using (var stream = new MemoryStream(Resources.ps)) {
            pixelShaderByteCode = ShaderBytecode.FromStream(stream);
            pixelShader = new PixelShader(device, pixelShaderByteCode);
         }

         // Create the input layout for the COMPLEX vertex
         inputLayout = new InputLayout(device, ShaderSignature.GetInputSignature(vertexShaderByteCode), new[] {
            new InputElement("POSITION", 0, Format.R32G32B32A32_Float, 0, 0),
            new InputElement("NORMAL", 0, Format.R32G32B32A32_Float, 16, 0),
            new InputElement("TEXCOORD", 0, Format.R32G32_Float, 32, 0)
         });

         // Release the bytecode
         vertexShaderByteCode.Dispose();
         pixelShaderByteCode.Dispose();

         // Create Constant Buffer
         vertexShaderPerFrameConstantBuffer = new Buffer(device, Utilities.SizeOf<Matrix>(), ResourceUsage.Default, BindFlags.ConstantBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);

         // Create Depth Buffer & View
         depthBuffer = new Texture2D(device, new Texture2DDescription {
            Format = Format.D32_Float,
            ArraySize = 1,
            MipLevels = 1,
            Width = form.ClientSize.Width,
            Height = form.ClientSize.Height,
            SampleDescription = new SampleDescription(1, 0),
            Usage = ResourceUsage.Default,
            BindFlags = BindFlags.DepthStencil,
            CpuAccessFlags = CpuAccessFlags.None,
            OptionFlags = ResourceOptionFlags.None
         });

         depthStencilView = new DepthStencilView(device, depthBuffer);

         // Create the default texture sampler
         textureSamplerWrap = new SamplerState(device, new SamplerStateDescription {
            Filter = Filter.MinMagMipPoint,
            AddressU = SharpDX.Direct3D11.TextureAddressMode.Wrap,
            AddressV = SharpDX.Direct3D11.TextureAddressMode.Wrap,
            AddressW = SharpDX.Direct3D11.TextureAddressMode.Wrap,
            BorderColor = new Color4(0.0f, 0.0f, 0.0f, 0.0f),
            ComparisonFunction = Comparison.Always,
            MaximumAnisotropy = 16,
            MipLodBias = 0,
            MinimumLod = 0,
            MaximumLod = 3.402823466e+38f//D3D11_FLOAT32_MAX
         });

         textureSamplerBorder = new SamplerState(device, new SamplerStateDescription {
            Filter = Filter.MinMagMipPoint,
            AddressU = SharpDX.Direct3D11.TextureAddressMode.Border,
            AddressV = SharpDX.Direct3D11.TextureAddressMode.Border,
            AddressW = SharpDX.Direct3D11.TextureAddressMode.Border,
            BorderColor = new Color4(0.0f, 0.0f, 0.0f, 0.0f),
            ComparisonFunction = Comparison.Always,
            MaximumAnisotropy = 16,
            MipLodBias = 0,
            MinimumLod = 0,
            MaximumLod = 3.402823466e+38f//D3D11_FLOAT32_MAX
         });

         // Prepare the camera
         this.camera = camera;
         camera.UpdateProjectionMatrix(clientSize.Width, clientSize.Height, 100000.0f, 10.0f);
         this.cameraPanScale = cameraPanScale;
         this.cameraScrollScale = cameraScrollScale;

         // Create the depth stencil state
         depthStencilState = new DepthStencilState(device, new DepthStencilStateDescription {
            IsDepthEnabled = true,
            DepthWriteMask = DepthWriteMask.All,
            DepthComparison = Comparison.GreaterEqual,
            IsStencilEnabled = false,
            StencilReadMask = 0xff, //D3D11_DEFAULT_STENCIL_READ_MASK
            StencilWriteMask = 0xff, //D3D11_DEFAULT_STENCIL_WRITE_MASK
            FrontFace = new DepthStencilOperationDescription {
               DepthFailOperation = StencilOperation.Keep,
               FailOperation = StencilOperation.Keep,
               PassOperation = StencilOperation.Replace,
               Comparison = Comparison.Always
            },
            BackFace = new DepthStencilOperationDescription {
               DepthFailOperation = StencilOperation.Keep,
               FailOperation = StencilOperation.Keep,
               PassOperation = StencilOperation.Replace,
               Comparison = Comparison.Always
            }
         });

         // Create the raster state
         rasterizerState = new RasterizerState(device, new RasterizerStateDescription {
            IsAntialiasedLineEnabled = false,
            CullMode = CullMode.Back,
            DepthBias = 0,
            DepthBiasClamp = 0.0f,
            IsDepthClipEnabled = true,
            FillMode = FillMode.Solid,
            IsFrontCounterClockwise = false,
            IsMultisampleEnabled = true,
            IsScissorEnabled = false,
            SlopeScaledDepthBias = 0
         });

         // Create the blend state
         var blendDesc = new BlendStateDescription {
            AlphaToCoverageEnable = false,
            IndependentBlendEnable = false
         };

         for (var i = 0; i < 8; ++i) {
            blendDesc.RenderTarget[i].IsBlendEnabled = true;
            blendDesc.RenderTarget[i].BlendOperation = BlendOperation.Add;
            blendDesc.RenderTarget[i].AlphaBlendOperation = BlendOperation.Add;
            blendDesc.RenderTarget[i].DestinationBlend = BlendOption.InverseSourceAlpha;
            blendDesc.RenderTarget[i].DestinationAlphaBlend = BlendOption.One;
            blendDesc.RenderTarget[i].RenderTargetWriteMask = ColorWriteMaskFlags.All;
            blendDesc.RenderTarget[i].SourceBlend = BlendOption.SourceAlpha;
            blendDesc.RenderTarget[i].SourceAlphaBlend = BlendOption.One;
         }

         blendState = new BlendState(device, blendDesc);

         // Prepare the stages that don't change per frame
         immediateContext.InputAssembler.InputLayout = inputLayout;
         immediateContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
         immediateContext.VertexShader.Set(vertexShader);
         immediateContext.PixelShader.Set(pixelShader);
         immediateContext.Rasterizer.SetViewport(new Viewport(0, 0, form.ClientSize.Width, form.ClientSize.Height, 0.0f, 1.0f));
         immediateContext.Rasterizer.State = rasterizerState;
         immediateContext.OutputMerger.SetTargets(depthStencilView, backbufferRTV);
         immediateContext.OutputMerger.SetDepthStencilState(depthStencilState);
         immediateContext.OutputMerger.SetBlendState(blendState);









         foreach (var sceneElement in sceneElements) {
            switch (sceneElement.GetType().Name) {
               case nameof(CharacterElement):

                  break;
               case nameof(MapElement):
                  WGEOFile wgeoFile;
                  using (var fs = new FileStream(@"C:\DargonDumpNew\LEVELS\map11\Scene\room.wgeo", FileMode.Open)) {
                     var readerFactory = new WGEOReaderFactory();
                     wgeoFile = readerFactory.ReadWGEOFile(fs);
                  }

                  var vertexBuffers_ = new List<float[]>();
                  var indexBuffers_ = new List<ushort[]>();
                  var textureDictionary_ = new Dictionary<string, int>();
                  var textures_ = new List<byte[]>();

                  wgeoFile.models.ForEach(model => {
                     var newVertexBuffer = model.vertices.SelectMany(vert => new[] { vert.position.x, vert.position.y, vert.position.z, 1.0f, 0.0f, 0.0f, 0.0f, 1.0f, vert.uv.x, vert.uv.y }).ToArray();
                     var newIndexBuffer = model.indices;

                     vertexBuffers_.Add(newVertexBuffer);
                     indexBuffers_.Add(newIndexBuffer);

                     if (textureDictionary_.ContainsKey(model.textureName))
                        return;



                     var data = File.ReadAllBytes(@"C:\DargonDumpNew\LEVELS\map11\Scene\Textures\" + model.textureName.Trim('\0'));

                     textureDictionary_.Add(model.textureName, textures_.Count);
                     textures_.Add(data);
                  });

                  vertexBuffers_.ForEach(x => { vertexBuffers.Add(Buffer.Create(device, BindFlags.VertexBuffer, x)); });
                  indexBuffers_.ForEach(x => { indexBuffers.Add(Buffer.Create(device, BindFlags.IndexBuffer, x)); });
                  textures_.ForEach(x => textures.Add(Resource.FromMemory<Texture2D>(device, x)));

                  sceneElementsInternalView = Util.Generate(wgeoFile.models.Count, i => new Model {
                     vertexBuffer = vertexBuffers[i],
                     vertexOffset = 0,
                     vertexCount = vertexBuffers_[i].Length,
                     indexBuffer = indexBuffers[i],
                     indexOffset = 0,
                     indexCount = indexBuffers_[i].Length,
                     textureSRV = new ShaderResourceView(device, textures[textureDictionary_[wgeoFile.models[i].textureName]]),
                     textureAddressMode = TextureAddressMode.Wrap
                  }).ToList();

                  break;
            }
         }
      }


      public void Run() {
         // Bind our Run() function to the delegate of the RenderForm
         RenderLoop.Run(form, RunInternal);
      }

      private void RunInternal() {
         // Clear views
         immediateContext.ClearDepthStencilView(depthStencilView, DepthStencilClearFlags.Depth, 0.0f, 0);
         immediateContext.ClearRenderTargetView(backbufferRTV, Color.LightGray);

         // Update WorldViewProj Matrix
         var worldViewProj = camera.GetView() * camera.GetProj();
         worldViewProj.Transpose();
         immediateContext.UpdateSubresource(ref worldViewProj, vertexShaderPerFrameConstantBuffer);
         immediateContext.VertexShader.SetConstantBuffer(0, vertexShaderPerFrameConstantBuffer);

         foreach (var model in sceneElementsInternalView) {
            immediateContext.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(model.vertexBuffer, 40, 0));
            immediateContext.InputAssembler.SetIndexBuffer(model.indexBuffer, Format.R16_UInt, 0);
            immediateContext.PixelShader.SetShaderResource(0, model.textureSRV);
            immediateContext.PixelShader.SetSampler(0, model.textureAddressMode == TextureAddressMode.Border ? textureSamplerBorder : textureSamplerWrap);

            immediateContext.DrawIndexed(model.indexCount, model.indexOffset, model.vertexOffset);
         }

         // Present
         swapChain.Present(1, PresentFlags.None);
      }

      private void MouseDown(object sender, MouseEventArgs e) {
         mouseLastLocation = e.Location;
         form.Capture = true;
      }

      private void MouseUp(object sender, MouseEventArgs e) {
         form.Capture = false;
      }

      private void MouseMove(object sender, MouseEventArgs e) {
         if (form.Capture && e.Button == MouseButtons.Left) {
            if (Control.ModifierKeys == Keys.Alt) {
               // Calculate the new phi and theta based on mouse position relative to where the user clicked
               var dPhi = ((float)(mouseLastLocation.Y - e.Y) / 300);
               var dTheta = ((float)(mouseLastLocation.X - e.X) / 300);

               camera.Rotate(-dTheta, dPhi);
            }
         } else if (form.Capture && e.Button == System.Windows.Forms.MouseButtons.Middle) {
            if (Control.ModifierKeys == Keys.Alt) {
               var dx = ((float)(mouseLastLocation.X - e.X));
               var dy = ((float)(mouseLastLocation.Y - e.Y));

               camera.Pan(-dx * cameraPanScale, dy * cameraPanScale);
            }
         }

         mouseLastLocation = e.Location;
      }

      private void MouseWheel(object sender, MouseEventArgs e) {
         // Make each wheel dedent correspond to a size based on the scene
         camera.Zoom(e.Delta * cameraScrollScale);
      }

      public void Shutdown() {
         // Release all resources
         vertexShader.Dispose();
         pixelShader.Dispose();
         inputLayout.Dispose();
         backbufferRTV.Dispose();
         immediateContext.ClearState();
         immediateContext.Flush();
         device.Dispose();
         immediateContext.Dispose();
         swapChain.Dispose();
      }
   }
}
