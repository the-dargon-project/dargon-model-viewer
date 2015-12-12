using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using Dargon.Renderer.Properties;
using Dargon.Scene.Api;
using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using Buffer = SharpDX.Direct3D11.Buffer;
using Device = SharpDX.Direct3D11.Device;

namespace Dargon.Renderer {
   public class Renderer {
      public Renderer(IWin32Window form, TextureCache textureCache) {
         this.textureCache = textureCache;
         sceneElements = new List<SceneElement>();

         // SwapChain description
         var desc = new SwapChainDescription() {
            BufferCount = 1,
            ModeDescription = new ModeDescription(100, 100, new Rational(60, 1), Format.R8G8B8A8_UNorm),
            IsWindowed = true,
            OutputHandle = form.Handle,
            SampleDescription = new SampleDescription(1, 0),
            SwapEffect = SwapEffect.Discard,
            Usage = Usage.RenderTargetOutput
         };

         #if DEBUG
            var flag = DeviceCreationFlags.Debug;
         #else
            var flag = DeviceCreationFlags.None;
         #endif

         // Create Device and SwapChain
         Device.CreateWithSwapChain(DriverType.Hardware, flag | DeviceCreationFlags.BgraSupport, desc, out device, out swapChain);
         immediateContext = device.ImmediateContext;
         textureCache.device = device;

         // Load the Vertex and Pixel shaders
         ShaderBytecode vertexShaderByteCode;
         using (var stream = new MemoryStream(Resources.vs)) {
            vertexShaderByteCode = ShaderBytecode.FromStream(stream);
            vertexShader = new VertexShader(device, vertexShaderByteCode);
         }
         immediateContext.VertexShader.Set(vertexShader);

         ShaderBytecode pixelShaderByteCode;
         using (var stream = new MemoryStream(Resources.ps)) {
            pixelShaderByteCode = ShaderBytecode.FromStream(stream);
            pixelShader = new PixelShader(device, pixelShaderByteCode);
         }
         immediateContext.PixelShader.Set(pixelShader);

         // Create the input layout for the COMPLEX vertex
         inputLayout = new InputLayout(device, ShaderSignature.GetInputSignature(vertexShaderByteCode), new[] {
            new InputElement("POSITION", 0, Format.R32G32B32A32_Float, 0, 0),
            new InputElement("TEXCOORD", 0, Format.R32G32_Float, 16, 0)
         });
         immediateContext.InputAssembler.InputLayout = inputLayout;
         immediateContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;

         // Release the bytecode
         vertexShaderByteCode.Dispose();
         pixelShaderByteCode.Dispose();

         // Create Constant Buffer
         vertexShaderPerFrameConstantBuffer = new Buffer(device, Utilities.SizeOf<Matrix>(), ResourceUsage.Default, BindFlags.ConstantBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);
         immediateContext.VertexShader.SetConstantBuffer(0, vertexShaderPerFrameConstantBuffer);

         // Create the default texture sampler
         textureSamplerWrap = new SamplerState(device, new SamplerStateDescription {
            Filter = Filter.MinMagMipPoint,
            AddressU = TextureAddressMode.Wrap,
            AddressV = TextureAddressMode.Wrap,
            AddressW = TextureAddressMode.Wrap,
            BorderColor = new Color4(0.0f, 0.0f, 0.0f, 0.0f),
            ComparisonFunction = Comparison.Always,
            MaximumAnisotropy = 16,
            MipLodBias = 0,
            MinimumLod = 0,
            MaximumLod = 3.402823466e+38f//D3D11_FLOAT32_MAX
         });

         textureSamplerBorder = new SamplerState(device, new SamplerStateDescription {
            Filter = Filter.MinMagMipPoint,
            AddressU = TextureAddressMode.Border,
            AddressV = TextureAddressMode.Border,
            AddressW = TextureAddressMode.Border,
            BorderColor = new Color4(0.0f, 0.0f, 0.0f, 0.0f),
            ComparisonFunction = Comparison.Always,
            MaximumAnisotropy = 16,
            MipLodBias = 0,
            MinimumLod = 0,
            MaximumLod = 3.402823466e+38f//D3D11_FLOAT32_MAX
         });

         // Prepare the camera
         Camera = new Camera();

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
         immediateContext.Rasterizer.State = rasterizerState;
         immediateContext.OutputMerger.SetDepthStencilState(depthStencilState);
         immediateContext.OutputMerger.SetBlendState(blendState);
         immediateContext.PixelShader.SetSampler(0, textureSamplerWrap);
      }


      private Device device;
      private SwapChain swapChain;
      private DeviceContext immediateContext;

      private Texture2D depthBuffer;
      private DepthStencilView depthStencilView;
      private RenderTargetView backbufferRTV;

      private DepthStencilState depthStencilState;
      private RasterizerState rasterizerState;
      private BlendState blendState;

