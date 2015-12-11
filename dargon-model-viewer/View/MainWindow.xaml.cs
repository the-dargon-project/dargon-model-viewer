using System;
using System.IO;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Input;
using Dargon.League.WGEO;
using Dargon.Scene.Api.Util;
using IWin32Window = System.Windows.Forms.IWin32Window;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using SWF = System.Windows.Forms;

namespace Dargon.ModelViewer.View {
   /// <summary>
   /// Interaction logic for MainWindow.xaml
   /// </summary>
   public partial class MainWindow : Window {
      public MainWindow(IWin32Window hiddenForm) {
         InitializeComponent();

         renderer = new Renderer.Renderer(hiddenForm);
         cameraPanScale = 5.0f;
         cameraScrollScale = 1.0f;

         var dialog = new SWF.FolderBrowserDialog();
         if (dialog.ShowDialog() == SWF.DialogResult.OK) {
            renderer.TextureCache.BasePath = dialog.SelectedPath + "/scene/textures";
            var mapPath = dialog.SelectedPath + "/scene/room.wgeo";

            using (var fs = File.OpenRead(mapPath)) {
               var reader = new WGEOReader();

               var wgeoFile = reader.Read(fs);
               foreach (var model in wgeoFile.models) {
                  renderer.AddMeshToScene(model, new Vector3());
               }
            }
         }

         lastRender = DateTime.Now.TimeOfDay;
         lastVisible = true;
         GridHost.Loaded += GridHostLoaded;
         GridHost.SizeChanged += GridHostSizeChanged;
      }


      private Renderer.Renderer renderer;
      private TimeSpan lastRender;
      private bool lastVisible;
      private Point mouseLastLocation;

      private float cameraPanScale;
      private float cameraScrollScale;

      private void GridHostLoaded(object sender, RoutedEventArgs e) {
         // Set up the interop image and start rendering
         InteropImage.WindowOwner = new System.Windows.Interop.WindowInteropHelper(this).Handle;
         InteropImage.OnRender = DoRender;

         InteropImage.RequestRender();

         GridHost.MouseDown += OnMouseDown;
         GridHost.MouseUp += OnMouseUp;
         GridHost.MouseMove += OnMouseMove;
         GridHost.MouseWheel += OnMouseWheel;

         CompositionTarget.Rendering += CompositionTarget_Rendering;
      }

      private void GridHostSizeChanged(object sender, SizeChangedEventArgs e) {
         var dpiScale = 1.0; // default value for 96 dpi

         // determine DPI
         // (as of .NET 4.6.1, this returns the DPI of the primary monitor, if you have several different DPIs)
         var hwndTarget = PresentationSource.FromVisual(this).CompositionTarget as HwndTarget;
         if (hwndTarget != null) {
            dpiScale = hwndTarget.TransformToDevice.M11;
         }

         var surfWidth = (int)(GridHost.ActualWidth < 0 ? 0 : Math.Ceiling(GridHost.ActualWidth * dpiScale));
         var surfHeight = (int)(GridHost.ActualHeight < 0 ? 0 : Math.Ceiling(GridHost.ActualHeight * dpiScale));

         // Notify the D3D11Image of the pixel size desired for the DirectX rendering.
         // The D3DRendering component will determine the size of the new surface it is given, at that point.
         InteropImage.SetPixelSize(surfWidth, surfHeight);

         // Stop rendering if the D3DImage isn't visible - currently just if width or height is 0
         // TODO: more optimizations possible (scrolled off screen, etc...)
         var isVisible = surfWidth != 0 && surfHeight != 0;
         if (lastVisible == isVisible) {
            return;
         }

         lastVisible = isVisible;
         if (lastVisible) {
            CompositionTarget.Rendering += CompositionTarget_Rendering;
         } else {
            CompositionTarget.Rendering -= CompositionTarget_Rendering;
         }
      }

      private void CompositionTarget_Rendering(object sender, EventArgs e) {
         var args = (RenderingEventArgs)e;

         // It's possible for Rendering to call back twice in the same frame 
         // so only render when we haven't already rendered in this frame.
         if (lastRender == args.RenderingTime) {
            return;
         }

         InteropImage.RequestRender();
         lastRender = args.RenderingTime;
      }

      private void DoRender(IntPtr resourcePtr, bool isNewSurface) {
         if (isNewSurface) {
            renderer.ResizeBackbuffer(resourcePtr);
         }
         renderer.Render();
      }

      private void OnMouseDown(object sender, MouseButtonEventArgs e) {
         mouseLastLocation = e.GetPosition(GridHost);
         GridHost.CaptureMouse();
      }

      private void OnMouseUp(object sender, MouseButtonEventArgs e) {
         GridHost.ReleaseMouseCapture();
      }

      private void OnMouseMove(object sender, MouseEventArgs e) {
         var location = e.GetPosition(GridHost);

         if (GridHost.IsMouseCaptured && e.LeftButton == MouseButtonState.Pressed) {
            if ((Keyboard.Modifiers & ModifierKeys.Alt) > 0) {
               // Calculate the new phi and theta based on mouse position relative to where the user clicked
               var dPhi = ((float)(mouseLastLocation.Y - location.Y) / 300);
               var dTheta = ((float)(mouseLastLocation.X - location.X) / 300);

               renderer.Camera.Rotate(-dTheta, dPhi);
            }
         } else if (GridHost.IsMouseCaptured && e.MiddleButton == MouseButtonState.Pressed) {
            if ((Keyboard.Modifiers & ModifierKeys.Alt) > 0) {
               var dx = ((float)(mouseLastLocation.X - location.X));
               var dy = ((float)(mouseLastLocation.Y - location.Y));

               renderer.Camera.Pan(-dx * cameraPanScale, dy * cameraPanScale);
            }
         }

         mouseLastLocation = location;
      }

      private void OnMouseWheel(object sender, MouseWheelEventArgs e) {
         // Make each wheel dedent correspond to a size based on the scene
         renderer.Camera.Zoom(e.Delta * cameraScrollScale);
      }
   }
}
