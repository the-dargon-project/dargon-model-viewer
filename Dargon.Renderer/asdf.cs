using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Dargon.ModelViewer {
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
   }
}