      private InputLayout inputLayout;
      private VertexShader vertexShader;
      private PixelShader pixelShader;

      private Buffer vertexShaderPerFrameConstantBuffer;

      private SamplerState textureSamplerWrap;
      private SamplerState textureSamplerBorder;

      public Camera Camera;

      private TextureCache textureCache;

      private class SceneElement {
         public SceneElement(RenderMesh mesh, Matrix transform) {
            Mesh = mesh;
            Transform = transform;
         }

         public RenderMesh Mesh;
         public Matrix Transform;
      }
      private List<SceneElement> sceneElements;


      public void AddMeshToScene(Mesh mesh, Dargon.Scene.Api.Util.Vector3 position) {
         sceneElements.Add(new SceneElement(new RenderMesh(device, mesh), Matrix.Translation(position.X, position.Y, position.Z)));
      }

      public void ResizeBackbuffer(IntPtr newBackbuffer) {
         // Unbind the old RTV and DSV from the pipeline by binding null
         // Then Dispose them
         immediateContext.OutputMerger.SetRenderTargets(null, renderTargetView:null);
         depthStencilView?.Dispose();
         depthBuffer?.Dispose();
         backbufferRTV?.Dispose();

         // Query to get to the DX11 version of the backbuffer
         // (The backbuffer is shared between DX9 and DX11)
         var surface = CppObject.FromPointer<Surface>(newBackbuffer);
         var resourceDXGI = surface.QueryInterface<SharpDX.DXGI.Resource>();
         var sharedHandle = resourceDXGI.SharedHandle;
         resourceDXGI.Dispose();

         var resourceDX11 = device.OpenSharedResource<SharpDX.Direct3D11.Resource>(sharedHandle);
         var texture2D = resourceDX11.QueryInterface<Texture2D>();
         resourceDX11.Dispose();

         // Create a new RTV
         var rtvDesc = new RenderTargetViewDescription {
            Format = Format.B8G8R8A8_UNorm,
            Dimension = RenderTargetViewDimension.Texture2D,
            Texture2D = { MipSlice = 0 }
         };
         backbufferRTV = new RenderTargetView(device, texture2D, rtvDesc);
         

         // Create the new Depth buffer and bind it
         var texture2DDesc = texture2D.Description;
         immediateContext.Rasterizer.SetViewport(new Viewport(0, 0, texture2DDesc.Width, texture2DDesc.Height, 0.0f, 1.0f));
         depthBuffer = new Texture2D(device, new Texture2DDescription {
            Format = Format.D32_Float,
            ArraySize = 1,
            MipLevels = 1,
            Width = texture2DDesc.Width,
            Height = texture2DDesc.Height,
            SampleDescription = new SampleDescription(1, 0),
            Usage = ResourceUsage.Default,
            BindFlags = BindFlags.DepthStencil,
            CpuAccessFlags = CpuAccessFlags.None,
            OptionFlags = ResourceOptionFlags.None
         });
         depthStencilView = new DepthStencilView(this.device, this.depthBuffer);
         immediateContext.OutputMerger.SetRenderTargets(depthStencilView, backbufferRTV);

         // Set up the viewport and projection matrix
         immediateContext.Rasterizer.SetViewport(0.0f, 0.0f, texture2DDesc.Width, texture2DDesc.Height);
         Camera.UpdateProjectionMatrix(texture2DDesc.Width, texture2DDesc.Height, 100000.0f, 1.0f);
      }

      public void Render() {
         // Clear views
         immediateContext.ClearDepthStencilView(depthStencilView, DepthStencilClearFlags.Depth, 0.0f, 0);
         immediateContext.ClearRenderTargetView(backbufferRTV, Color.LightGray);
         immediateContext.OutputMerger.SetRenderTargets(depthStencilView, backbufferRTV);

         // Update WorldViewProj Matrix
         var view = Camera.GetView();
         var proj = Camera.GetProj();
         var viewProj = Matrix.Multiply(view, proj);

         foreach (var sceneElement in sceneElements) {
            var worldViewProj = sceneElement.Transform * viewProj;
            worldViewProj.Transpose();
            immediateContext.UpdateSubresource(ref worldViewProj, vertexShaderPerFrameConstantBuffer);

            immediateContext.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(sceneElement.Mesh.VertexBuffer, 24, 0));
            immediateContext.InputAssembler.SetIndexBuffer(sceneElement.Mesh.IndexBuffer, Format.R16_UInt, 0);

            var textureSRV = textureCache.GetSRV(sceneElement.Mesh.TexturePath);
            immediateContext.PixelShader.SetShaderResource(0, textureSRV);

            immediateContext.DrawIndexed(sceneElement.Mesh.IndexCount, 0, 0);
         }

         immediateContext.Flush();
         swapChain.Present(0, PresentFlags.None);
      }
   }
}
