using Dargon.ModelViewer.ViewModel;
using Dargon.Renderer;
using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using IWin32Window = System.Windows.Forms.IWin32Window;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;

namespace Dargon.ModelViewer.View {
   /// <summary>
   /// Interaction logic for MainWindow.xaml
   /// </summary>
   public partial class MainWindow : Window, RenderHost {
      public MainWindow(IWin32Window hiddenForm) {
         InitializeComponent();
         DataContext = viewModel = new ViewModelBase(hiddenForm, this);

         lastRender = DateTime.Now.TimeOfDay;
         lastVisible = true;

         ImageHostGrid.Loaded += ImageHostGridLoaded;
         ImageHostGrid.SizeChanged += ImageHostGridSizeChanged;
      }


      private readonly ViewModelBase viewModel;
      private TimeSpan lastRender;
      private bool lastVisible;
      private Point mouseLastLocation;
      private Point mouseDownLocation;

      public void RequestRender() {
         Application.Current.Dispatcher.BeginInvoke(
            new Action(() => {
               InteropImage.RequestRender();
            }), DispatcherPriority.Render);
      }

      private void ImageHostGridLoaded(object sender, RoutedEventArgs e) {
         // Set up the interop image and start rendering
         InteropImage.WindowOwner = new WindowInteropHelper(this).Handle;
         InteropImage.OnRender = viewModel.DoRender;

         InteropImage.RequestRender();
         CompositionTarget.Rendering += CompositionTarget_Rendering;

         // Bind the mouse events
         ImageHostGrid.MouseDown += OnMouseDown;
         ImageHostGrid.MouseUp += OnMouseUp;
         ImageHostGrid.MouseMove += OnMouseMove;
         ImageHostGrid.MouseWheel += OnMouseWheel;
      }

      private void ImageHostGridSizeChanged(object sender, SizeChangedEventArgs e) {
         var dpiScale = 1.0; // default value for 96 dpi

         // determine DPI
         // (as of .NET 4.6.1, this returns the DPI of the primary monitor, if you have several different DPIs)
         var hwndTarget = PresentationSource.FromVisual(this)?.CompositionTarget as HwndTarget;
         if (hwndTarget != null) {
            dpiScale = hwndTarget.TransformToDevice.M11;
         }

         var surfWidth = (int)(ImageHostGrid.ActualWidth < 0 ? 0 : Math.Ceiling(ImageHostGrid.ActualWidth * dpiScale));
         var surfHeight = (int)(ImageHostGrid.ActualHeight < 0 ? 0 : Math.Ceiling(ImageHostGrid.ActualHeight * dpiScale));

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

         lastRender = args.RenderingTime;
      }


      #region MouseEvents

      private void OnMouseDown(object sender, MouseButtonEventArgs e) {
         mouseLastLocation = mouseDownLocation = e.GetPosition(ImageHostGrid);
         ImageHostGrid.CaptureMouse();
      }

      private void OnMouseUp(object sender, MouseButtonEventArgs e) {
         ImageHostGrid.ReleaseMouseCapture();

         var location = e.GetPosition(ImageHostGrid);

         // A 'click' happens if we MouseUp less than 5 pixels from where we MouseDown
         if (Math.Abs(location.X - mouseDownLocation.X) < 5 && Math.Abs(location.Y - mouseDownLocation.Y) < 5) {
            viewModel.GridSingleClick(location);
         }
      }

      private void OnMouseMove(object sender, MouseEventArgs e) {
         var location = e.GetPosition(ImageHostGrid);
         if (ImageHostGrid.IsMouseCaptured && e.LeftButton == MouseButtonState.Pressed) {
            if ((Keyboard.Modifiers & ModifierKeys.Alt) > 0) {
               // Calculate the new phi and theta based on mouse position relative to where the user clicked
               var dPhi = ((float)(mouseLastLocation.Y - location.Y) / 300);
               var dTheta = ((float)(mouseLastLocation.X - location.X) / 300);

               viewModel.CameraRotate(-dTheta, dPhi);
            }
         } else if (ImageHostGrid.IsMouseCaptured && e.MiddleButton == MouseButtonState.Pressed) {
            if ((Keyboard.Modifiers & ModifierKeys.Alt) > 0) {
               var dx = ((float)(mouseLastLocation.X - location.X));
               var dy = ((float)(mouseLastLocation.Y - location.Y));

               viewModel.CameraPan(-dx, dy);
            }
         }

         mouseLastLocation = location;
      }

      private void OnMouseWheel(object sender, MouseWheelEventArgs e) {
         // Make each wheel dedent correspond to a size based on the scene
         viewModel.CameraZoom(e.Delta);
      }

      #endregion // Mouse Events
   }
}
