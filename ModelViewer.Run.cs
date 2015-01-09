using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Windows;

namespace Dargon.ModelViewer {
   public partial class ModelViewerWindow {
      public void Start() {
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
            immediateContext.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(vertexBuffers[model.vertexBufferIndex], 40, 0));
            immediateContext.InputAssembler.SetIndexBuffer(indexBuffers[model.indexBufferIndex], Format.R16_UInt, 0);
            immediateContext.PixelShader.SetShaderResource(0, model.textureSRV);

            immediateContext.DrawIndexed(model.indexCount, model.indexOffset, model.vertexOffset);
         }

         // Present
         swapChain.Present(1, PresentFlags.None);
      }
   }
}
