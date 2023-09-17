using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using static Godot.Mathf;

namespace JD.Utility
{
	/// <summary>
	/// Tools for drawing debug shapes.
	/// </summary>
	public static partial class DebugDraw
	{
		#region Internal Variables
		// Internal variables
		private static readonly List<Action> _DrawQueue = new();
		private static readonly Control _Canvas = new();
		private static readonly SceneTree _SceneTree = Engine.GetMainLoop() as SceneTree;
		private static readonly Window _Window = _SceneTree.Root;
		private static readonly Camera3D _Camera = _Window.GetViewport().GetCamera3D();
		private static readonly SphereMesh _HemisphereMesh = new() { IsHemisphere = true, Radius = 0.5f, Height = 1.0f, RadialSegments = 32, Rings = 32 };
		private static readonly SphereMesh _SphereMesh = new() { Radius = 0.5f, Height = 1.0f, RadialSegments = 32, Rings = 32 };
		private static readonly CylinderMesh _CylinderMesh = new() { TopRadius = 0.5f, BottomRadius = 0.5f, Height = 1.0f, RadialSegments = 32, Rings = 32 };
		private static readonly CapsuleMesh _CapsuleMesh = new() { Radius = 0.5f, Height = 1.0f, RadialSegments = 32, Rings = 32 };
		private static readonly CylinderMesh _ConeMesh = new() { TopRadius = 0f, BottomRadius = 0.5f, Height = 1.0f, RadialSegments = 32, Rings = 32 };
		private static readonly Font _Font = new SystemFont() { FontNames = new[] { "JetBrains Mono", "Ubuntu Mono", "Courier New", "Courier", "monospace" } };
		private static readonly float _NinetyDegreesInRadians = DegToRad( 90.0f );
		private static SphereMesh _PrevHemisphereMesh = _HemisphereMesh;
		private static Transform3D _PrevHemisphereMeshTrans = Transform3D.Identity;
		private static Transform3D _PrevHemisphereCamTrans = Transform3D.Identity;
		private static Vector2[] _HemisphereHull = new Vector2[] { Vector2.Zero };
		private static SphereMesh _PrevSphereMesh = _SphereMesh;
		private static Transform3D _PrevSphereMeshTrans = Transform3D.Identity;
		private static Transform3D _PrevSphereCamTrans = Transform3D.Identity;
		private static Vector2[] _SphereHull = new Vector2[] { Vector2.Zero };
		private static CylinderMesh _PrevCylinderMesh = _CylinderMesh;
		private static Transform3D _PrevCylinderMeshTrans = Transform3D.Identity;
		private static Transform3D _PrevCylinderCamTrans = Transform3D.Identity;
		private static Vector2[] _CylinderHull = new Vector2[] { Vector2.Zero };
		private static CapsuleMesh _PrevCapsuleMesh = _CapsuleMesh;
		private static Transform3D _PrevCapsuleMeshTrans = Transform3D.Identity;
		private static Transform3D _PrevCapsuleCamTrans = Transform3D.Identity;
		private static Vector2[] _CapsuleHull = new Vector2[] { Vector2.Zero };
		private static CylinderMesh _PrevConeMesh = _ConeMesh;
		private static Transform3D _PrevConeMeshTrans = Transform3D.Identity;
		private static Transform3D _PrevConeCamTrans = Transform3D.Identity;
		private static Vector2[] _ConeHull = new Vector2[] { Vector2.Zero };
		#endregion

		#region Initialization
		/// <summary>
		/// Static class constructor (only ever called once).
		/// </summary>
		static DebugDraw()
		{
			// Add our canvas node to the scene so we can use it to draw
			_Window.AddChild( _Canvas );

			// Connect needed signals
			_Canvas.Draw += _CanvasDraw;
			_SceneTree.PhysicsFrame += _MaybeQueueRedraw;
		}
		#endregion

		#region Internal Methods
		/// <summary>
		/// Draws all the items in the drawing queue and then clears the queue.
		/// </summary>
		/// <returns><see langword="void"/></returns>
		private static void _CanvasDraw()
		{
			// Draw every item in the queue
			_DrawQueue.ForEach( d => d.Invoke() );

			// Clear the queue so it's ready for the next frame
			_DrawQueue.Clear();
		}

		/// <summary>
		/// Makes the canvas queue a redraw if needed.
		/// </summary>
		/// <returns><see langword="void"/></returns>
		private static void _MaybeQueueRedraw()
		{
			// If we need to draw anything...
			if ( _DrawQueue.Count > 0 )
			{
				// Tell the canvas to redraw
				_Canvas.QueueRedraw();
			}
		}

		/// <summary>
		/// Queues a draw action to happen.
		/// </summary>
		/// <param name="action">The method to call to do the drawing.</param>
		/// <returns><see langword="void"/></returns>
		private static void _Draw( Action action )
		{
			// Add this item to the drawing queue
			_DrawQueue.Add( action );
		}

		/// <summary>
		/// Internal helper method that draws a point shape in the game for debug visualization.
		/// </summary>
		/// <param name="position">The <see cref="Vector3">position</see> where the point will be drawn.</param>
		/// <param name="size">The size of the point.</param>
		/// <param name="color">The <see cref="Color"/> that the point will be drawn in.</param>
		/// <param name="antialiased">Whether the point should be anti-aliased.</param>
		/// <returns><see langword="void"/></returns>
		private static void _DrawPointImpl( Vector3 position, float size, Color color, bool antialiased )
		{
			_DrawCircleImpl(
				Transform3D.Identity.TranslatedLocal( position ).LookingAt( _Camera.GlobalPosition, Vector3.Up ),
				size / ( Pi * 2f ) * 0.01f,
				color,
				size,
				8,
				antialiased
			);
		}

		/// <summary>
		/// Internal helper method that draws a line shape in the game for debug visualization.
		/// </summary>
		/// <param name="start">The <see cref="Vector3">position</see> where the line will start drawing from.</param>
		/// <param name="end">The <see cref="Vector3">position</see> where the line will end drawing at.</param>
		/// <param name="color">The <see cref="Color"/> that the line will be drawn in.</param>
		/// <param name="thickness">The thickness of the line.</param>
		/// <param name="antialiased">Whether the line should be anti-aliased.</param>
		/// <returns><see langword="void"/></returns>
		private static void _DrawLineImpl( Vector3 start, Vector3 end, Color color, float thickness, bool antialiased )
		{
			_Canvas.DrawLine(
				_Camera.UnprojectPosition( start ),
				_Camera.UnprojectPosition( end ),
				color,
				thickness,
				antialiased
			);
		}

		/// <summary>
		/// Internal helper method that draws a direction line shape in the game for debug visualization.
		/// </summary>
		/// <param name="position">The <see cref="Vector3">position</see> of the direction line.</param>
		/// <param name="direction">The <see cref="Vector3">direction</see> that the line will point in.</param>
		/// <param name="color">The <see cref="Color"/> that the direction line will be drawn in.</param>
		/// <param name="thickness">The thickness of the direction line.</param>
		/// <param name="antialiased">Whether the direction line should be anti-aliased.</param>
		/// <returns><see langword="void"/></returns>
		private static void _DrawDirectionImpl( Vector3 position, Vector3 direction, Color color, float thickness, bool antialiased )
		{
			// Calculate some values for the direction line
			Vector3 dirNorm = direction.Normalized();

			// Draw the direction line
			_DrawLineImpl(
				position,
				position + ( dirNorm * ( direction.Length() - 0.15f ) ),
				color,
				thickness,
				antialiased
			);

			// Draw a cone at the end to indicate the direction
			Transform3D coneTrans = Transform3D.Identity;
			if ( ! dirNorm.Abs().IsEqualApprox( Vector3.Up ) )
			{
				coneTrans = coneTrans.LookingAt( dirNorm, Vector3.Up ).RotatedLocal( Vector3.Left, _NinetyDegreesInRadians );
			}
			coneTrans = coneTrans.Translated( position + ( dirNorm * ( direction.Length() - 0.075f ) ) );
			_DrawConeImpl(
				coneTrans,
				0.15f,
				0.1f,
				color,
				thickness,
				32,
				true,
				antialiased
			);
		}

		/// <summary>
		/// Internal helper method that draws a polyline shape in the game for debug visualization.
		/// </summary>
		/// <param name="points">An array of <see cref="Vector3">vectors</see> representing the points that the polyline will be drawn through.</param>
		/// <param name="color">The <see cref="Color"/> that the polyline will be drawn in.</param>
		/// <param name="thickness">The thickness of the polyline.</param>
		/// <param name="antialiased">Whether the polyline should be anti-aliased.</param>
		/// <returns><see langword="void"/></returns>
		private static void _DrawPolylineImpl( Vector3[] points, Color color, float thickness, bool antialiased )
		{
			_Canvas.DrawPolyline(
				points.Select( _Camera.UnprojectPosition ).ToArray(),
				color,
				thickness,
				antialiased
			);
		}

		/// <summary>
		/// Internal helper method that draws an arc shape in the game for debug visualization.
		/// </summary>
		/// <param name="transform">The <see cref="Transform3D"/> where the arc will be located at.</param>
		/// <param name="start">The angle that the arc starts at.</param>
		/// <param name="end">The angle that the arc ends at.</param>
		/// <param name="radiusX">The radius on the X axis of the arc.</param>
		/// <param name="radiusY">The radius on the Y axis of the arc.</param>
		/// <param name="color">The <see cref="Color"/> that the arc will be drawn in.</param>
		/// <param name="thickness">The thickness of the lines that make up the arc.</param>
		/// <param name="resolution">The resolution of the arc.</param>
		/// <param name="antialiased">Whether the arc should be anti-aliased.</param>
		/// <returns><see langword="void"/></returns>
		private static void _DrawArcImpl( Transform3D transform, float start, float end, float radiusX, float radiusY, Color color, float thickness, int resolution, bool antialiased )
		{
			_DrawPolylineImpl(
				_ArcPoints( transform, start, end, radiusX, radiusY, resolution ),
				color,
				thickness,
				antialiased
			);
		}

