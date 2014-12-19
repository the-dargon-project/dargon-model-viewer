using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Windows;

namespace Dargon.ModelViewer {
   public partial class ModelViewer {
      public void Run() {
         // Bind our Run() function to the delegate of the RenderForm
         RenderLoop.Run(form, RunInternal);
      }

      private void RunInternal() {
         // Clear views
         immediateContext.ClearDepthStencilView(depthStencilView, DepthStencilClearFlags.Depth, 1.0f, 0);
         immediateContext.ClearRenderTargetView(backbufferRTV, Color.Black);

         // Update WorldViewProj Matrix
         var worldViewProj = viewProj;
         worldViewProj.Transpose();
         immediateContext.UpdateSubresource(ref worldViewProj, vertexShaderPerFrameConstantBuffer);
         immediateContext.VertexShader.SetConstantBuffer(0, vertexShaderPerFrameConstantBuffer);

         foreach (var model in models) {
            immediateContext.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(vertexBuffers[model.vertexBufferIndex], Utilities.SizeOf<Vector4>() + Utilities.SizeOf<Vector2>(), 0));
            immediateContext.PixelShader.SetShaderResource(0, model.textureSRV);

            immediateContext.Draw(model.vertexCount, 0);
         }

         // Present
         swapChain.Present(0, PresentFlags.None);
      }
   }
}
