using System;
using System.IO;
using System.Linq;
using Dargon.Scene.Api;
using SharpDX.Direct3D11;
using Buffer = SharpDX.Direct3D11.Buffer;

namespace Dargon.Renderer {
   public class RenderMesh {
      public RenderMesh(Device device, Mesh mesh) {
         var rawVertexData = mesh.Vertices.SelectMany(vert => new[] { vert.Position.X, vert.Position.Y, vert.Position.Z, 1.0f, vert.TexCoord.X, vert.TexCoord.Y }).ToArray();
         VertexBuffer = Buffer.Create(device, BindFlags.VertexBuffer, rawVertexData);

         IndexBuffer = Buffer.Create(device, BindFlags.IndexBuffer, mesh.Indices.ToArray());
         IndexCount = mesh.Indices.Count;

         TexturePath = mesh.TexturePath;
      }

      public Buffer VertexBuffer;
      public Buffer IndexBuffer;
      public int IndexCount;
      public string TexturePath;
   }
}