		/// <summary>
		/// Internal helper method that draws an ellipse shape in the game for debug visualization.
		/// </summary>
		/// <param name="transform">The <see cref="Transform3D"/> where the ellipse will be located at.</param>
		/// <param name="radiusX">The radius on the X axis of the ellipse.</param>
		/// <param name="radiusY">The radius on the Y axis of the ellipse.</param>
		/// <param name="color">The <see cref="Color"/> that the ellipse will be drawn in.</param>
		/// <param name="thickness">The thickness of the lines that make up the ellipse.</param>
		/// <param name="resolution">The resolution of the ellipse.</param>
		/// <param name="antialiased">Whether the ellipse should be anti-aliased.</param>
		/// <returns><see langword="void"/></returns>
		private static void _DrawEllipseImpl( Transform3D transform, float radiusX, float radiusY, Color color, float thickness, int resolution, bool antialiased )
		{
			_DrawArcImpl(
				transform,
				0f,
				360f,
				radiusX,
				radiusY,
				color,
				thickness,
				resolution,
				antialiased
			);
		}

		/// <summary>
		/// Internal helper method that draws a circle shape in the game for debug visualization.
		/// </summary>
		/// <param name="transform">The <see cref="Transform3D"/> where the circle will be located at.</param>
		/// <param name="radius">The radius of the circle.</param>
		/// <param name="color">The <see cref="Color"/> that the circle will be drawn in.</param>
		/// <param name="thickness">The thickness of the lines that make up the circle.</param>
		/// <param name="resolution">The resolution of the circle.</param>
		/// <param name="antialiased">Whether the circle should be anti-aliased.</param>
		/// <returns><see langword="void"/></returns>
		private static void _DrawCircleImpl( Transform3D transform, float radius, Color color, float thickness, int resolution, bool antialiased )
		{
			_DrawEllipseImpl(
				transform,
				radius,
				radius,
				color,
				thickness,
				resolution,
				antialiased
			);
		}

		/// <summary>
		/// Internal helper method that draws a wireframe hemisphere shape in the game for debug visualization.
		/// </summary>
		/// <param name="transform">The <see cref="Transform3D"/> where the hemisphere will be located at.</param>
		/// <param name="radius">The radius of the hemisphere.</param>
		/// <param name="color">The <see cref="Color"/> that the hemisphere will be drawn in.</param>
		/// <param name="thickness">The thickness of the lines that make up the wireframe.</param>
		/// <param name="resolution">The resolution of the hemisphere.</param>
		/// <param name="contour">Whether the contour of the hemisphere should also be drawn.</param>
		/// <param name="antialiased">Whether the wireframe should be anti-aliased.</param>
		/// <returns><see langword="void"/></returns>
		private static void _DrawHemisphereImpl( Transform3D transform, float radius, Color color, float thickness, int resolution, bool contour, bool antialiased )
		{
			// Draw two semicircles and one circle to represent the major axes of the hemisphere
			_DrawArcImpl(
				transform,
				-90f,
				180f,
				radius,
				radius,
				color,
				thickness,
				resolution,
				antialiased
			);
			_DrawArcImpl(
				transform.RotatedLocal( Vector3.Up, _NinetyDegreesInRadians ),
				-90f,
				180f,
				radius,
				radius,
				color,
				thickness,
				resolution,
				antialiased
			);
			_DrawCircleImpl(
				transform.RotatedLocal( Vector3.Right, _NinetyDegreesInRadians ),
				radius,
				color,
				thickness,
				resolution,
				antialiased
			);

			// Maybe draw the contour of the hemisphere from the perspective of the camera
			if ( contour )
			{
				// Update some values for comparison
				if ( ! IsEqualApprox( _HemisphereMesh.Radius, radius ) ) _HemisphereMesh.Radius = radius;
				if ( ! IsEqualApprox( _HemisphereMesh.Height, radius ) ) _HemisphereMesh.Height = radius;
				if ( _HemisphereMesh.Rings != resolution ) _HemisphereMesh.Rings = resolution;
				if ( _HemisphereMesh.RadialSegments != resolution ) _HemisphereMesh.RadialSegments = resolution;

				// If there has been any changes that would change the hemisphere hull...
				if (
					! IsEqualApprox( _HemisphereMesh.Radius, _PrevHemisphereMesh.Radius )
					|| ! IsEqualApprox( _HemisphereMesh.Height, _PrevHemisphereMesh.Height )
					|| ( _HemisphereMesh.Rings != _PrevHemisphereMesh.Rings )
					|| ( _HemisphereMesh.RadialSegments != _PrevHemisphereMesh.RadialSegments )
					|| ! transform.IsEqualApprox( _PrevHemisphereMeshTrans )
					|| ! _Camera.GlobalTransform.IsEqualApprox( _PrevHemisphereCamTrans )
				)
				{
					// Calculate the hull for the hemisphere in its new state
					_HemisphereHull = _ConvexHullFromMeshInView(
						_HemisphereMesh,
						transform,
						_Camera
					);

					// Cache values for comparison next frame
					_PrevHemisphereMesh = _HemisphereMesh;
					_PrevHemisphereMeshTrans = transform;
					_PrevHemisphereCamTrans = _Camera.GlobalTransform;
				}

				// If there is anything to actually draw...
				if ( _HemisphereHull.Length > 2 )
				{
					// Draw the outline of the hull
					_Canvas.DrawPolyline(
						_HemisphereHull,
						color,
						thickness,
						antialiased
					);
				}
			}
		}

		/// <summary>
		/// Internal helper method that draws a wireframe sphere shape in the game for debug visualization.
		/// </summary>
		/// <param name="transform">The <see cref="Transform3D"/> where the sphere will be located at.</param>
		/// <param name="radius">The radius of the sphere.</param>
		/// <param name="color">The <see cref="Color"/> that the sphere will be drawn in.</param>
		/// <param name="thickness">The thickness of the lines that make up the wireframe.</param>
		/// <param name="resolution">The resolution of the sphere.</param>
		/// <param name="contour">Whether the contour of the sphere should also be drawn.</param>
		/// <param name="antialiased">Whether the wireframe should be anti-aliased.</param>
		/// <returns><see langword="void"/></returns>
		private static void _DrawSphereImpl( Transform3D transform, float radius, Color color, float thickness, int resolution, bool contour, bool antialiased )
		{
			// Draw circles for the 3 major axes of the sphere
			_DrawCircleImpl(
				transform,
				radius,
				color,
				thickness,
				resolution,
				antialiased
			);
			_DrawCircleImpl(
				transform.RotatedLocal( Vector3.Up, _NinetyDegreesInRadians ),
				radius,
				color,
				thickness,
				resolution,
				antialiased
			);
			_DrawCircleImpl(
				transform.RotatedLocal( Vector3.Right, _NinetyDegreesInRadians ),
				radius,
				color,
				thickness,
				resolution,
				antialiased
			);

			// Maybe draw the contour of the sphere from the perspective of the camera
			if ( contour )
			{
				// Update some values for comparison
				if ( ! IsEqualApprox( _SphereMesh.Radius, radius ) ) _SphereMesh.Radius = radius;
				if ( ! IsEqualApprox( _SphereMesh.Height, radius ) ) _SphereMesh.Height = radius;
				if ( _SphereMesh.Rings != resolution ) _SphereMesh.Rings = resolution;
				if ( _SphereMesh.RadialSegments != resolution ) _SphereMesh.RadialSegments = resolution;

				// If there has been any changes that would change the sphere hull...
				if (
					! IsEqualApprox( _SphereMesh.Radius, _PrevSphereMesh.Radius )
					|| ! IsEqualApprox( _SphereMesh.Height, _PrevSphereMesh.Height )
					|| ( _SphereMesh.Rings != _PrevSphereMesh.Rings )
					|| ( _SphereMesh.RadialSegments != _PrevSphereMesh.RadialSegments )
					|| ! transform.IsEqualApprox( _PrevSphereMeshTrans )
					|| ! _Camera.GlobalTransform.IsEqualApprox( _PrevSphereCamTrans )
				)
				{
					// Calculate the hull for the sphere in its new state
					_SphereHull = _ConvexHullFromMeshInView(
						_SphereMesh,
						transform,
						_Camera
					);

					// Cache values for comparison next frame
					_PrevSphereMesh = _SphereMesh;
					_PrevSphereMeshTrans = transform;
					_PrevSphereCamTrans = _Camera.GlobalTransform;
				}

				// If there is anything to actually draw...
				if ( _SphereHull.Length > 2 )
				{
					// Draw the outline of the hull
					_Canvas.DrawPolyline(
						_SphereHull,
						color,
						thickness,
						antialiased
					);
				}
			}
		}

