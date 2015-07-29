using System;
using SharpDX;

namespace Dargon.Renderer {
   public class Camera {
	   public Camera() {
	      m_theta = 0.0f;
	      m_phi = 0.0f;
	      m_radius = 0.0f;
	      m_up = 1.0f;
         m_pivotDistance = 30.0f;
         m_lookAt = new Vector3(0.0f, 0.0f, 0.0f);
	      m_view = Matrix.Identity;
	      m_proj = Matrix.Identity;
	      m_viewNeedsUpdate = true;
	   }

      public Camera(float theta, float phi, float radius, float pivotDistance) {
         m_theta = theta;
         m_phi = phi;
         m_radius = radius;
         m_up = 1.0f;
         m_pivotDistance = pivotDistance;
         m_lookAt = new Vector3(0.0f, 0.0f, 0.0f);
         m_view = Matrix.Identity;
         m_proj = Matrix.Identity;
         m_viewNeedsUpdate = true;
      }

      public Camera(float theta, float phi, float radius, float pivotDistance, Vector3 lookAt) {
         m_theta = theta;
         m_phi = phi;
         m_radius = radius;
         m_up = 1.0f;
         m_pivotDistance = pivotDistance;
         m_lookAt = lookAt;
         m_view = Matrix.Identity;
         m_proj = Matrix.Identity;
         m_viewNeedsUpdate = true;
      }

      private float m_theta;
      private float m_phi;
      private float m_radius;
      private float m_up;

      private readonly float m_pivotDistance;
      private Vector3 m_lookAt;
      

      private Matrix m_view;
      private Matrix m_proj;

      private bool m_viewNeedsUpdate;

      private const float TWO_PI = (float)(2 * Math.PI);

	   /**
	    * Rotate the camera about a point in front of it (m_target). Theta is a rotation 
	    * that tilts the camera forward and backward. Phi tilts the camera side to side. 
	    *
	    * @param dTheta    The number of radians to rotate in the theta direction
	    * @param dPhi      The number of radians to rotate in the phi direction
	    */
      
	   public void Rotate(float dTheta, float dPhi) {
         m_viewNeedsUpdate = true;

         if (m_up > 0.0f) {
            m_theta += dTheta;
         } else {
            m_theta -= dTheta;
         }

         m_phi += dPhi;

         // Keep phi within -2PI to +2PI for easy 'up' comparison
         if (m_phi > TWO_PI) {
            m_phi -= TWO_PI;
         } else if (m_phi < -TWO_PI) {
            m_phi += TWO_PI;
         }

         // If phi is between 0 to PI or -PI to -2PI, make 'up' be positive Y, other wise make it negative Y
         if ((m_phi > 0 && m_phi < Math.PI) || (m_phi < -Math.PI && m_phi > -TWO_PI)) {
            m_up = 1.0f;
         } else {
            m_up = -1.0f;
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
         m_viewNeedsUpdate = true;

         m_radius -= distance;

         // Don't let the radius go negative
         // If it does, re-project our target down the look vector
         if (!(m_radius <= 0.0f))
            return;

         m_radius = m_pivotDistance;
         var look = Vector3.Normalize(m_lookAt - GetCameraPosition());
         m_lookAt += look * m_pivotDistance;
      }

      /**
       * Moves the camera within its local X-Y plane
       *
       * @param dx    The amount to move the camera right or left
       * @param dy    The amount to move the camera up or down
       */
      public void Pan(float dx, float dy) {
         m_viewNeedsUpdate = true;

         var look = Vector3.Normalize(m_lookAt - GetCameraPosition());
         var worldUp = new Vector3(0.0f, m_up, 0.0f);

         var right = Vector3.Cross(look, worldUp);
         var up = Vector3.Cross(look, right);

         m_lookAt += (right * dx) + (up * dy);
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
         m_proj = Matrix.PerspectiveFovLH((float)Math.PI * 0.25f, clientWidth / clientHeight, nearClip, farClip);
      }

      /**
	    * Returns the position of the camera in Cartesian coordinates
	    *
	    * @return    The position of the camera
	    */
      public Vector3 GetCameraPosition() {
		   return m_lookAt + ToCartesian();
	   }

      /**
       * Returns the view matrix represented by the camera
       *
       * @return    The view matrix
       */
      public Matrix GetView() {
         if (!m_viewNeedsUpdate)
            return m_view;

         UpdateViewMatrix();
         m_viewNeedsUpdate = false;

         return m_view;
      }

      /**
       * Returns the internal projection matrix
       *
       * @return    The projection matrix
       */
      public Matrix GetProj() { return m_proj; }

	   /**
	    * Re-creates the view matrix. Don't call this directly. Lazy load
	    * the view matrix with GetView().       
	    */
	   private void UpdateViewMatrix() {
         m_view = Matrix.LookAtLH(GetCameraPosition(), m_lookAt, new Vector3(0.0f, m_up, 0.0f));
	   }

      /**
       * A helper function for converting the camera's location parameters
       * into Cartesian coordinates relative to m_target. To get the absolute location,
       * add this to m_target;
       *
       * @return    The camera's location relative to m_target in cartesian coordinates
       */
      private Vector3 ToCartesian() {
		   var x = (float)(m_radius * Math.Sin(m_phi) * Math.Sin(m_theta));
         var y = (float)(m_radius * Math.Cos(m_phi));
         var z = (float)(m_radius * Math.Sin(m_phi) * Math.Cos(m_theta));

		   return new Vector3(x, y, z);
	   }
   };

}
