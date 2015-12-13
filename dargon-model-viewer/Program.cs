using Dargon.ModelViewer.View;
using Dargon.Renderer;
using System;
using System.Threading;
using System.Windows;


namespace Dargon.ModelViewer {
   static class Program {
      private static ManualResetEventSlim signalEvent = new ManualResetEventSlim();

      [STAThread]
      static void Main(string[] args) {
         // Create a small hidden form
         // We need a window handle in order to create a swapchain / use Present
         // All the graphics debuggers need Present() to work properly
         var hiddenForm = new HiddenForm();

         var window = new MainWindow(hiddenForm);
         window.Show();
         window.Closed += OnClosed;

         // Run the form and an empty mainloop
         var application = Application.Current ?? new Application();
         application.Run();
      }

      static void OnClosed(object sender, EventArgs e) {
         Environment.Exit(0);
      }
   }
}
