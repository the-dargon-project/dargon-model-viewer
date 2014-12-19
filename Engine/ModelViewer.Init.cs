using System;
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
   public partial class ModelViewer {
      public void Initialize() {
         form = new RenderForm("The Dargon Project Model Viewer");

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
         var backBuffer = Texture2D.FromSwapChain<Texture2D>(swapChain, 0);
         backbufferRTV = new RenderTargetView(device, backBuffer);
         backBuffer.Dispose();

         // Load the Vertex and Pixel shaders
         var vertexShaderByteCode = ShaderBytecode.FromFile("vs.cso");
         vertexShader = new VertexShader(device, vertexShaderByteCode);

         var pixelShaderByteCode = ShaderBytecode.FromFile("ps.cso");
         pixelShader = new PixelShader(device, pixelShaderByteCode);

         // Create the input layout for the COMPLEX vertex
         inputLayout = new InputLayout(device, ShaderSignature.GetInputSignature(vertexShaderByteCode), new[] {
         new InputElement("POSITION", 0, Format.R32G32B32A32_Float, 0, 0),
         new InputElement("TEXCOORD", 0, Format.R32G32_Float, 16, 0)
         });

         // Release the bytecode
         vertexShaderByteCode.Dispose();
         pixelShaderByteCode.Dispose();

         // Create Constant Buffer
         vertexShaderPerFrameConstantBuffer = new Buffer(device, Utilities.SizeOf<Matrix>(), ResourceUsage.Default, BindFlags.ConstantBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);

         // Create Depth Buffer & View
         depthBuffer = new Texture2D(device, new Texture2DDescription {
            Format = Format.D32_Float_S8X24_UInt,
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
         textureSampler = new SamplerState(device, new SamplerStateDescription {
            Filter = Filter.MinMagMipLinear,
            AddressU = TextureAddressMode.Wrap,
            AddressV = TextureAddressMode.Wrap,
            AddressW = TextureAddressMode.Wrap,
            BorderColor = Color.Black,
            ComparisonFunction = Comparison.Never,
            MaximumAnisotropy = 16,
            MipLodBias = 0,
            MinimumLod = 0,
            MaximumLod = 16
         });

         // Prepare the stages that don't change per frame
         immediateContext.InputAssembler.InputLayout = inputLayout;
         immediateContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
         immediateContext.VertexShader.Set(vertexShader);
         immediateContext.PixelShader.Set(pixelShader);
         immediateContext.PixelShader.SetSampler(0, textureSampler);
         immediateContext.Rasterizer.SetViewport(new Viewport(0, 0, form.ClientSize.Width, form.ClientSize.Height, 0.0f, 1.0f));
         immediateContext.OutputMerger.SetTargets(depthStencilView, backbufferRTV);

         var view = Matrix.LookAtLH(new Vector3(8, 8, 8), new Vector3(0, 0, 0), Vector3.UnitY);
         var proj = Matrix.PerspectiveFovLH((float)Math.PI / 4.0f, form.ClientSize.Width / (float)form.ClientSize.Height, 0.1f, 100.0f);
         viewProj = Matrix.Multiply(view, proj);
      }

      public void LoadModels() {
         // Instantiate Vertex buffer from vertex data
         vertexBuffers.Add(Buffer.Create(device, BindFlags.VertexBuffer, new[] {
            // 3D coordinates              UV Texture coordinates
            -1.0f, -1.0f, -1.0f, 1.0f,     0.0f, 1.0f, // Front
            -1.0f,  1.0f, -1.0f, 1.0f,     0.0f, 0.0f,
             1.0f,  1.0f, -1.0f, 1.0f,     1.0f, 0.0f,
            -1.0f, -1.0f, -1.0f, 1.0f,     0.0f, 1.0f,
             1.0f,  1.0f, -1.0f, 1.0f,     1.0f, 0.0f,
             1.0f, -1.0f, -1.0f, 1.0f,     1.0f, 1.0f,

            -1.0f, -1.0f,  1.0f, 1.0f,     1.0f, 0.0f, // BACK
             1.0f,  1.0f,  1.0f, 1.0f,     0.0f, 1.0f,
            -1.0f,  1.0f,  1.0f, 1.0f,     1.0f, 1.0f,
            -1.0f, -1.0f,  1.0f, 1.0f,     1.0f, 0.0f,
             1.0f, -1.0f,  1.0f, 1.0f,     0.0f, 0.0f,
             1.0f,  1.0f,  1.0f, 1.0f,     0.0f, 1.0f,

            -1.0f, 1.0f, -1.0f,  1.0f,     0.0f, 1.0f, // Top
            -1.0f, 1.0f,  1.0f,  1.0f,     0.0f, 0.0f,
             1.0f, 1.0f,  1.0f,  1.0f,     1.0f, 0.0f,
            -1.0f, 1.0f, -1.0f,  1.0f,     0.0f, 1.0f,
             1.0f, 1.0f,  1.0f,  1.0f,     1.0f, 0.0f,
             1.0f, 1.0f, -1.0f,  1.0f,     1.0f, 1.0f,

            -1.0f,-1.0f, -1.0f,  1.0f,     1.0f, 0.0f, // Bottom
             1.0f,-1.0f,  1.0f,  1.0f,     0.0f, 1.0f,
            -1.0f,-1.0f,  1.0f,  1.0f,     1.0f, 1.0f,
            -1.0f,-1.0f, -1.0f,  1.0f,     1.0f, 0.0f,
             1.0f,-1.0f, -1.0f,  1.0f,     0.0f, 0.0f,
             1.0f,-1.0f,  1.0f,  1.0f,     0.0f, 1.0f,

            -1.0f, -1.0f, -1.0f, 1.0f,     0.0f, 1.0f, // Left
            -1.0f, -1.0f,  1.0f, 1.0f,     0.0f, 0.0f,
            -1.0f,  1.0f,  1.0f, 1.0f,     1.0f, 0.0f,
            -1.0f, -1.0f, -1.0f, 1.0f,     0.0f, 1.0f,
            -1.0f,  1.0f,  1.0f, 1.0f,     1.0f, 0.0f,
            -1.0f,  1.0f, -1.0f, 1.0f,     1.0f, 1.0f,

             1.0f, -1.0f, -1.0f, 1.0f,     1.0f, 0.0f, // Right
             1.0f,  1.0f,  1.0f, 1.0f,     0.0f, 1.0f,
             1.0f, -1.0f,  1.0f, 1.0f,     1.0f, 1.0f,
             1.0f, -1.0f, -1.0f, 1.0f,     1.0f, 0.0f,
             1.0f,  1.0f, -1.0f, 1.0f,     0.0f, 0.0f,
             1.0f,  1.0f,  1.0f, 1.0f,     0.0f, 1.0f
         }));

         var model = new Model {
            vertexBufferIndex = 0,
            vertexOffset = 0,
            vertexCount = 36,

            indexBufferIndex = -1,
            indexOffset = -1,
            indexCount = -1,
            texture = Resource.FromFile<Texture2D>(device, "GeneticaMortarlessBlocks.jpg")
         };

         model.textureSRV = new ShaderResourceView(device, model.texture);

         models.Add(model);
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