		/// <summary>
		/// Internal helper method that draws a wireframe cylinder shape in the game for debug visualization.
		/// </summary>
		/// <param name="start">The <see cref="Transform3D"/> where the cylinder will start from.</param>
		/// <param name="end">The <see cref="Transform3D"/> where the cylinder will end at.</param>
		/// <param name="height">The height of the cylinder</param>
		/// <param name="topRadius">The radius of the top of the cylinder.</param>
		/// <param name="bottomRadius">The radius of the bottom of the cylinder.</param>
		/// <param name="color">The <see cref="Color"/> that the cylinder will be drawn in.</param>
		/// <param name="thickness">The thickness of the lines that make up the wireframe.</param>
		/// <param name="resolution">The resolution of the cylinder.</param>
		/// <param name="contour">Whether the contour of the cylinder should also be drawn.</param>
		/// <param name="antialiased">Whether the wireframe should be anti-aliased.</param>
		/// <returns><see langword="void"/></returns>
		private static void _DrawCylinderImpl( Transform3D start, Transform3D end, float topRadius, float bottomRadius, Color color, float thickness, int resolution, bool contour, bool antialiased )
		{
			// Calculate the transforms for the ends of the cylinder
			Transform3D startFacingEnd = start.LookingAt( end.Origin, start.Basis.Z.Cross( start.Basis.Y ) );
			Transform3D endFacingStart = end.LookingAt( start.Origin, end.Basis.Z.Cross( end.Basis.Y ) );
			Transform3D startFacingEndRotated = startFacingEnd.RotatedLocal( Vector3.Right, _NinetyDegreesInRadians );
			Transform3D endFacingStartRotated = endFacingStart.RotatedLocal( Vector3.Left, -_NinetyDegreesInRadians );

			// Draw the end circles for the cylinder
			_DrawCircleImpl( startFacingEnd, topRadius, color, thickness, resolution, antialiased );
			_DrawCircleImpl( endFacingStart, bottomRadius, color, thickness, resolution, antialiased );

			// Draw the lines connecting the end circles
			_DrawLineImpl(
				startFacingEndRotated.TranslatedLocal( Vector3.Right * topRadius ).Origin,
				endFacingStartRotated.TranslatedLocal( Vector3.Left * bottomRadius ).Origin,
				color,
				thickness,
				antialiased
			);
			_DrawLineImpl(
				startFacingEndRotated.TranslatedLocal( Vector3.Left * topRadius ).Origin,
				endFacingStartRotated.TranslatedLocal( Vector3.Right * bottomRadius ).Origin,
				color,
				thickness,
				antialiased
			);
			_DrawLineImpl(
				startFacingEndRotated.TranslatedLocal( Vector3.Back * topRadius ).Origin,
				endFacingStartRotated.TranslatedLocal( Vector3.Back * bottomRadius ).Origin,
				color,
				thickness,
				antialiased
			);
			_DrawLineImpl(
				startFacingEndRotated.TranslatedLocal( Vector3.Forward * topRadius ).Origin,
				endFacingStartRotated.TranslatedLocal( Vector3.Forward * bottomRadius ).Origin,
				color,
				thickness,
				antialiased
			);

			// Maybe draw the contour of the cylinder from the perspective of the camera...
			if ( contour )
			{
				// Update some values for comparison
				float height = start.Origin.DistanceTo( end.Origin );
				if ( ! IsEqualApprox( _CylinderMesh.TopRadius, topRadius ) ) _CylinderMesh.TopRadius = topRadius;
				if ( ! IsEqualApprox( _CylinderMesh.BottomRadius, bottomRadius ) ) _CylinderMesh.BottomRadius = bottomRadius;
				if ( ! IsEqualApprox( _CylinderMesh.Height, height ) ) _CylinderMesh.Height = height;
				if ( _CylinderMesh.Rings != resolution ) _CylinderMesh.Rings = resolution;
				if ( _CylinderMesh.RadialSegments != resolution ) _CylinderMesh.RadialSegments = resolution;
				Transform3D meshTrans = new( startFacingEndRotated.Basis, ( start.Origin + end.Origin ) / 2f );

				// If there has been any changes that would change the cylinder hull...
				if (
					! IsEqualApprox( _CylinderMesh.TopRadius, _PrevCylinderMesh.TopRadius )
					|| ! IsEqualApprox( _CylinderMesh.BottomRadius, _PrevCylinderMesh.BottomRadius )
					|| ! IsEqualApprox( _CylinderMesh.Height, _PrevCylinderMesh.Height )
					|| ( _CylinderMesh.Rings != _PrevCylinderMesh.Rings )
					|| ( _CylinderMesh.RadialSegments != _PrevCylinderMesh.RadialSegments )
					|| ! meshTrans.IsEqualApprox( _PrevCylinderMeshTrans )
					|| ! _Camera.GlobalTransform.IsEqualApprox( _PrevCylinderCamTrans )
				)
				{
					// Calculate the hull for the cylinder in its new state
					_CylinderHull = _ConvexHullFromMeshInView(
						_CylinderMesh,
						meshTrans,
						_Camera
					);

					// Cache values for comparison next frame
					_PrevCylinderMesh = _CylinderMesh;
					_PrevCylinderMeshTrans = meshTrans;
					_PrevCylinderCamTrans = _Camera.GlobalTransform;
				}

				// If there is anything to actually draw...
				if ( _CylinderHull.Length > 2 )
				{
					// Draw the outline of the hull
					_Canvas.DrawPolyline(
						_CylinderHull,
						color,
						thickness,
						antialiased
					);
				}
			}
		}

		/// <summary>
		/// Internal helper method that draws a wireframe cylinder shape in the game for debug visualization.
		/// </summary>
		/// <param name="transform">The <see cref="Transform3D"/> where the cylinder will be located at.</param>
		/// <param name="height">The height of the cylinder.</param>
		/// <param name="topRadius">The radius of the top of the cylinder.</param>
		/// <param name="bottomRadius">The radius of the bottom of the cylinder.</param>
		/// <param name="color">The <see cref="Color"/> that the cylinder will be drawn in.</param>
		/// <param name="thickness">The thickness of the lines that make up the wireframe.</param>
		/// <param name="resolution">The resolution of the cylinder.</param>
		/// <param name="contour">Whether the contour of the cylinder should also be drawn.</param>
		/// <param name="antialiased">Whether the wireframe should be anti-aliased.</param>
		/// <returns><see langword="void"/></returns>
		private static void _DrawCylinderImpl( Transform3D transform, float height, float topRadius, float bottomRadius, Color color, float thickness, int resolution, bool contour, bool antialiased )
		{
			float halfHeight = height / 2f;

			_DrawCylinderImpl(
				transform.TranslatedLocal( Vector3.Up * halfHeight ),
				transform.TranslatedLocal( Vector3.Down * halfHeight ),
				topRadius,
				bottomRadius,
				color,
				thickness,
				resolution,
				contour,
				antialiased
			);
		}

		/// <summary>
		/// Internal helper method that draws a wireframe capsule shape in the game for debug visualization.
		/// </summary>
		/// <param name="start">The <see cref="Transform3D"/> where the capsule will start from.</param>
		/// <param name="end">The <see cref="Transform3D"/> where the capsule will end at.</param>
		/// <param name="height">The height of the capsule, not including the hemisphere caps.</param>
		/// <param name="radius">The radius of the capsule.</param>
		/// <param name="color">The <see cref="Color"/> that the capsule will be drawn in.</param>
		/// <param name="thickness">The thickness of the lines that make up the wireframe.</param>
		/// <param name="resolution">The resolution of the capsule.</param>
		/// <param name="contour">Whether the contour of the capsule should also be drawn.</param>
		/// <param name="antialiased">Whether the wireframe should be anti-aliased.</param>
		/// <returns><see langword="void"/></returns>
		private static void _DrawCapsuleImpl( Transform3D start, Transform3D end, float radius, Color color, float thickness, int resolution, bool contour, bool antialiased )
		{
			// If there is any actual height to the capsule...
			if ( start.Origin.DistanceTo( end.Origin ) > 0f )
			{
				// Calculate the transforms for the ends of the capsule
				Transform3D startFacingEnd = start.LookingAt( end.Origin, start.Basis.Z.Cross( start.Basis.Y ) ).RotatedLocal( Vector3.Right, _NinetyDegreesInRadians );
				Transform3D endFacingStart = end.LookingAt( start.Origin, end.Basis.Z.Cross( end.Basis.Y ) ).RotatedLocal( Vector3.Left, -_NinetyDegreesInRadians );

				// Draw the end caps for the capsule
				_DrawHemisphereImpl( startFacingEnd, radius, color, thickness, resolution, false, antialiased );
				_DrawHemisphereImpl( endFacingStart, radius, color, thickness, resolution, false, antialiased );

				// Draw the lines connecting the end caps
				_DrawLineImpl(
					startFacingEnd.TranslatedLocal( Vector3.Right * radius ).Origin,
					endFacingStart.TranslatedLocal( Vector3.Left * radius ).Origin,
					color,
					thickness,
					antialiased
				);
				_DrawLineImpl(
					startFacingEnd.TranslatedLocal( Vector3.Left * radius ).Origin,
					endFacingStart.TranslatedLocal( Vector3.Right * radius ).Origin,
					color,
					thickness,
					antialiased
				);
				_DrawLineImpl(
					startFacingEnd.TranslatedLocal( Vector3.Back * radius ).Origin,
					endFacingStart.TranslatedLocal( Vector3.Back * radius ).Origin,
					color,
					thickness,
					antialiased
				);
				_DrawLineImpl(
					startFacingEnd.TranslatedLocal( Vector3.Forward * radius ).Origin,
					endFacingStart.TranslatedLocal( Vector3.Forward * radius ).Origin,
					color,
					thickness,
					antialiased
				);

				// Maybe draw the contour of the capsule from the perspective of the camera...
				if ( contour )
				{
					// Update some values for comparison
					float height = start.Origin.DistanceTo( end.Origin ) + ( radius * 2f );
					if ( ! IsEqualApprox( _CapsuleMesh.Radius, radius ) ) _CapsuleMesh.Radius = radius;
					if ( ! IsEqualApprox( _CapsuleMesh.Height, height ) ) _CapsuleMesh.Height = height;
					if ( _CapsuleMesh.Rings != resolution ) _CapsuleMesh.Rings = resolution;
					if ( _CapsuleMesh.RadialSegments != resolution ) _CapsuleMesh.RadialSegments = resolution;
					Transform3D meshTrans = new( startFacingEnd.Basis, ( start.Origin + end.Origin ) / 2f );

					// If there has been any changes that would change the capsule hull...
					if (
						! IsEqualApprox( _CapsuleMesh.Radius, _PrevCapsuleMesh.Radius )
						|| ! IsEqualApprox( _CapsuleMesh.Height, _PrevCapsuleMesh.Height )
						|| ( _CapsuleMesh.Rings != _PrevCapsuleMesh.Rings )
						|| ( _CapsuleMesh.RadialSegments != _PrevCapsuleMesh.RadialSegments )
						|| ! meshTrans.IsEqualApprox( _PrevCapsuleMeshTrans )
						|| ! _Camera.GlobalTransform.IsEqualApprox( _PrevCapsuleCamTrans )
					)
					{
						// Calculate the hull for the capsule in its new state
						_CapsuleHull = _ConvexHullFromMeshInView(
							_CapsuleMesh,
							meshTrans,
							_Camera
						);

						// Cache values for comparison next frame
						_PrevCapsuleMesh = _CapsuleMesh;
						_PrevCapsuleMeshTrans = meshTrans;
						_PrevCapsuleCamTrans = _Camera.GlobalTransform;
					}

					// If there is anything to actually draw...
					if ( _CapsuleHull.Length > 2 )
					{
						// Draw the outline of the hull
						_Canvas.DrawPolyline(
							_CapsuleHull,
							color,
							thickness,
							antialiased
						);
					}
				}
			}
			else
			{
				// Otherwise, just draw a sphere
				_DrawSphereImpl( start, radius, color, thickness, resolution, contour, antialiased );
			}
		}

