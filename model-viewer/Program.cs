using System.Collections.Generic;
using System.Drawing;
using System.Security.Cryptography;
using Dargon.Nest.Egg;
using Dargon.Renderer;
using Dargon.Scene.Api;

namespace model_viewer {
   public class ModelViewer : INestApplicationEgg {
      private RenderWindow renderWindow { get; set; }

      public NestResult Start(IEggParameters parameters) {
         renderWindow = new RenderWindow();
         renderWindow.Initialize(new Size(1280, 720), new Camera(0.0f, 0.0f, 10.0f, 10.0f), 1.0f, 1.0f, new List<SceneElement> { new MapElement(), new CharacterElement() });
         renderWindow.Run();

         return NestResult.Success;
      }

      public NestResult Shutdown() {
         renderWindow.Shutdown();

         return NestResult.Success;
      }
   }


   class Program {
      static void Main(string[] args) {
         var modelViewer = new ModelViewer();
         modelViewer.Start(null);
         modelViewer.Shutdown();
      }
   }
}
