// Copyright (c) 2002-2003, Sony Computer Entertainment America
// Copyright (c) 2002-2003, Craig Reynolds <craig_reynolds@playstation.sony.com>
// Copyright (C) 2007 Bjoern Graf <bjoern.graf@gmx.net>
// All rights reserved.
//
// This software is licensed as described in the file license.txt, which
// you should have received as part of this distribution. The terms
// are also available at http://www.codeplex.com/SharpSteer/Project/License.aspx.

using System;
using System.Collections.Generic;
using System.Text;
using Bnoerj.AI.Steering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Bnoerj.SharpSteer
{
	public struct TextEntry
	{
		public Color Color;
		public Vector2 Position;
		public String Text;
	}

	public class Drawing : IDraw
	{
		enum DrawMode
		{
			GL_LINES,
			GL_LINE_LOOP,
			GL_TRIANGLES,
			GL_TRIANGLE_FAN,
		}

		public static Demo game = null;
		static CullMode cullMode;
		static Color curColor;
		static DrawMode curMode;
		static List<VertexPositionColor> vertices = new List<VertexPositionColor>();
		static LocalSpace localSpace = new LocalSpace();

		static void glColor(Color color)
		{
			curColor = color;
		}

		static void glBegin(DrawMode mode)
		{
			curMode = mode;
		}

		static void glEnd()
		{
			PrimitiveType type = PrimitiveType.LineList;
			int primitiveCount = 0;
			switch (curMode)
			{
			case DrawMode.GL_LINES:
				type = PrimitiveType.LineList;
				primitiveCount = vertices.Count / 2;
				break;
			case DrawMode.GL_LINE_LOOP:
				type = PrimitiveType.LineStrip;
				vertices.Add(vertices[0]);
				primitiveCount = vertices.Count - 1;
				break;
			case DrawMode.GL_TRIANGLES:
				type = PrimitiveType.TriangleList;
				primitiveCount = vertices.Count / 3;
				break;
			case DrawMode.GL_TRIANGLE_FAN:
				type = PrimitiveType.TriangleFan;
				primitiveCount = vertices.Count - 2;
				break;
			}

			game.graphics.GraphicsDevice.DrawUserPrimitives(type, vertices.ToArray(), 0, primitiveCount);

			vertices.Clear();
		}

		static void glVertexVec3(Vec3 v)
		{
			vertices.Add(new VertexPositionColor(v.ToVector3(), curColor));
		}

		static void BeginDoubleSidedDrawing()
		{
			cullMode = game.graphics.GraphicsDevice.RenderState.CullMode;
			game.graphics.GraphicsDevice.RenderState.CullMode = CullMode.None;
		}

		static void EndDoubleSidedDrawing()
		{
			game.graphics.GraphicsDevice.RenderState.CullMode = cullMode;
		}

		public static void iDrawLine(Vec3 startPoint, Vec3 endPoint, Color color)
		{
			glColor(color);
			glBegin(DrawMode.GL_LINES);
			glVertexVec3(startPoint);
			glVertexVec3(endPoint);
			glEnd();
		}

		static void iDrawTriangle(Vec3 a, Vec3 b, Vec3 c, Color color)
		{
			glColor(color);
			glBegin(DrawMode.GL_TRIANGLES);
			{
				glVertexVec3(a);
				glVertexVec3(b);
				glVertexVec3(c);
			}
			glEnd();
		}

		// Draw a single OpenGL quadrangle given four Vec3 vertices, and color.
		static void iDrawQuadrangle(Vec3 a, Vec3 b, Vec3 c, Vec3 d, Color color)
		{
			glColor(color);
			glBegin(DrawMode.GL_TRIANGLE_FAN);
			{
				glVertexVec3(a);
				glVertexVec3(b);
				glVertexVec3(c);
				glVertexVec3(d);
			}
			glEnd();
		}

		public void Line(Vec3 startPoint, Vec3 endPoint, Color color)
		{
			DrawLine(startPoint, endPoint, color);
		}

		// draw a line with alpha blending
		public void LineAlpha(Vec3 startPoint, Vec3 endPoint, Color color, float alpha)
		{
			DrawLineAlpha(startPoint, endPoint, color, alpha);
		}

		public void CircleOrDisk(float radius, Vec3 axis, Vec3 center, Color color, int segments, bool filled, bool in3d)
		{
			DrawCircleOrDisk(radius, axis, center, color, segments, filled, in3d);
		}

		public static void DrawLine(Vec3 startPoint, Vec3 endPoint, Color color)
		{
			if (Demo.IsDrawPhase == true)
			{
				iDrawLine(startPoint, endPoint, color);
			}
			else
			{
				DeferredLine.AddToBuffer(startPoint, endPoint, color);
			}
		}

		// draw a line with alpha blending
		public static void DrawLineAlpha(Vec3 startPoint, Vec3 endPoint, Color color, float alpha)
		{
			Color c = new Color(color.R, color.G, color.B, (byte)(255.0f * alpha));
			if (Demo.IsDrawPhase == true)
			{
				iDrawLine(startPoint, endPoint, c);
			}
			else
			{
				DeferredLine.AddToBuffer(startPoint, endPoint, c);
			}
		}

		// draw 2d lines in screen space: x and y are the relevant coordinates
		public static void Draw2dLine(Vec3 startPoint, Vec3 endPoint, Color color)
		{
			iDrawLine(startPoint, endPoint, color);
		}

		// draws a "wide line segment": a rectangle of the given width and color
		// whose mid-line connects two given endpoints
		public static void DrawXZWideLine(Vec3 startPoint, Vec3 endPoint, Color color, float width)
		{
			Vec3 offset = endPoint - startPoint;
			Vec3 along = offset.Normalize();
			Vec3 perp = localSpace.LocalRotateForwardToSide(along);
			Vec3 radius = perp * width / 2;

			Vec3 a = startPoint + radius;
			Vec3 b = endPoint + radius;
			Vec3 c = endPoint - radius;
			Vec3 d = startPoint - radius;

			iDrawQuadrangle(a, b, c, d, color);
		}

		// draw a (filled-in, polygon-based) square checkerboard grid on the XZ
		// (horizontal) plane.
		//
		// ("size" is the length of a side of the overall grid, "subsquares" is the
		// number of subsquares along each edge (for example a standard checkboard
		// has eight), "center" is the 3d position of the center of the grid,
		// color1 and color2 are used for alternating subsquares.)
		public static void DrawXZCheckerboardGrid(float size, int subsquares, Vec3 center, Color color1, Color color2)
		{
			float half = size / 2;
			float spacing = size / subsquares;

			BeginDoubleSidedDrawing();
			{
				bool flag1 = false;
				float p = -half;
				Vec3 corner = new Vec3();
				for (int i = 0; i < subsquares; i++)
				{
					bool flag2 = flag1;
					float q = -half;
					for (int j = 0; j < subsquares; j++)
					{
						corner.Set(p, 0, q);
						corner += center;
						iDrawQuadrangle(corner,
										 corner + new Vec3(spacing, 0, 0),
										 corner + new Vec3(spacing, 0, spacing),
										 corner + new Vec3(0, 0, spacing),
										 flag2 ? color1 : color2);
						flag2 = !flag2;
						q += spacing;
					}
					flag1 = !flag1;
					p += spacing;
				}
			}
			EndDoubleSidedDrawing();
		}

		// draw a square grid of lines on the XZ (horizontal) plane.
		//
		// ("size" is the length of a side of the overall grid, "subsquares" is the
		// number of subsquares along each edge (for example a standard checkboard
		// has eight), "center" is the 3d position of the center of the grid, lines
		// are drawn in the specified "color".)
		public static void DrawXZLineGrid(float size, int subsquares, Vec3 center, Color color)
		{
			float half = size / 2;
			float spacing = size / subsquares;

			// set grid drawing color
			glColor(color);

			// draw a square XZ grid with the given size and line count
			glBegin(DrawMode.GL_LINES);
			float q = -half;
			for (int i = 0; i < (subsquares + 1); i++)
			{
				Vec3 x1 = new Vec3(q, 0, +half); // along X parallel to Z
				Vec3 x2 = new Vec3(q, 0, -half);
				Vec3 z1 = new Vec3(+half, 0, q); // along Z parallel to X
				Vec3 z2 = new Vec3(-half, 0, q);

				glVertexVec3(x1 + center);
				glVertexVec3(x2 + center);
				glVertexVec3(z1 + center);
				glVertexVec3(z2 + center);

				q += spacing;
			}
			glEnd();
		}

		// draw the three axes of a LocalSpace: three lines parallel to the
		// basis vectors of the space, centered at its origin, of lengths
		// given by the coordinates of "size".
		public static void DrawAxes(ILocalSpace ls, Vec3 size, Color color)
		{
			Vec3 x = new Vec3(size.X / 2, 0, 0);
			Vec3 y = new Vec3(0, size.Y / 2, 0);
			Vec3 z = new Vec3(0, 0, size.Z / 2);

			iDrawLine(ls.GlobalizePosition(x), ls.GlobalizePosition(x * -1), color);
			iDrawLine(ls.GlobalizePosition(y), ls.GlobalizePosition(y * -1), color);
			iDrawLine(ls.GlobalizePosition(z), ls.GlobalizePosition(z * -1), color);
		}

		public static void DrawQuadrangle(Vec3 a, Vec3 b, Vec3 c, Vec3 d, Color color)
		{
			iDrawQuadrangle(a, b, c, d, color);
		}

		// draw the edges of a box with a given position, orientation, size
		// and color.  The box edges are aligned with the axes of the given
		// LocalSpace, and it is centered at the origin of that LocalSpace.
		// "size" is the main diagonal of the box.
		//
		// use gGlobalSpace to draw a box aligned with global space
		public static void DrawBoxOutline(ILocalSpace localSpace, Vec3 size, Color color)
		{
			Vec3 s = size / 2.0f;  // half of main diagonal

			Vec3 a = new Vec3(+s.X, +s.Y, +s.Z);
			Vec3 b = new Vec3(+s.X, -s.Y, +s.Z);
			Vec3 c = new Vec3(-s.X, -s.Y, +s.Z);
			Vec3 d = new Vec3(-s.X, +s.Y, +s.Z);

			Vec3 e = new Vec3(+s.X, +s.Y, -s.Z);
			Vec3 f = new Vec3(+s.X, -s.Y, -s.Z);
			Vec3 g = new Vec3(-s.X, -s.Y, -s.Z);
			Vec3 h = new Vec3(-s.X, +s.Y, -s.Z);

			Vec3 A = localSpace.GlobalizePosition(a);
			Vec3 B = localSpace.GlobalizePosition(b);
			Vec3 C = localSpace.GlobalizePosition(c);
			Vec3 D = localSpace.GlobalizePosition(d);

			Vec3 E = localSpace.GlobalizePosition(e);
			Vec3 F = localSpace.GlobalizePosition(f);
			Vec3 G = localSpace.GlobalizePosition(g);
			Vec3 H = localSpace.GlobalizePosition(h);

			iDrawLine(A, B, color);
			iDrawLine(B, C, color);
			iDrawLine(C, D, color);
			iDrawLine(D, A, color);

			iDrawLine(A, E, color);
			iDrawLine(B, F, color);
			iDrawLine(C, G, color);
			iDrawLine(D, H, color);

			iDrawLine(E, F, color);
			iDrawLine(F, G, color);
			iDrawLine(G, H, color);
			iDrawLine(H, E, color);
		}

		public static void DrawXZCircle(float radius, Vec3 center, Color color, int segments)
		{
			DrawXZCircleOrDisk(radius, center, color, segments, false);
		}

		public static void DrawXZDisk(float radius, Vec3 center, Color color, int segments)
		{
			DrawXZCircleOrDisk(radius, center, color, segments, true);
		}

		// drawing utility used by both drawXZCircle and drawXZDisk
		public static void DrawXZCircleOrDisk(float radius, Vec3 center, Color color, int segments, bool filled)
		{
			// draw a circle-or-disk on the XZ plane
			DrawCircleOrDisk(radius, Vec3.Zero, center, color, segments, filled, false);
		}

		// a simple 2d vehicle on the XZ plane
		public static void DrawBasic2dCircularVehicle(IVehicle vehicle, Color color)
		{
			// "aspect ratio" of body (as seen from above)
			float x = 0.5f;
			float y = (float)Math.Sqrt(1 - (x * x));

			// radius and position of vehicle
			float r = vehicle.Radius;
			Vec3 p = vehicle.Position;

			// shape of triangular body
			Vec3 u = new Vec3(0, 1, 0) * r * 0.05f; // slightly up
			Vec3 f = vehicle.Forward * r;
			Vec3 s = vehicle.Side * x * r;
			Vec3 b = vehicle.Forward * -y * r;

			// draw double-sided triangle (that is: no (back) face culling)
			BeginDoubleSidedDrawing();
			iDrawTriangle(p + f + u,
						   p + b - s + u,
						   p + b + s + u,
						   color);
			EndDoubleSidedDrawing();

			// draw the circular collision boundary
			DrawXZCircle(r, p + u, Color.White, 20);
		}

		// a simple 3d vehicle
		public static void DrawBasic3dSphericalVehicle(IVehicle vehicle, Color color)
		{
			Vector3 vColor = new Vector3((float)color.R / 255.0f, (float)color.G / 255.0f, (float)color.B / 255.0f);

			// "aspect ratio" of body (as seen from above)
			const float x = 0.5f;
			float y = (float)Math.Sqrt(1 - (x * x));

			// radius and position of vehicle
			float r = vehicle.Radius;
			Vec3 p = vehicle.Position;

			// body shape parameters
			Vec3 f = vehicle.Forward * r;
			Vec3 s = vehicle.Side * r * x;
			Vec3 u = vehicle.Up * r * x * 0.5f;
			Vec3 b = vehicle.Forward * r * -y;

			// vertex positions
			Vec3 nose = p + f;
			Vec3 side1 = p + b - s;
			Vec3 side2 = p + b + s;
			Vec3 top = p + b + u;
			Vec3 bottom = p + b - u;

			// colors
			const float j = +0.05f;
			const float k = -0.05f;
			Color color1 = new Color(vColor + new Vector3(j, j, k));
			Color color2 = new Color(vColor + new Vector3(j, k, j));
			Color color3 = new Color(vColor + new Vector3(k, j, j));
			Color color4 = new Color(vColor + new Vector3(k, j, k));
			Color color5 = new Color(vColor + new Vector3(k, k, j));

			// draw body
			iDrawTriangle(nose, side1, top, color1);  // top, side 1
			iDrawTriangle(nose, top, side2, color2);  // top, side 2
			iDrawTriangle(nose, bottom, side1, color3);  // bottom, side 1
			iDrawTriangle(nose, side2, bottom, color4);  // bottom, side 2
			iDrawTriangle(side1, side2, top, color5);  // top back
			iDrawTriangle(side2, side1, bottom, color5);  // bottom back
		}

		// General purpose circle/disk drawing routine.  Draws circles or disks (as
		// specified by "filled" argument) and handles both special case 2d circles
		// on the XZ plane or arbitrary circles in 3d space (as specified by "in3d"
		// argument)
		public static void DrawCircleOrDisk(float radius, Vec3 axis, Vec3 center, Color color, int segments, bool filled, bool in3d)
		{
			if (Demo.IsDrawPhase == true)
			{
				LocalSpace ls = new LocalSpace();
				if (in3d)
				{
					// define a local space with "axis" as the Y/up direction
					// (XXX should this be a method on  LocalSpace?)
					Vec3 unitAxis = axis.Normalize();
					Vec3 unitPerp = Vec3.FindPerpendicularIn3d(axis).Normalize();
					ls.Up = unitAxis;
					ls.Forward = unitPerp;
					ls.Position = (center);
					ls.SetUnitSideFromForwardAndUp();
				}

				// make disks visible (not culled) from both sides 
				if (filled) BeginDoubleSidedDrawing();

				// point to be rotated about the (local) Y axis, angular step size
				Vec3 pointOnCircle = new Vec3(radius, 0, 0);
				float step = (float)(2 * Math.PI) / (float)segments;

				// set drawing color
				glColor(color);

				// begin drawing a triangle fan (for disk) or line loop (for circle)
				glBegin(filled ? DrawMode.GL_TRIANGLE_FAN : DrawMode.GL_LINE_LOOP);

				// for the filled case, first emit the center point
				if (filled) glVertexVec3(in3d ? ls.Position : center);

				// rotate p around the circle in "segments" steps
				float sin = 0, cos = 0;
				int vertexCount = filled ? segments + 1 : segments;
				for (int i = 0; i < vertexCount; i++)
				{
					// emit next point on circle, either in 3d (globalized out
					// of the local space), or in 2d (offset from the center)
					glVertexVec3(in3d ? ls.GlobalizePosition(pointOnCircle) : pointOnCircle + center);

					// rotate point one more step around circle
					pointOnCircle = pointOnCircle.RotateAboutGlobalY(step, ref sin, ref cos);
				}

				// close drawing operation
				glEnd();
				if (filled) EndDoubleSidedDrawing();
			}
			else
			{
				DeferredCircle.AddToBuffer(radius, axis, center, color, segments, filled, in3d);
			}
		}

		public static void Draw3dCircleOrDisk(float radius, Vec3 center, Vec3 axis, Color color, int segments, bool filled)
		{
			// draw a circle-or-disk in the given local space
			DrawCircleOrDisk(radius, axis, center, color, segments, filled, true);
		}

		public static void Draw3dCircle(float radius, Vec3 center, Vec3 axis, Color color, int segments)
		{
			Draw3dCircleOrDisk(radius, center, axis, color, segments, false);
		}

		public static void AllDeferredLines()
		{
			DeferredLine.DrawAll();
		}

		public static void AllDeferredCirclesOrDisks()
		{
			DeferredCircle.DrawAll();
		}

		public static void Draw2dTextAt3dLocation(String text, Vec3 location, Color color)
		{
			// XXX NOTE: "it would be nice if" this had a 2d screenspace offset for
			// the origin of the text relative to the screen space projection of
			// the 3d point.

			// set text color and raster position
			Vector3 p = game.graphics.GraphicsDevice.Viewport.Project(location.ToVector3(), game.projectionMatrix, game.viewMatrix, game.worldMatrix);
			TextEntry textEntry = new TextEntry();
			textEntry.Color = color;
			textEntry.Position = new Vector2(p.X, p.Y);
			textEntry.Text = text;
			game.AddText(textEntry);
		}

		public static void Draw2dTextAt2dLocation(String text, Vec3 location, Color color)
		{
			// set text color and raster position
			TextEntry textEntry = new TextEntry();
			textEntry.Color = color;
			textEntry.Position = new Vector2(location.X, location.Y);
			textEntry.Text = text;
			game.AddText(textEntry);
		}

		public static float GetWindowWidth()
		{
			return 1024;
		}

		public static float GetWindowHeight()
		{
			return 640;
		}
	}
}