		/// <summary>
		/// Internal helper method that draws a wireframe capsule shape in the game for debug visualization.
		/// </summary>
		/// <param name="transform">The <see cref="Transform3D"/> where the capsule will be located at.</param>
		/// <param name="height">The height of the capsule, not including the hemisphere caps.</param>
		/// <param name="radius">The radius of the capsule.</param>
		/// <param name="color">The <see cref="Color"/> that the capsule will be drawn in.</param>
		/// <param name="thickness">The thickness of the lines that make up the wireframe.</param>
		/// <param name="resolution">The resolution of the capsule.</param>
		/// <param name="contour">Whether the contour of the capsule should also be drawn.</param>
		/// <param name="antialiased">Whether the wireframe should be anti-aliased.</param>
		/// <returns><see langword="void"/></returns>
		private static void _DrawCapsuleImpl( Transform3D transform, float height, float radius, Color color, float thickness, int resolution, bool contour, bool antialiased )
		{
			float halfHeight = height / 2f;

			_DrawCapsuleImpl(
				transform.TranslatedLocal( Vector3.Up * halfHeight ),
				transform.TranslatedLocal( Vector3.Down * halfHeight ),
				radius,
				color,
				thickness,
				resolution,
				contour,
				antialiased
			);
		}

		/// <summary>
		/// Internal helper method that draws a wireframe square shape in the game for debug visualization.
		/// </summary>
		/// <param name="transform">The <see cref="Transform3D"/> where the square will be located at.</param>
		/// <param name="height">The height of the square.</param>
		/// <param name="width">The width of the square.</param>
		/// <param name="color">The <see cref="Color"/> that the square will be drawn in.</param>
		/// <param name="thickness">The thickness of the lines that make up the square.</param>
		/// <param name="antialiased">Whether the square should be anti-aliased.</param>
		/// <returns><see langword="void"/></returns>
		private static void _DrawSquareImpl( Transform3D transform, float height, float width, Color color, float thickness, bool antialiased )
		{
			// Calculate some values for the square
			float halfHeight = height / 2f;
			float halfWidth = width / 2f;

			// Draw the four lines that make up the square
			_DrawLineImpl(
				transform.TranslatedLocal( new Vector3( -halfWidth, halfHeight, 0f ) ).Origin,
				transform.TranslatedLocal( new Vector3( halfWidth, halfHeight, 0f ) ).Origin,
				color,
				thickness,
				antialiased
			);
			_DrawLineImpl(
				transform.TranslatedLocal( new Vector3( -halfWidth, -halfHeight, 0f ) ).Origin,
				transform.TranslatedLocal( new Vector3( halfWidth, -halfHeight, 0f ) ).Origin,
				color,
				thickness,
				antialiased
			);
			_DrawLineImpl(
				transform.TranslatedLocal( new Vector3( -halfWidth, -halfHeight, 0f ) ).Origin,
				transform.TranslatedLocal( new Vector3( -halfWidth, halfHeight, 0f ) ).Origin,
				color,
				thickness,
				antialiased
			);
			_DrawLineImpl(
				transform.TranslatedLocal( new Vector3( halfWidth, -halfHeight, 0f ) ).Origin,
				transform.TranslatedLocal( new Vector3( halfWidth, halfHeight, 0f ) ).Origin,
				color,
				thickness,
				antialiased
			);
		}

		/// <summary>
		/// Internal helper method that draws a wireframe cube shape in the game for debug visualization.
		/// </summary>
		/// <param name="transform">The <see cref="Transform3D"/> where the cube will be located at.</param>
		/// <param name="height">The height of the cube.</param>
		/// <param name="width">The width of the cube.</param>
		/// <param name="depth">The depth of the cube.</param>
		/// <param name="color">The <see cref="Color"/> that the cube will be drawn in.</param>
		/// <param name="thickness">The thickness of the lines that make up the cube.</param>
		/// <param name="antialiased">Whether the cube should be anti-aliased.</param>
		/// <returns><see langword="void"/></returns>
		private static void _DrawCubeImpl( Transform3D transform, float height, float width, float depth, Color color, float thickness, bool antialiased )
		{
			// Calculate some values for the cube
			float halfHeight = height / 2f;
			float halfWidth = width / 2f;
			float halfDepth = depth / 2f;

			// Draw two squares representing the front and back of the cube
			_DrawSquareImpl(
				transform.TranslatedLocal( Vector3.Back * halfDepth ),
				height,
				width,
				color,
				thickness,
				antialiased
			);
			_DrawSquareImpl(
				transform.TranslatedLocal( Vector3.Forward * halfDepth ),
				height,
				width,
				color,
				thickness,
				antialiased
			);

			// Draw lines connecting the front and back of the cube
			_DrawLineImpl(
				transform.TranslatedLocal( new Vector3( -halfWidth, halfHeight, -halfDepth ) ).Origin,
				transform.TranslatedLocal( new Vector3( -halfWidth, halfHeight, halfDepth ) ).Origin,
				color,
				thickness,
				antialiased
			);
			_DrawLineImpl(
				transform.TranslatedLocal( new Vector3( halfWidth, halfHeight, -halfDepth ) ).Origin,
				transform.TranslatedLocal( new Vector3( halfWidth, halfHeight, halfDepth ) ).Origin,
				color,
				thickness,
				antialiased
			);
			_DrawLineImpl(
				transform.TranslatedLocal( new Vector3( -halfWidth, -halfHeight, -halfDepth ) ).Origin,
				transform.TranslatedLocal( new Vector3( -halfWidth, -halfHeight, halfDepth ) ).Origin,
				color,
				thickness,
				antialiased
			);
			_DrawLineImpl(
				transform.TranslatedLocal( new Vector3( halfWidth, -halfHeight, -halfDepth ) ).Origin,
				transform.TranslatedLocal( new Vector3( halfWidth, -halfHeight, halfDepth ) ).Origin,
				color,
				thickness,
				antialiased
			);
		}

		/// <summary>
		/// Internal helper method that draws a wireframe scalene triangle shape in the game for debug visualization.
		/// </summary>
		/// <param name="transform">The <see cref="Transform3D"/> where the triangle will be located at.</param>
		/// <param name="lengthA">The length of the first side of the triangle.</param>
		/// <param name="lengthB">The length of the second side of the triangle.</param>
		/// <param name="lengthC">The length of the third side of the triangle.</param>
		/// <param name="color">The <see cref="Color"/> that the triangle will be drawn in.</param>
		/// <param name="thickness">The thickness of the lines that make up the triangle.</param>
		/// <param name="antialiased">Whether the triangle should be anti-aliased.</param>
		/// <returns><see langword="void"/></returns>
		private static void _DrawScaleneTriangleImpl( Transform3D transform, float lengthA, float lengthB, float lengthC, Color color, float thickness, bool antialiased )
		{
			_DrawPolylineImpl(
				_TrianglePoints( transform, lengthA, lengthB, lengthC ),
				color,
				thickness,
				antialiased
			);
		}

		/// <summary>
		/// Internal helper method that draws a wireframe equilateral triangle shape in the game for debug visualization.
		/// </summary>
		/// <param name="transform">The <see cref="Transform3D"/> where the triangle will be located at.</param>
		/// <param name="length">The length of all sides of the triangle.</param>
		/// <param name="color">The <see cref="Color"/> that the triangle will be drawn in.</param>
		/// <param name="thickness">The thickness of the lines that make up the triangle.</param>
		/// <param name="antialiased">Whether the triangle should be anti-aliased.</param>
		/// <returns><see langword="void"/></returns>
		private static void _DrawEquilateralTriangleImpl( Transform3D transform, float length, Color color, float thickness, bool antialiased )
		{
			_DrawScaleneTriangleImpl( transform, length, length, length, color, thickness, antialiased );
		}

		/// <summary>
		/// Internal helper method that draws a wireframe pyramid shape in the game for debug visualization.
		/// </summary>
		/// <param name="transform">The <see cref="Transform3D"/> where the pyramid will be located at.</param>
		/// <param name="height">The height of the pyramid.</param>
		/// <param name="width">The width of the pyramid.</param>
		/// <param name="depth">The depth of the pyramid.</param>
		/// <param name="color">The <see cref="Color"/> that the pyramid will be drawn in.</param>
		/// <param name="thickness">The thickness of the lines that make up the pyramid.</param>
		/// <param name="antialiased">Whether the pyramid should be anti-aliased.</param>
		/// <returns><see langword="void"/></returns>
		private static void _DrawPyramidImpl( Transform3D transform, float height, float width, float depth, Color color, float thickness, bool antialiased )
		{
			// Calculate some values for the pyramid
			float halfHeight = height / 2f;
			float halfWidth = width / 2f;
			float halfDepth = depth / 2f;

			// Draw a square for the base of the pyramid
			_DrawSquareImpl(
				transform.TranslatedLocal( Vector3.Down * halfHeight ).RotatedLocal( Vector3.Right, _NinetyDegreesInRadians ),
				width,
				depth,
				color,
				thickness,
				antialiased
			);

			// Draw the lines that meet at the peak of the pyramid
			_DrawLineImpl(
				transform.TranslatedLocal( new Vector3( -halfWidth, -halfHeight, -halfDepth ) ).Origin,
				transform.TranslatedLocal( new Vector3( 0f, halfHeight, 0f ) ).Origin,
				color,
				thickness,
				antialiased
			);
			_DrawLineImpl(
				transform.TranslatedLocal( new Vector3( halfWidth, -halfHeight, -halfDepth ) ).Origin,
				transform.TranslatedLocal( new Vector3( 0f, halfHeight, 0f ) ).Origin,
				color,
				thickness,
				antialiased
			);
			_DrawLineImpl(
				transform.TranslatedLocal( new Vector3( -halfWidth, -halfHeight, halfDepth ) ).Origin,
				transform.TranslatedLocal( new Vector3( 0f, halfHeight, 0f ) ).Origin,
				color,
				thickness,
				antialiased
			);
			_DrawLineImpl(
				transform.TranslatedLocal( new Vector3( halfWidth, -halfHeight, halfDepth ) ).Origin,
				transform.TranslatedLocal( new Vector3( 0f, halfHeight, 0f ) ).Origin,
				color,
				thickness,
				antialiased
			);
		}

