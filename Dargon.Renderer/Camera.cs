using System;
using SharpDX;

namespace Dargon.Renderer {
   public class Camera {
      public Camera() {
         theta = 0.0f;
         phi = (float)Math.PI / 2.0f;
         radius = 5.0f;
         up = 1.0f;
         pivotDistance = 100.0f;
         lookAt = new Vector3(0.0f, 0.0f, 0.0f);
         view = Matrix.Identity;
         proj = Matrix.Identity;
         viewNeedsUpdate = true;
      }

      public Camera(float theta, float phi, float radius, float pivotDistance) {
         this.theta = theta;
         this.phi = phi;
         this.radius = radius;
         up = 1.0f;
         this.pivotDistance = pivotDistance;
         lookAt = new Vector3(0.0f, 0.0f, 0.0f);
         view = Matrix.Identity;
         proj = Matrix.Identity;
         viewNeedsUpdate = true;
      }

      public Camera(float theta, float phi, float radius, float pivotDistance, Vector3 lookAt) {
         this.theta = theta;
         this.phi = phi;
         this.radius = radius;
         up = 1.0f;
         this.pivotDistance = pivotDistance;
         this.lookAt = lookAt;
         view = Matrix.Identity;
         proj = Matrix.Identity;
         viewNeedsUpdate = true;
      }

      private float theta;
      private float phi;
      private float radius;
      private float up;

      private readonly float pivotDistance;
      private Vector3 lookAt;


      private Matrix view;
      private Matrix proj;

      private bool viewNeedsUpdate;

      private const float TWO_PI = (float)(2 * Math.PI);

      /**
	    * Rotate the camera about a point in front of it (m_target). Theta is a rotation 
	    * that tilts the camera forward and backward. Phi tilts the camera side to side. 
	    *
	    * @param dTheta    The number of radians to rotate in the theta direction
	    * @param dPhi      The number of radians to rotate in the phi direction
	    */
      public void Rotate(float dTheta, float dPhi) {
         viewNeedsUpdate = true;

         if (up > 0.0f) {
            theta += dTheta;
         } else {
            theta -= dTheta;
         }

         phi += dPhi;

         // Keep phi within -2PI to +2PI for easy 'up' comparison
         if (phi > TWO_PI) {
            phi -= TWO_PI;
         } else if (phi < -TWO_PI) {
            phi += TWO_PI;
         }

         // If phi is between 0 to PI or -PI to -2PI, make 'up' be positive Y, other wise make it negative Y
         if ((phi > 0 && phi < Math.PI) || (phi < -Math.PI && phi > -TWO_PI)) {
            up = 1.0f;
         } else {
            up = -1.0f;
         }
      }

      /**
       * Move the camera down the look vector, closer to m_target. If we overtake m_target,
       * it is reprojected 30 units down the look vector
       *
       * TODO: Find a way to *not* hard-code the reprojection distance. Perhaps base it on the 
       *       scene size? Or maybe have it defined in an settings.ini file
       *
       * @param distance    The distance to zoom. Negative distance will move the camera away from the target, positive will move towards
       */
      public void Zoom(float distance) {
         viewNeedsUpdate = true;

         radius -= distance;

         // Don't let the radius go negative
         // If it does, re-project our target down the look vector
         if (!(radius <= 0.0f))
            return;

         radius = pivotDistance;
         var look = Vector3.Normalize(lookAt - GetCameraPosition());
         lookAt += look * pivotDistance;
      }

      /**
       * Moves the camera within its local X-Y plane
       *
       * @param dx    The amount to move the camera right or left
       * @param dy    The amount to move the camera up or down
       */
      public void Pan(float dx, float dy) {
         viewNeedsUpdate = true;

         var look = Vector3.Normalize(lookAt - GetCameraPosition());
         var worldUp = new Vector3(0.0f, this.up, 0.0f);

         var right = Vector3.Cross(look, worldUp);
         var up = Vector3.Cross(look, right);

         lookAt += (right * dx) + (up * dy);
      }

      /**
       * Re-creates the internal projection matrix based on the input parameters
       *
       * @param clientWidth     The width of the client window
       * @param clientHeight    The height of the client window
       * @param nearClip        The distance to the near clip plane
       * @param farClip         The distance to the far clip plane
       */
      public void UpdateProjectionMatrix(float clientWidth, float clientHeight, float nearClip, float farClip) {
         proj = Matrix.PerspectiveFovLH((float)Math.PI * 0.25f, clientWidth / clientHeight, nearClip, farClip);
      }

      /**
	    * Returns the position of the camera in Cartesian coordinates
	    *
	    * @return    The position of the camera
	    */
      public Vector3 GetCameraPosition() {
         return lookAt + ToCartesian();
      }

      /**
       * Returns the view matrix represented by the camera
       *
       * @return    The view matrix
       */
      public Matrix GetView() {
         if (!viewNeedsUpdate)
            return view;

         UpdateViewMatrix();
         viewNeedsUpdate = false;

         return view;
      }

      /**
       * Returns the internal projection matrix
       *
       * @return    The projection matrix
       */
      public Matrix GetProj() { return proj; }

      /**
	    * Re-creates the view matrix. Don't call this directly. Lazy load
	    * the view matrix with GetView().       
	    */
      private void UpdateViewMatrix() {
         view = Matrix.LookAtLH(GetCameraPosition(), lookAt, new Vector3(0.0f, up, 0.0f));
      }

      /**
       * A helper function for converting the camera's location parameters
       * into Cartesian coordinates relative to m_target. To get the absolute location,
       * add this to m_target;
       *
       * @return    The camera's location relative to m_target in cartesian coordinates
       */
      private Vector3 ToCartesian() {
         var x = (float)(radius * Math.Sin(phi) * Math.Sin(theta));
         var y = (float)(radius * Math.Cos(phi));
         var z = (float)(radius * Math.Sin(phi) * Math.Cos(theta));

         return new Vector3(x, y, z);
      }
   };

}
