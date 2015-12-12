using System.Collections.Generic;
using System.Linq;
using Dargon.Scene.Api;
using SharpDX;
using SharpDX.Direct3D11;
using Buffer = SharpDX.Direct3D11.Buffer;

namespace Dargon.Renderer {
   public class Triangle {
      public Triangle(Vector3 v0, Vector3 v1, Vector3 v2) {
         V0 = v0;
         V1 = v1;
         V2 = v2;
      }

      public Vector3 V0;
      public Vector3 V1;
      public Vector3 V2;
   }

   public class RenderMesh {
      public RenderMesh(Device device, Mesh mesh) {
         var rawVertexData = mesh.Vertices.SelectMany(vert => new[] { vert.Position.X, vert.Position.Y, vert.Position.Z, 1.0f, vert.TexCoord.X, vert.TexCoord.Y }).ToArray();
         VertexBuffer = Buffer.Create(device, BindFlags.VertexBuffer, rawVertexData);

         IndexBuffer = Buffer.Create(device, BindFlags.IndexBuffer, mesh.Indices.ToArray());
         IndexCount = mesh.Indices.Count;

         AABB = new BoundingBox(new Vector3(mesh.AABB.Min.X, mesh.AABB.Min.Y, mesh.AABB.Min.Z), new Vector3(mesh.AABB.Max.X, mesh.AABB.Max.Y, mesh.AABB.Max.Z));
         var triangles = new List<Triangle>();
         for (var i = 0; i < mesh.Indices.Count; i+=3) {
            var v0 = mesh.Vertices[mesh.Indices[i]].Position;
            var v1 = mesh.Vertices[mesh.Indices[i + 1]].Position;
            var v2 = mesh.Vertices[mesh.Indices[i + 2]].Position;

            triangles.Add(new Triangle(new Vector3(v0.X, v0.Y, v0.Z),
                                       new Vector3(v1.X, v1.Y, v1.Z),
                                       new Vector3(v2.X, v2.Y, v2.Z)));
         }
         Triangles = triangles.ToArray();

         TexturePath = mesh.TexturePath;
      }

      public Buffer VertexBuffer;
      public Buffer IndexBuffer;
      public int IndexCount;

      public BoundingBox AABB;
      public Triangle[] Triangles;

      public string TexturePath;
   }
}