		/// <summary>
		/// Internal helper method that draws a wireframe cone shape in the game for debug visualization.
		/// </summary>
		/// <param name="transform">The <see cref="Transform3D"/> where the cone will be located at.</param>
		/// <param name="height">The height of the cone.</param>
		/// <param name="radius">The radius of the cone.</param>
		/// <param name="color">The <see cref="Color"/> that the cone will be drawn in.</param>
		/// <param name="thickness">The thickness of the lines that make up the cone.</param>
		/// <param name="resolution">The resolution of the cone.</param>
		/// <param name="contour">Whether the contour of the cone should also be drawn.</param>
		/// <param name="antialiased">Whether the cone should be anti-aliased.</param>
		/// <returns><see langword="void"/></returns>
		private static void _DrawConeImpl( Transform3D transform, float height, float radius, Color color, float thickness, int resolution, bool contour, bool antialiased )
		{
			// Calculate some values for the cone
			float halfHeight = height / 2f;

			// Draw a circle for the base of the cone
			_DrawCircleImpl(
				transform.TranslatedLocal( Vector3.Down * halfHeight ).RotatedLocal( Vector3.Right, _NinetyDegreesInRadians ),
				radius,
				color,
				thickness,
				resolution,
				antialiased
			);

			// Draw the lines that meet at the peak of the cone
			_DrawLineImpl(
				transform.TranslatedLocal( new Vector3( 0f, -halfHeight, -radius ) ).Origin,
				transform.TranslatedLocal( new Vector3( 0f, halfHeight, 0f ) ).Origin,
				color,
				thickness,
				antialiased
			);
			_DrawLineImpl(
				transform.TranslatedLocal( new Vector3( 0f, -halfHeight, radius ) ).Origin,
				transform.TranslatedLocal( new Vector3( 0f, halfHeight, 0f ) ).Origin,
				color,
				thickness,
				antialiased
			);
			_DrawLineImpl(
				transform.TranslatedLocal( new Vector3( -radius, -halfHeight, 0f ) ).Origin,
				transform.TranslatedLocal( new Vector3( 0f, halfHeight, 0f ) ).Origin,
				color,
				thickness,
				antialiased
			);
			_DrawLineImpl(
				transform.TranslatedLocal( new Vector3( radius, -halfHeight, 0f ) ).Origin,
				transform.TranslatedLocal( new Vector3( 0f, halfHeight, 0f ) ).Origin,
				color,
				thickness,
				antialiased
			);

			// Maybe draw the contour of the cone from the perspective of the camera
			if ( contour )
			{
				// Update some values for comparison
				if ( ! IsEqualApprox( _ConeMesh.Height, height ) ) _ConeMesh.Height = height;
				if ( ! IsEqualApprox( _ConeMesh.BottomRadius, radius ) ) _ConeMesh.BottomRadius = radius;
				if ( _ConeMesh.Rings != resolution ) _ConeMesh.Rings = resolution;
				if ( _ConeMesh.RadialSegments != resolution ) _ConeMesh.RadialSegments = resolution;

				// If there has been any changes that would change the cone hull...
				if (
					! IsEqualApprox( _ConeMesh.Height, _PrevConeMesh.Height )
					|| ! IsEqualApprox( _ConeMesh.BottomRadius, _PrevConeMesh.BottomRadius )
					|| ( _ConeMesh.Rings != _PrevConeMesh.Rings )
					|| ( _ConeMesh.RadialSegments != _PrevConeMesh.RadialSegments )
					|| ! transform.IsEqualApprox( _PrevConeMeshTrans )
					|| ! _Camera.GlobalTransform.IsEqualApprox( _PrevConeCamTrans )
				)
				{
					// Calculate the hull for the cone in its new state
					_ConeHull = _ConvexHullFromMeshInView(
						_ConeMesh,
						transform,
						_Camera
					);

					// Cache values for comparison next frame
					_PrevConeMesh = _ConeMesh;
					_PrevConeMeshTrans = transform;
					_PrevConeCamTrans = _Camera.GlobalTransform;
				}

				// If there is anything to actually draw...
				if ( _ConeHull.Length > 2 )
				{
					// Draw the outline of the hull
					_Canvas.DrawPolyline(
						_ConeHull,
						color,
						thickness,
						antialiased
					);
				}
			}
		}

		/// <summary>
		/// Internal helper method that draws some screen text in the game for debug visualization.
		/// </summary>
		/// <param name="text">The text to draw.</param>
		/// <param name="color">The <see cref="Color"/> that the text will be drawn in.</param>
		/// <param name="size">The font size of the text.</param>
		/// <param name="position">The <see cref="Vector2">position</see> on the screen where the text will be drawn.</param>
		/// <returns><see langword="void"/></returns>
		private static void _DrawTextImpl( object text, Color color, int size, Vector2 position )
		{
			// Get the string value of the passed "text" object
			string txt = text.ToString();

			// The text will be positioned offset from the upper-left corner of the screen
			Vector2 pos = ( Vector2.Down * size ) + position;

			// Draw an outline around the text for better contrast
			_Canvas.DrawMultilineStringOutline( _Font, pos, txt, HorizontalAlignment.Left, _Window.Size.X, size, -1, 2, Colors.Black );

			// Draw the text
			_Canvas.DrawMultilineString( _Font, pos, txt, HorizontalAlignment.Left, _Window.Size.X, size, -1, color );
		}

		/// <summary>
		/// Computes the centroid of a 3D polygon.
		/// </summary>
		/// <param name="points">The <see cref="Vector3">points</see> that define the polygon.</param>
		/// <returns>A <see cref="Vector3"/> that represents the centroid point.</returns>
		private static Vector3 _CalculateCentroid( Vector3[] points )
		{
			Vector3 s = Vector3.Zero;
			float areaTotal = 0f;

			Vector3 p1 = points[ 0 ];
			Vector3 p2 = points[ 1 ];

			for ( int i = 2; i < points.Length; i++ )
			{
				Vector3 p3 = points[ i ];
				float area = ( p3 - p1 ).Cross( p3 - p2 ).Length() / 2f;

				s.X += area * ( p1.X + p2.X + p3.X ) / 3f;
				s.Y += area * ( p1.Y + p2.Y + p3.Y ) / 3f;
				s.Z += area * ( p1.Z + p2.Z + p3.Z ) / 3f;

				areaTotal += area;
				p2 = p3;
			}

			return new( s.X / areaTotal, s.Y / areaTotal, s.Z / areaTotal );
		}

		/// <summary>
		/// Generates an array of points along an arc defined by the specified parameters, transformed by the given 3D transform.
		/// </summary>
		/// <param name="transform">The <see cref="Transform3D"/> object used to transform the points.</param>
		/// <param name="start">The starting angle of the arc in degrees.</param>
		/// <param name="end">The sweep angle of the arc in degrees.</param>
		/// <param name="radiusX">The X radius of the arc.</param>
		/// <param name="radiusY">The Y radius of the arc.</param>
		/// <param name="resolution">The number of points to generate along the arc (optional, default value is <see langword="32"/>).</param>
		/// <returns>An array of <see cref="Vector3"/> points that represent positions along the arc.</returns>
		private static Vector3[] _ArcPoints( Transform3D transform, float start, float end, float radiusX, float radiusY, int resolution = 32 )
		{
			Vector3[] points = new Vector3[ resolution + 1 ];
			float a = start;

			for ( int i = 0; i < ( resolution + 1 ); i++ )
			{
				points[ i ] = transform.TranslatedLocal(
					new Vector3(
						Sin( DegToRad( a ) ) * radiusX,
						Cos( DegToRad( a ) ) * radiusY,
						0f
					)
				).Origin;
				a += end / resolution;
			}

			return points;
		}

		/// <summary>
		/// Generates an array of points that represent a triangle that has sides with the given lengths, transformed by the given 3D transform.
		/// </summary>
		/// <param name="transform">The <see cref="Transform3D"/> object used to transform the points.</param>
		/// <param name="lengthA">The length of the first side of the triangle.</param>
		/// <param name="lengthB">The length of the second side of the triangle.</param>
		/// <param name="lengthC">The length of the third side of the triangle.</param>
		/// <returns>An array of <see cref="Vector3"/> points that represent a triangle with sides that have the given lengths.</returns>
		private static Vector3[] _TrianglePoints( Transform3D transform, float lengthA, float lengthB, float lengthC )
		{
			float angle = Acos( ( ( lengthC * lengthC ) + ( lengthA * lengthA ) - ( lengthB * lengthB ) ) / ( 2 * lengthC * lengthA ) );

			Vector3[] points = new[] {
				Vector3.Zero,
				Vector3.Right * lengthC,
				new Vector3( lengthA * Cos( angle ), lengthA * Sin( angle ), 0f ),
				Vector3.Zero
			};

			Vector3 center = _CalculateCentroid( points );
			return points.Select( p => transform.TranslatedLocal( p - center ).Origin ).ToArray();
		}

		/// <summary>
		/// Computes the convex hull of a 3D mesh projected onto the 2D view space defined by the camera.
		/// </summary>
		/// <param name="mesh">The <see cref="Mesh"/> to compute the convex hull from.</param>
		/// <param name="transform">The <see cref="Transform3D"/> applied to the mesh vertices.</param>
		/// <param name="camera">The <see cref="Camera3D"/> used for the projection.</param>
		/// <returns>An array of <see cref="Vector2"/> representing the convex hull in the view space.</returns>
		private static Vector2[] _ConvexHullFromMeshInView( Mesh mesh, Transform3D transform, Camera3D camera )
		{
			return Geometry2D.ConvexHull(
				( (Vector3[]) mesh.SurfaceGetArrays( 0 )[ (int) Mesh.ArrayType.Vertex ] ).Select(
					v => camera.UnprojectPosition( transform.TranslatedLocal( v ).Origin )
				).ToArray()
			);
		}
		#endregion

		#region Exposed Methods
		/// <summary>
		/// Draws a point shape in the game for debug visualization.
		/// </summary>
		/// <remarks>
		/// Note: The point will be drawn exactly when called, not at a fixed interval or fps. So for example, calling
		/// this from <see cref="Node._PhysicsProcess"/> will make the point appear to lag because
		/// <see cref="Node._PhysicsProcess"/> happens at a different timestep than regular screen updates.
		/// </remarks>
		/// <param name="position">The <see cref="Vector3">position</see> where the point will be drawn.</param>
		/// <param name="size">The size of the point. (Optional, defaults to <see langword="5"/>)</param>
		/// <param name="color">The <see cref="Color"/> that the point will be drawn in. (Optional, defaults to <see cref="Colors.Magenta"/>)</param>
		/// <param name="antialiased">Whether the point should be anti-aliased. (Optional, defaults to <see langword="true"/>)</param>
		/// <returns><see langword="void"/></returns>
		public static void DrawPoint( Vector3 position, float size = 5f, Color? color = null, bool antialiased = true )
		{
			_Draw( () => _DrawPointImpl(
				position,
				size,
				color is null ? Colors.Magenta : (Color) color,
				antialiased
			) );
		}

