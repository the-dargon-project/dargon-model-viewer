using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using Dargon.Client.ViewModels.Helpers;
using Dargon.League.WGEO;
using Dargon.ModelViewer.View;
using Dargon.Renderer;
using Dargon.Scene.Api.Util;
using Microsoft.Wpf.Interop.DirectX;
using MessageBox = System.Windows.MessageBox;
using SWF = System.Windows.Forms;

namespace Dargon.ModelViewer.ViewModel {
   public class ViewModelBase : INotifyPropertyChanged {
      public ViewModelBase(SWF.IWin32Window hiddenForm) {
         textureCache = new TextureCache();
         renderer = new Renderer.Renderer(hiddenForm, textureCache);

         var dialog = new SWF.FolderBrowserDialog();
         if (dialog.ShowDialog() != SWF.DialogResult.OK) {
            return;
         }

         textureCache.BasePath = dialog.SelectedPath + "/scene/textures";
         var mapPath = dialog.SelectedPath + "/scene/room.wgeo";

         using (var fs = File.OpenRead(mapPath)) {
            var reader = new WGEOReader();

            var wgeoFile = reader.Read(fs);
            foreach (var model in wgeoFile.models) {
               renderer.AddMeshToScene(model, new Vector3());
            }
         }
      }


      private Renderer.Renderer renderer;
      private TextureCache textureCache;

      private string clickedTexture = "foo";
      public string ClickedTexture { get { return clickedTexture; } set { clickedTexture = value; OnPropertyChanged(); } }


      public void DoRender(IntPtr resourcePtr, bool isNewSurface) {
         if (isNewSurface) {
            renderer.ResizeBackbuffer(resourcePtr);
         }
         renderer.Render();
      }

      public void GridSingleClick(Point mouseLocation) {
         ClickedTexture = renderer.GetTextureAtScreenLocation((float)mouseLocation.X, (float)mouseLocation.Y);
      }


      public void CameraRotate(float dTheta, float dPhi) {
         renderer.Camera.Rotate(dTheta, dPhi);
      }

      public void CameraPan(float dx, float dy) {
         renderer.Camera.Pan(dx, dy);
      }

      public void CameraZoom(float distance) {
         renderer.Camera.Zoom(distance);
      }


      private ICommand buttonClickCommand;

      public ICommand ButtonClickCommand {
         get {
            return buttonClickCommand ?? (buttonClickCommand = new ActionCommand(o => {
               textureCache.SwapSRV(ClickedTexture, @"T:\Programming_Projects\dargon-root\dev\dargon-model-viewer\dargon-model-viewer\bin\Debug\marowak.dds");
            }));
         }

      }


      public event PropertyChangedEventHandler PropertyChanged;

      protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) {
         PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
      }
   }
}
