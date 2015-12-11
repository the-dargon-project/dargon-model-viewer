using System.Windows.Forms;
using SharpDX.Windows;

namespace Dargon.Renderer {
   public partial class HiddenForm : Form {
      public HiddenForm() {
         InitializeComponent();
      }

      public void RenderFrame() {
         // the form is shown on application start (during the start of the Mainloop on sharp dx side)
         // if you don't want to hide the window change InitializeComponent, too.
         if (Visible)
            Hide();
      }

      public void Run() {
         RenderLoop.Run(this, RenderFrame);
      }
   }
}