		/// <summary>
		/// Draws a line shape in the game for debug visualization.
		/// </summary>
		/// <remarks>
		/// Note: The line will be drawn exactly when called, not at a fixed interval or fps. So for example, calling
		/// this from <see cref="Node._PhysicsProcess"/> will make the line appear to lag because
		/// <see cref="Node._PhysicsProcess"/> happens at a different timestep than regular screen updates.
		/// </remarks>
		/// <param name="start">The <see cref="Vector3">position</see> where the line will start drawing from.</param>
		/// <param name="end">The <see cref="Vector3">position</see> where the line will end drawing at.</param>
		/// <param name="color">The <see cref="Color"/> that the line will be drawn in. (Optional, defaults to <see cref="Colors.Magenta"/>)</param>
		/// <param name="thickness">The thickness of the line. (Optional, defaults to <see langword="0.5"/>)</param>
		/// <param name="antialiased">Whether the line should be anti-aliased. (Optional, defaults to <see langword="true"/>)</param>
		/// <returns><see langword="void"/></returns>
		public static void DrawLine( Vector3 start, Vector3 end, Color? color = null, float thickness = 0.5f, bool antialiased = true )
		{
			_Draw( () => _DrawLineImpl(
				start,
				end,
				color is null ? Colors.Magenta : (Color) color,
				thickness,
				antialiased
			) );
		}

		/// <summary>
		/// Draws a direction line shape in the game for debug visualization.
		/// </summary>
		/// <remarks>
		/// Note: The direction will be drawn exactly when called, not at a fixed interval or fps. So for example, calling
		/// this from <see cref="Node._PhysicsProcess"/> will make the direction appear to lag because
		/// <see cref="Node._PhysicsProcess"/> happens at a different timestep than regular screen updates.
		/// </remarks>
		/// <param name="position">The <see cref="Vector3">position</see> of the direction line.</param>
		/// <param name="direction">The <see cref="Vector3">direction</see> that the line will point in.</param>
		/// <param name="color">The <see cref="Color"/> that the direction line will be drawn in. (Optional, defaults to <see cref="Colors.Magenta"/>)</param>
		/// <param name="thickness">The thickness of the direction line. (Optional, defaults to <see langword="0.5"/>)</param>
		/// <param name="antialiased">Whether the direction line should be anti-aliased. (Optional, defaults to <see langword="true"/>)</param>
		/// <returns><see langword="void"/></returns>
		public static void DrawDirection( Vector3 position, Vector3 direction, Color? color = null, float thickness = 0.5f, bool antialiased = true )
		{
			_Draw( () => _DrawDirectionImpl(
				position,
				direction,
				color is null ? Colors.Magenta : (Color) color,
				thickness,
				antialiased
			) );
		}

		/// <summary>
		/// Draws a polyline shape in the game for debug visualization.
		/// </summary>
		/// <remarks>
		/// Note: The polyline will be drawn exactly when called, not at a fixed interval or fps. So for example, calling
		/// this from <see cref="Node._PhysicsProcess"/> will make the polyline appear to lag because
		/// <see cref="Node._PhysicsProcess"/> happens at a different timestep than regular screen updates.
		/// </remarks>
		/// <param name="points">Any array of <see cref="Vector3">vectors</see> representing the points that the polyline will be drawn through.</param>
		/// <param name="color">The <see cref="Color"/> that the polyline will be drawn in. (Optional, defaults to <see cref="Colors.Magenta"/>)</param>
		/// <param name="thickness">The thickness of the polyline. (Optional, defaults to <see langword="0.5"/>)</param>
		/// <param name="antialiased">Whether the polyline should be anti-aliased. (Optional, defaults to <see langword="true"/>)</param>
		/// <returns><see langword="void"/></returns>
		public static void DrawPolyline( Vector3[] points, Color? color = null, float thickness = 0.5f, bool antialiased = true )
		{
			_Draw( () => _DrawPolylineImpl(
				points,
				color is null ? Colors.Magenta : (Color) color,
				thickness,
				antialiased
			) );
		}

		/// <summary>
		/// Draws an arc shape in the game for debug visualization.
		/// </summary>
		/// <remarks>
		/// Note: The arc will be drawn exactly when called, not at a fixed interval or fps. So for example, calling
		/// this from <see cref="Node._PhysicsProcess"/> will make the arc appear to lag because
		/// <see cref="Node._PhysicsProcess"/> happens at a different timestep than regular screen updates.
		/// </remarks>
		/// <param name="transform">The <see cref="Transform3D"/> where the arc will be located at.</param>
		/// <param name="start">The angle that the arc starts at.</param>
		/// <param name="end">The angle that the arc ends at.</param>
		/// <param name="radiusX">The radius on the X axis of the arc. (Optional, defaults to <see langword="0.5"/>)</param>
		/// <param name="radiusY">The radius on the Y axis of the arc. (Optional, defaults to <see langword="0.5"/>)</param>
		/// <param name="color">The <see cref="Color"/> that the arc will be drawn in. (Optional, defaults to <see cref="Colors.Magenta"/>)</param>
		/// <param name="thickness">The thickness of the lines that make up the arc. (Optional, defaults to <see langword="0.5"/>)</param>
		/// <param name="resolution">The resolution of the arc. (Optional, defaults to <see langword="32"/>)</param>
		/// <param name="antialiased">Whether the arc should be anti-aliased. (Optional, defaults to <see langword="true"/>)</param>
		/// <returns><see langword="void"/></returns>
		public static void DrawArc( Transform3D transform, float start, float end, float radiusX = 0.5f, float radiusY = 0.5f, Color? color = null, float thickness = 0.5f, int resolution = 32, bool antialiased = true )
		{
			_Draw( () => _DrawArcImpl(
				transform,
				start,
				end,
				radiusX,
				radiusY,
				color is null ? Colors.Magenta : (Color) color,
				thickness,
				resolution,
				antialiased
			) );
		}

		/// <summary>
		/// Draws an ellipse shape in the game for debug visualization.
		/// </summary>
		/// <remarks>
		/// Note: The ellipse will be drawn exactly when called, not at a fixed interval or fps. So for example, calling
		/// this from <see cref="Node._PhysicsProcess"/> will make the ellipse appear to lag because
		/// <see cref="Node._PhysicsProcess"/> happens at a different timestep than regular screen updates.
		/// </remarks>
		/// <param name="transform">The <see cref="Transform3D"/> where the ellipse will be located at.</param>
		/// <param name="radiusX">The radius on the X axis of the ellipse. (Optional, defaults to <see langword="0.5"/>)</param>
		/// <param name="radiusY">The radius on the Y axis of the ellipse. (Optional, defaults to <see langword="0.5"/>)</param>
		/// <param name="color">The <see cref="Color"/> that the ellipse will be drawn in. (Optional, defaults to <see cref="Colors.Magenta"/>)</param>
		/// <param name="thickness">The thickness of the lines that make up the ellipse. (Optional, defaults to <see langword="0.5"/>)</param>
		/// <param name="resolution">The resolution of the ellipse. (Optional, defaults to <see langword="32"/>)</param>
		/// <param name="antialiased">Whether the ellipse should be anti-aliased. (Optional, defaults to <see langword="true"/>)</param>
		/// <returns><see langword="void"/></returns>
		public static void DrawEllipse( Transform3D transform, float radiusX = 0.5f, float radiusY = 0.5f, Color? color = null, float thickness = 0.5f, int resolution = 32, bool antialiased = true )
		{
			_Draw( () => _DrawEllipseImpl(
				transform,
				radiusX,
				radiusY,
				color is null ? Colors.Magenta : (Color) color,
				thickness,
				resolution,
				antialiased
			) );
		}

		/// <summary>
		/// Draws a circle shape in the game for debug visualization.
		/// </summary>
		/// <remarks>
		/// Note: The circle will be drawn exactly when called, not at a fixed interval or fps. So for example, calling
		/// this from <see cref="Node._PhysicsProcess"/> will make the circle appear to lag because
		/// <see cref="Node._PhysicsProcess"/> happens at a different timestep than regular screen updates.
		/// </remarks>
		/// <param name="transform">The <see cref="Transform3D"/> where the circle will be located at.</param>
		/// <param name="radius">The radius of the circle. (Optional, defaults to <see langword="0.5"/>)</param>
		/// <param name="color">The <see cref="Color"/> that the circle will be drawn in. (Optional, defaults to <see cref="Colors.Magenta"/>)</param>
		/// <param name="thickness">The thickness of the lines that make up the circle. (Optional, defaults to <see langword="0.5"/>)</param>
		/// <param name="resolution">The resolution of the circle. (Optional, defaults to <see langword="32"/>)</param>
		/// <param name="antialiased">Whether the circle should be anti-aliased. (Optional, defaults to <see langword="true"/>)</param>
		/// <returns><see langword="void"/></returns>
		public static void DrawCircle( Transform3D transform, float radius = 0.5f, Color? color = null, float thickness = 0.5f, int resolution = 32, bool antialiased = true )
		{
			_Draw( () => _DrawCircleImpl(
				transform,
				radius,
				color is null ? Colors.Magenta : (Color) color,
				thickness,
				resolution,
				antialiased
			) );
		}

		/// <summary>
		/// Draws a wireframe hemisphere shape in the game for debug visualization.
		/// </summary>
		/// <remarks>
		/// Note: The hemisphere will be drawn exactly when called, not at a fixed interval or fps. So for example, calling
		/// this from <see cref="Node._PhysicsProcess"/> will make the hemisphere appear to lag because
		/// <see cref="Node._PhysicsProcess"/> happens at a different timestep than regular screen updates.
		/// </remarks>
		/// <param name="transform">The <see cref="Transform3D"/> where the hemisphere will be located at.</param>
		/// <param name="radius">The radius of the hemisphere. (Optional, defaults to <see langword="0.5"/>)</param>
		/// <param name="color">The <see cref="Color"/> that the hemisphere will be drawn in. (Optional, defaults to <see cref="Colors.Magenta"/>)</param>
		/// <param name="thickness">The thickness of the lines that make up the wireframe. (Optional, defaults to <see langword="0.5"/>)</param>
		/// <param name="resolution">The resolution of the hemisphere. (Optional, defaults to <see langword="32"/>)</param>
		/// <param name="contour">Whether the contour of the hemisphere should also be drawn. (Optional, defaults to <see langword="true"/>)</param>
		/// <param name="antialiased">Whether the wireframe should be anti-aliased. (Optional, defaults to <see langword="true"/>)</param>
		/// <returns><see langword="void"/></returns>
		public static void DrawHemisphere( Transform3D transform, float radius = 0.5f, Color? color = null, float thickness = 0.5f, int resolution = 32, bool contour = true, bool antialiased = true )
		{
			_Draw( () => _DrawHemisphereImpl(
				transform,
				radius,
				color is null ? Colors.Magenta : (Color) color,
				thickness,
				resolution,
				contour,
				antialiased
			) );
		}

