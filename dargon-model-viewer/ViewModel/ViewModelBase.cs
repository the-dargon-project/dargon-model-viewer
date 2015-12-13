using Dargon.Client.ViewModels.Helpers;
using Dargon.League.WGEO;
using Dargon.Renderer;
using Dargon.Scene.Api.Util;
using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using SWF = System.Windows.Forms;

namespace Dargon.ModelViewer.ViewModel {
   public class ViewModelBase : INotifyPropertyChanged {
      public ViewModelBase(SWF.IWin32Window hiddenForm, RenderHost renderHost) {
         this.renderHost = renderHost;
         colorTextures = new ColorTextures();
         textureCache = new TextureCache(colorTextures, renderHost);
         renderer = new Renderer.Renderer(hiddenForm, renderHost, colorTextures, textureCache);
         mapLoaded = false;
         cameraPanScale = 1.0f;
         cameraScrollScale = 1.0f;
      }

      private RenderHost renderHost;
      private Renderer.Renderer renderer;
      private ColorTextures colorTextures;
      private TextureCache textureCache;

      private float cameraPanScale;
      private float cameraScrollScale;
      private const float kcameraPanScaleFactor = 400.0f;
      private const float kcameraScrollScaleFactor = 1000.0f;

      private string clickedTexture = "foo";
      public string ClickedTexture { get { return clickedTexture; } set { clickedTexture = value; OnPropertyChanged(); } }


      public void DoRender(IntPtr resourcePtr, bool isNewSurface) {
         if (isNewSurface) {
            renderer.ResizeBackbuffer(resourcePtr);
         }
         try {
            renderer.Render();
         } catch (Exception e) {
            Console.WriteLine(e);
         }
      }

      public void GridSingleClick(Point mouseLocation) {
         string pickedTexture;
         renderer.PickSceneAtScreenLocation((float)mouseLocation.X, (float)mouseLocation.Y, out pickedTexture);

         ClickedTexture = pickedTexture;
         renderHost.RequestRender();
      }


      public void CameraRotate(float dTheta, float dPhi) {
         renderer.Camera.Rotate(dTheta, dPhi);

         var distanceToScene = renderer.DistanceFromViewToFirstSceneObject();
         if (distanceToScene != float.MaxValue) {
            cameraPanScale = distanceToScene / kcameraPanScaleFactor;
            cameraScrollScale = distanceToScene / kcameraScrollScaleFactor;
         }
         renderHost.RequestRender();
      }

      public void CameraPan(float dx, float dy) {
         renderer.Camera.Pan(dx * cameraPanScale, dy * cameraPanScale);

         var distanceToScene = renderer.DistanceFromViewToFirstSceneObject();
         if (distanceToScene != float.MaxValue) {
            cameraPanScale = distanceToScene / kcameraPanScaleFactor;
            cameraScrollScale = distanceToScene / kcameraScrollScaleFactor;
         }
         renderHost.RequestRender();
      }

      public void CameraZoom(float distance) {
         renderer.Camera.Zoom(distance * cameraScrollScale);

         var distanceToScene = renderer.DistanceFromViewToFirstSceneObject();
         if (distanceToScene != float.MaxValue) {
            cameraPanScale = distanceToScene / kcameraPanScaleFactor;
            cameraScrollScale = distanceToScene / kcameraScrollScaleFactor;
         }
         renderHost.RequestRender();
      }


      private ICommand changeTextureCommand;
      public ICommand ChangeTextureCommand {
         get {
            return changeTextureCommand ?? (changeTextureCommand = new ActionCommand(o => {
               textureCache.ReplaceTexture(ClickedTexture, @"T:\Programming_Projects\dargon-root\dev\dargon-model-viewer\dargon-model-viewer\bin\Debug\marowak.png");
            }, o => mapLoaded));
         }
      }

      private bool mapLoaded;
      private ICommand loadMapCommand;

      public ICommand LoadMapCommand {
         get {
            return loadMapCommand ?? (loadMapCommand = new ActionCommand(o => {
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

                  Task.Factory.StartNew(
                     () => {
                        foreach (var textureName in wgeoFile.models.Select(m => m.TexturePath).Distinct()) {
                           Task.Factory.StartNew(
                              () => textureCache.ReplaceTextureAsync(textureName, Path.Combine(textureCache.BasePath, textureName)),
                              TaskCreationOptions.LongRunning);
                        }
                     },
                     CancellationToken.None,
                     TaskCreationOptions.LongRunning,
                     TaskScheduler.Default);

                  // Move the camera So it can see all the scene and lookAt is at the center of the scene
                  var sceneSize = renderer.GetSceneSize();
                  var length = new Vector3(sceneSize.Max.X - sceneSize.Min.X, sceneSize.Max.Y - sceneSize.Min.Y, sceneSize.Max.Z - sceneSize.Min.Z);
                  var maxLength = Math.Max(Math.Max(length.X, length.Y), length.Z);
                  var sceneCenter = new Vector3(sceneSize.Min.X + (length.X / 2.0f), sceneSize.Min.Y + (length.Y / 2.0f), sceneSize.Min.Z + (length.Z / 2.0f));

                  renderer.SetCamera((float)Math.PI, (float)Math.PI / 4.0f, maxLength, sceneCenter);

                  var distanceToScene = renderer.DistanceFromViewToFirstSceneObject();
                  if (distanceToScene != float.MaxValue) {
                     cameraPanScale = distanceToScene / kcameraPanScaleFactor;
                     cameraScrollScale = distanceToScene / kcameraScrollScaleFactor;
                  }
               }

               mapLoaded = true;
            }, o => !mapLoaded));
         }
      }

      public event PropertyChangedEventHandler PropertyChanged;

      protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) {
         PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
      }
   }
}
