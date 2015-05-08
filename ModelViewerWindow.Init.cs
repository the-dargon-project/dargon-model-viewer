using System;
using System.Collections.Generic;
using System.IO;
using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Windows;
using Buffer = SharpDX.Direct3D11.Buffer;
using Device = SharpDX.Direct3D11.Device;
using Resource = SharpDX.Direct3D11.Resource;

namespace Dargon.ModelViewer {
   public partial class ModelViewerWindow {
      public void Initialize(int clientWidth, int clientHeight, 
                             float cameraTheta, float cameraPhi, float cameraRadius, 
                             float cameraTargetX, float cameraTargetY, float cameraTargetZ,
                             float cameraPanScale, float cameraScrollScale) {
         form = new RenderForm("The Dargon Project Model Viewer") {
            Width = clientWidth,
            Height = clientHeight
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
         using (var stream = new MemoryStream(Properties.Resources.vs)) {
            vertexShaderByteCode = ShaderBytecode.FromStream(stream);
            vertexShader = new VertexShader(device, vertexShaderByteCode);
         }

         ShaderBytecode pixelShaderByteCode;
         using (var stream = new MemoryStream(Properties.Resources.ps)) {
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
         camera = new Camera(cameraTheta, cameraPhi, cameraRadius, cameraScrollScale * 10.0f, new Vector3(cameraTargetX, cameraTargetY, cameraTargetZ));
         camera.UpdateProjectionMatrix(clientWidth, clientHeight, 100000.0f, 0.1f);
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
      }

      public void LoadModels(List<float[]> vertexBuffers_, List<ushort[]> indexBuffers_, List<byte[]> textures_, List<Model> models_) {
         vertexBuffers_.ForEach(x => vertexBuffers.Add(Buffer.Create(device, BindFlags.VertexBuffer, x)));
         indexBuffers_.ForEach(x => indexBuffers.Add(Buffer.Create(device, BindFlags.IndexBuffer, x)));
         textures_.ForEach(x => textures.Add(Resource.FromMemory<Texture2D>(device, x)));
         models_.ForEach(x => models.Add(new InternalModel {
                                            vertexBufferIndex = x.vertexBufferIndex,
                                            vertexOffset = x.vertexOffset,
                                            vertexCount = x.vertexCount,
                                            indexBufferIndex = x.indexBufferIndex,
                                            indexOffset = x.indexOffset,
                                            indexCount = x.indexCount,
                                            textureSRV = new ShaderResourceView(device, textures[x.textureIndex]),
                                            textureAddressMode = x.textureAddressMode
                                         }));
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