		/// <summary>
		/// Draws a wireframe sphere shape in the game for debug visualization.
		/// </summary>
		/// <remarks>
		/// Note: The sphere will be drawn exactly when called, not at a fixed interval or fps. So for example, calling
		/// this from <see cref="Node._PhysicsProcess"/> will make the sphere appear to lag because
		/// <see cref="Node._PhysicsProcess"/> happens at a different timestep than regular screen updates.
		/// </remarks>
		/// <param name="transform">The <see cref="Transform3D"/> where the sphere will be located at.</param>
		/// <param name="radius">The radius of the sphere. (Optional, defaults to <see langword="0.5"/>)</param>
		/// <param name="color">The <see cref="Color"/> that the sphere will be drawn in. (Optional, defaults to <see cref="Colors.Magenta"/>)</param>
		/// <param name="thickness">The thickness of the lines that make up the wireframe. (Optional, defaults to <see langword="0.5"/>)</param>
		/// <param name="resolution">The resolution of the sphere. (Optional, defaults to <see langword="32"/>)</param>
		/// <param name="contour">Whether the contour of the sphere should also be drawn. (Optional, defaults to <see langword="true"/>)</param>
		/// <param name="antialiased">Whether the wireframe should be anti-aliased. (Optional, defaults to <see langword="true"/>)</param>
		/// <returns><see langword="void"/></returns>
		public static void DrawSphere( Transform3D transform, float radius = 0.5f, Color? color = null, float thickness = 0.5f, int resolution = 32, bool contour = true, bool antialiased = true )
		{
			_Draw( () => _DrawSphereImpl(
				transform,
				radius,
				color is null ? Colors.Magenta : (Color) color,
				thickness,
				resolution,
				contour,
				antialiased
			) );
		}

		/// <summary>
		/// Draws a wireframe cylinder shape in the game for debug visualization.
		/// </summary>
		/// <remarks>
		/// Note: The cylinder will be drawn exactly when called, not at a fixed interval or fps. So for example, calling
		/// this from <see cref="Node._PhysicsProcess"/> will make the cylinder appear to lag because
		/// <see cref="Node._PhysicsProcess"/> happens at a different timestep than regular screen updates.
		/// </remarks>
		/// <param name="start">The <see cref="Transform3D"/> where the cylinder will start from.</param>
		/// <param name="end">The <see cref="Transform3D"/> where the cylinder will end at.</param>
		/// <param name="topRadius">The radius of the top of the cylinder. (Optional, defaults to <see langword="0.5"/>)</param>
		/// <param name="bottomRadius">The radius of the bottom of the cylinder. (Optional, defaults to <see langword="0.5"/>)</param>
		/// <param name="color">The <see cref="Color"/> that the cylinder will be drawn in. (Optional, defaults to <see cref="Colors.Magenta"/>)</param>
		/// <param name="thickness">The thickness of the lines that make up the wireframe. (Optional, defaults to <see langword="1"/>)</param>
		/// <param name="resolution">The resolution of the cylinder. (Optional, defaults to <see langword="32"/>)</param>
		/// <param name="contour">Whether the contour of the cylinder should also be drawn. (Optional, defaults to <see langword="true"/>)</param>
		/// <param name="antialiased">Whether the wireframe should be anti-aliased. (Optional, defaults to <see langword="true"/>)</param>
		/// <returns><see langword="void"/></returns>
		public static void DrawCylinder( Transform3D start, Transform3D end, float topRadius = 0.5f, float bottomRadius = 0.5f, Color? color = null, float thickness = 0.5f, int resolution = 32, bool contour = true, bool antialiased = true )
		{
			_Draw( () => _DrawCylinderImpl(
				start,
				end,
				topRadius,
				bottomRadius,
				color is null ? Colors.Magenta : (Color) color,
				thickness,
				resolution,
				contour,
				antialiased
			) );
		}

		/// <summary>
		/// Draws a wireframe cylinder shape in the game for debug visualization.
		/// </summary>
		/// <remarks>
		/// Note: The cylinder will be drawn exactly when called, not at a fixed interval or fps. So for example, calling
		/// this from <see cref="Node._PhysicsProcess"/> will make the cylinder appear to lag because
		/// <see cref="Node._PhysicsProcess"/> happens at a different timestep than regular screen updates.
		/// </remarks>
		/// <param name="transform">The <see cref="Transform3D"/> where the cylinder will be located at.</param>
		/// <param name="height">The height of the cylinder.</param>
		/// <param name="topRadius">The radius of the top of the cylinder. (Optional, defaults to <see langword="0.5"/>)</param>
		/// <param name="bottomRadius">The radius of the bottom of the cylinder. (Optional, defaults to <see langword="0.5"/>)</param>
		/// <param name="color">The <see cref="Color"/> that the cylinder will be drawn in. (Optional, defaults to <see cref="Colors.Magenta"/>)</param>
		/// <param name="thickness">The thickness of the lines that make up the wireframe. (Optional, defaults to <see langword="1"/>)</param>
		/// <param name="resolution">The resolution of the cylinder. (Optional, defaults to <see langword="32"/>)</param>
		/// <param name="contour">Whether the contour of the cylinder should also be drawn. (Optional, defaults to <see langword="true"/>)</param>
		/// <param name="antialiased">Whether the wireframe should be anti-aliased. (Optional, defaults to <see langword="true"/>)</param>
		/// <returns><see langword="void"/></returns>
		public static void DrawCylinder( Transform3D transform, float height = 1f, float topRadius = 0.5f, float bottomRadius = 0.5f, Color? color = null, float thickness = 0.5f, int resolution = 32, bool contour = true, bool antialiased = true )
		{
			_Draw( () => _DrawCylinderImpl(
				transform,
				height,
				topRadius,
				bottomRadius,
				color is null ? Colors.Magenta : (Color) color,
				thickness,
				resolution,
				contour,
				antialiased
			) );
		}

		/// <summary>
		/// Draws a wireframe capsule shape in the game for debug visualization.
		/// </summary>
		/// <remarks>
		/// Note: The capsule will be drawn exactly when called, not at a fixed interval or fps. So for example, calling
		/// this from <see cref="Node._PhysicsProcess"/> will make the capsule appear to lag because
		/// <see cref="Node._PhysicsProcess"/> happens at a different timestep than regular screen updates.
		/// </remarks>
		/// <param name="start">The <see cref="Transform3D"/> where the capsule will start from.</param>
		/// <param name="end">The <see cref="Transform3D"/> where the capsule will end at.</param>
		/// <param name="radius">The radius of the capsule. (Optional, defaults to <see langword="0.5"/>)</param>
		/// <param name="color">The <see cref="Color"/> that the capsule will be drawn in. (Optional, defaults to <see cref="Colors.Magenta"/>)</param>
		/// <param name="thickness">The thickness of the lines that make up the wireframe. (Optional, defaults to <see langword="1"/>)</param>
		/// <param name="resolution">The resolution of the capsule. (Optional, defaults to <see langword="32"/>)</param>
		/// <param name="contour">Whether the contour of the capsule should also be drawn. (Optional, defaults to <see langword="true"/>)</param>
		/// <param name="antialiased">Whether the wireframe should be anti-aliased. (Optional, defaults to <see langword="true"/>)</param>
		/// <returns><see langword="void"/></returns>
		public static void DrawCapsule( Transform3D start, Transform3D end, float radius = 0.5f, Color? color = null, float thickness = 0.5f, int resolution = 32, bool contour = true, bool antialiased = true )
		{
			_Draw( () => _DrawCapsuleImpl(
				start,
				end,
				radius,
				color is null ? Colors.Magenta : (Color) color,
				thickness,
				resolution,
				contour,
				antialiased
			) );
		}

		/// <summary>
		/// Draws a wireframe capsule shape in the game for debug visualization.
		/// </summary>
		/// <remarks>
		/// Note: The capsule will be drawn exactly when called, not at a fixed interval or fps. So for example, calling
		/// this from <see cref="Node._PhysicsProcess"/> will make the capsule appear to lag because
		/// <see cref="Node._PhysicsProcess"/> happens at a different timestep than regular screen updates.
		/// </remarks>
		/// <param name="transform">The <see cref="Transform3D"/> where the capsule will be located at.</param>
		/// <param name="height">The height of the capsule, not including the hemisphere caps. (Optional, defaults to <see langword="1"/>)</param>
		/// <param name="radius">The radius of the capsule. (Optional, defaults to <see langword="0.5"/>)</param>
		/// <param name="color">The <see cref="Color"/> that the capsule will be drawn in. (Optional, defaults to <see cref="Colors.Magenta"/>)</param>
		/// <param name="thickness">The thickness of the lines that make up the wireframe. (Optional, defaults to <see langword="0.5"/>)</param>
		/// <param name="resolution">The resolution of the capsule. (Optional, defaults to <see langword="32"/>)</param>
		/// <param name="contour">Whether the contour of the capsule should also be drawn. (Optional, defaults to <see langword="true"/>)</param>
		/// <param name="antialiased">Whether the wireframe should be anti-aliased. (Optional, defaults to <see langword="true"/>)</param>
		/// <returns><see langword="void"/></returns>
		public static void DrawCapsule( Transform3D transform, float height = 1f, float radius = 0.5f, Color? color = null, float thickness = 0.5f, int resolution = 32, bool contour = true, bool antialiased = true )
		{
			_Draw( () => _DrawCapsuleImpl(
				transform,
				height,
				radius,
				color is null ? Colors.Magenta : (Color) color,
				thickness,
				resolution,
				contour,
				antialiased
			) );
		}

