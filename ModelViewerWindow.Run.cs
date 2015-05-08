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
         var worldViewProj = camera.GetView() * camera.GetProj();
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
   }
}