		/// <summary>
		/// Draws a square shape in the game for debug visualization.
		/// </summary>
		/// <remarks>
		/// Note: The square will be drawn exactly when called, not at a fixed interval or fps. So for example, calling
		/// this from <see cref="Node._PhysicsProcess"/> will make the square appear to lag because
		/// <see cref="Node._PhysicsProcess"/> happens at a different timestep than regular screen updates.
		/// </remarks>
		/// <param name="transform">The <see cref="Transform3D"/> where the square will be located at.</param>
		/// <param name="height">The height of the square. (Optional, defaults to <see langword="1"/>)</param>
		/// <param name="width">The width of the square. (Optional, defaults to <see langword="1"/>)</param>
		/// <param name="color">The <see cref="Color"/> that the square will be drawn in. (Optional, defaults to <see cref="Colors.Magenta"/>)</param>
		/// <param name="thickness">The thickness of the lines that make up the square. (Optional, defaults to <see langword="0.5"/>)</param>
		/// <param name="antialiased">Whether the square should be anti-aliased. (Optional, defaults to <see langword="true"/>)</param>
		/// <returns><see langword="void"/></returns>
		public static void DrawSquare( Transform3D transform, float height = 1f, float width = 1f, Color? color = null, float thickness = 0.5f, bool antialiased = true )
		{
			_Draw( () => _DrawSquareImpl(
				transform,
				height,
				width,
				color is null ? Colors.Magenta : (Color) color,
				thickness,
				antialiased
			) );
		}

		/// <summary>
		/// Draws a wireframe cube shape in the game for debug visualization.
		/// </summary>
		/// <remarks>
		/// Note: The cube will be drawn exactly when called, not at a fixed interval or fps. So for example, calling
		/// this from <see cref="Node._PhysicsProcess"/> will make the cube appear to lag because
		/// <see cref="Node._PhysicsProcess"/> happens at a different timestep than regular screen updates.
		/// </remarks>
		/// <param name="transform">The <see cref="Transform3D"/> where the cube will be located at.</param>
		/// <param name="height">The height of the cube. (Optional, defaults to <see langword="1"/>)</param>
		/// <param name="width">The width of the cube. (Optional, defaults to <see langword="1"/>)</param>
		/// <param name="depth">The depth of the cube. (Optional, defaults to <see langword="1"/>)</param>
		/// <param name="color">The <see cref="Color"/> that the cube will be drawn in. (Optional, defaults to <see cref="Colors.Magenta"/>)</param>
		/// <param name="thickness">The thickness of the lines that make up the wireframe. (Optional, defaults to <see langword="0.5"/>)</param>
		/// <param name="antialiased">Whether the wireframe should be anti-aliased. (Optional, defaults to <see langword="true"/>)</param>
		/// <returns><see langword="void"/></returns>
		public static void DrawCube( Transform3D transform, float height = 1f, float width = 1f, float depth = 1f, Color? color = null, float thickness = 0.5f, bool antialiased = true )
		{
			_Draw( () => _DrawCubeImpl(
				transform,
				height,
				width,
				depth,
				color is null ? Colors.Magenta : (Color) color,
				thickness,
				antialiased
			) );
		}

		/// <summary>
		/// Draws a wireframe scalene triangle shape in the game for debug visualization.
		/// </summary>
		/// <remarks>
		/// Note: The triangle will be drawn exactly when called, not at a fixed interval or fps. So for example, calling
		/// this from <see cref="Node._PhysicsProcess"/> will make the triangle appear to lag because
		/// <see cref="Node._PhysicsProcess"/> happens at a different timestep than regular screen updates.
		/// </remarks>
		/// <param name="transform">The <see cref="Transform3D"/> where the triangle will be located at.</param>
		/// <param name="lengthA">The length of the first side of the triangle. (Optional, defaults to <see langword="1"/>)</param>
		/// <param name="lengthB">The length of the second side of the triangle. (Optional, defaults to <see langword="1"/>)</param>
		/// <param name="lengthC">The length of the third side of the triangle. (Optional, defaults to <see langword="1"/>)</param>
		/// <param name="color">The <see cref="Color"/> that the triangle will be drawn in. (Optional, defaults to <see cref="Colors.Magenta"/>)</param>
		/// <param name="thickness">The thickness of the lines that make up the triangle. (Optional, defaults to <see langword="0.5"/>)</param>
		/// <param name="antialiased">Whether the triangle should be anti-aliased. (Optional, defaults to <see langword="true"/>)</param>
		/// <returns><see langword="void"/></returns>
		public static void DrawScaleneTriangle( Transform3D transform, float lengthA = 1f, float lengthB = 1f, float lengthC = 1f, Color? color = null, float thickness = 0.5f, bool antialiased = true )
		{
			_Draw( () => _DrawScaleneTriangleImpl(
				transform,
				lengthA,
				lengthB,
				lengthC,
				color is null ? Colors.Magenta : (Color) color,
				thickness,
				antialiased
			) );
		}

		/// <summary>
		/// Draws a wireframe equilateral triangle shape in the game for debug visualization.
		/// </summary>
		/// <remarks>
		/// Note: The triangle will be drawn exactly when called, not at a fixed interval or fps. So for example, calling
		/// this from <see cref="Node._PhysicsProcess"/> will make the triangle appear to lag because
		/// <see cref="Node._PhysicsProcess"/> happens at a different timestep than regular screen updates.
		/// </remarks>
		/// <param name="transform">The <see cref="Transform3D"/> where the triangle will be located at.</param>
		/// <param name="length">The length of all sides of the triangle. (Optional, defaults to <see langword="1"/>)</param>
		/// <param name="color">The <see cref="Color"/> that the triangle will be drawn in. (Optional, defaults to <see cref="Colors.Magenta"/>)</param>
		/// <param name="thickness">The thickness of the lines that make up the triangle. (Optional, defaults to <see langword="0.5"/>)</param>
		/// <param name="antialiased">Whether the triangle should be anti-aliased. (Optional, defaults to <see langword="true"/>)</param>
		/// <returns><see langword="void"/></returns>
		public static void DrawEquilateralTriangle( Transform3D transform, float length = 1f, Color? color = null, float thickness = 0.5f, bool antialiased = true )
		{
			_Draw( () => _DrawEquilateralTriangleImpl(
				transform,
				length,
				color is null ? Colors.Magenta : (Color) color,
				thickness,
				antialiased
			) );
		}

		/// <summary>
		/// Draws a wireframe pyramid shape in the game for debug visualization.
		/// </summary>
		/// <remarks>
		/// Note: The pyramid will be drawn exactly when called, not at a fixed interval or fps. So for example, calling
		/// this from <see cref="Node._PhysicsProcess"/> will make the pyramid appear to lag because
		/// <see cref="Node._PhysicsProcess"/> happens at a different timestep than regular screen updates.
		/// </remarks>
		/// <param name="transform">The <see cref="Transform3D"/> where the pyramid will be located at.</param>
		/// <param name="height">The height of the pyramid. (Optional, defaults to <see langword="1"/>)</param>
		/// <param name="width">The width of the pyramid. (Optional, defaults to <see langword="1"/>)</param>
		/// <param name="depth">The depth of the pyramid. (Optional, defaults to <see langword="1"/>)</param>
		/// <param name="color">The <see cref="Color"/> that the pyramid will be drawn in. (Optional, defaults to <see cref="Colors.Magenta"/>)</param>
		/// <param name="thickness">The thickness of the lines that make up the pyramid. (Optional, defaults to <see langword="0.5"/>)</param>
		/// <param name="antialiased">Whether the pyramid should be anti-aliased. (Optional, defaults to <see langword="true"/>)</param>
		/// <returns><see langword="void"/></returns>
		public static void DrawPyramid( Transform3D transform, float height = 1f, float width = 1f, float depth = 1f, Color? color = null, float thickness = 0.5f, bool antialiased = true )
		{
			_Draw( () => _DrawPyramidImpl(
				transform,
				height,
				width,
				depth,
				color is null ? Colors.Magenta : (Color) color,
				thickness,
				antialiased
			) );
		}

		/// <summary>
		/// Draws a wireframe cone shape in the game for debug visualization.
		/// </summary>
		/// <remarks>
		/// Note: The cone will be drawn exactly when called, not at a fixed interval or fps. So for example, calling
		/// this from <see cref="Node._PhysicsProcess"/> will make the cone appear to lag because
		/// <see cref="Node._PhysicsProcess"/> happens at a different timestep than regular screen updates.
		/// </remarks>
		/// <param name="transform">The <see cref="Transform3D"/> where the cone will be located at.</param>
		/// <param name="height">The height of the cone. (Optional, defaults to <see langword="1"/>)</param>
		/// <param name="radius">The radius of the cone. (Optional, defaults to <see langword="0.5"/>)</param>
		/// <param name="color">The <see cref="Color"/> that the cone will be drawn in. (Optional, defaults to <see cref="Colors.Magenta"/>)</param>
		/// <param name="thickness">The thickness of the lines that make up the cone. (Optional, defaults to <see langword="0.5"/>)</param>
		/// <param name="resolution">The resolution of the cone. (Optional, defaults to <see langword="32"/>)</param>
		/// <param name="contour">Whether the contour of the cone should also be drawn. (Optional, defaults to <see langword="true"/>)</param>
		/// <param name="antialiased">Whether the cone should be anti-aliased. (Optional, defaults to <see langword="true"/>)</param>
		/// <returns><see langword="void"/></returns>
		public static void DrawCone( Transform3D transform, float height = 1f, float radius = 0.5f, Color? color = null, float thickness = 0.5f, int resolution = 32, bool contour = true, bool antialiased = true )
		{
			_Draw( () => _DrawConeImpl(
				transform,
				height,
				radius,
				color is null ? Colors.Magenta : (Color) color,
				thickness,
				resolution,
				contour,
				antialiased
			) );
		}

		/// <summary>
		/// Draws some screen text in the game for debug visualization.
		/// </summary>
		/// <remarks>
		/// Note: The text will be drawn exactly when called, not at a fixed interval or fps. So for example, calling
		/// this from <see cref="Node._PhysicsProcess"/> will make the text appear to lag because
		/// <see cref="Node._PhysicsProcess"/> happens at a different timestep than regular screen updates.
		/// </remarks>
		/// <param name="text">The text to draw.</param>
		/// <param name="color">The <see cref="Color"/> that the text will be drawn in. (Optional, defaults to <see cref="Colors.Magenta"/>)</param>
		/// <param name="size">The font size of the text. (Optional, defaults to <see langword="16"/>)</param>
		/// <param name="position">The <see cref="Vector2">position</see> on the screen where the text will be drawn. (Optional, defaults to <see cref="Vector2.Zero"/>, i.e. Upper-left)</param>
		/// <returns><see langword="void"/></returns>
		public static void DrawText( object text, Color? color = null, int size = 16, Vector2? position = null )
		{
			_Draw( () => _DrawTextImpl(
				text,
				color is null ? Colors.Magenta : (Color) color,
				size,
				position is null ? Vector2.Zero : (Vector2) position
			) );
		}
		#endregion
	}
}
