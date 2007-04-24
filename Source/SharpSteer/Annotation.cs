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
using Microsoft.Xna.Framework.Graphics;

namespace Bnoerj.AI.Steering
{
	//FIXME: this class should not be abstract
	public abstract class Annotation : AbstractVehicle
	{
		// trails
		int trailVertexCount;       // number of vertices in array (ring buffer)
		int trailIndex;             // array index of most recently recorded point
		float trailDuration;        // duration (in seconds) of entire trail
		float trailSampleInterval;  // desired interval between taking samples
		float trailLastSampleTime;  // global time when lat sample was taken
		int trailDottedPhase;       // dotted line: draw segment or not
		Vec3 curPosition;           // last reported position of vehicle
		Vec3[] trailVertices;        // array (ring) of recent points along trail
		byte[] trailFlags;           // array (ring) of flag bits for trail points

		static bool isAnnotationEnabled;

		public static IDraw drawer;

		// constructor
		public Annotation()
		{
			trailVertices = null;
			trailFlags = null;
			isAnnotationEnabled = true;

			// xxx I wonder if it makes more sense to NOT do this here, see if the
			// xxx vehicle class calls it to set custom parameters, and if not, set
			// xxx these default parameters on first call to a "trail" function.  The
			// xxx issue is whether it is a problem to allocate default-sized arrays
			// xxx then to free them and allocate new ones
			SetTrailParameters(5, 100);  // 5 seconds with 100 points along the trail
		}

		public static bool IsAnnotationEnabled
		{
			get { return isAnnotationEnabled; }
			set { isAnnotationEnabled = value; }
		}

		// ------------------------------------------------------------------------
		// trails / streamers
		//
		// these routines support visualization of a vehicle's recent path
		//
		// XXX conceivable trail/streamer should be a separate class,
		// XXX Annotation would "has-a" one (or more))

		// record a position for the current time, called once per update
		public void RecordTrailVertex(float currentTime, Vec3 position)
		{
			float timeSinceLastTrailSample = currentTime - trailLastSampleTime;
			if (timeSinceLastTrailSample > trailSampleInterval)
			{
				trailIndex = (trailIndex + 1) % trailVertexCount;
				trailVertices[trailIndex] = position;
				trailDottedPhase = (trailDottedPhase + 1) % 2;
				bool tick = (Math.Floor(currentTime) > Math.Floor(trailLastSampleTime));
				trailFlags[trailIndex] = (byte)(trailDottedPhase | (tick ? 2 : 0));
				trailLastSampleTime = currentTime;
			}
			curPosition = position;
		}

		// draw the trail as a dotted line, fading away with age
		public void DrawTrail()
		{
			DrawTrail(Color.LightGray, Color.White);
		}

		public void DrawTrail(Color trailColor, Color tickColor)
		{
			if (isAnnotationEnabled == true)
			{
				int index = trailIndex;
				for (int j = 0; j < trailVertexCount; j++)
				{
					// index of the next vertex (mod around ring buffer)
					int next = (index + 1) % trailVertexCount;

					// "tick mark": every second, draw a segment in a different color
					bool tick = ((trailFlags[index] & 2) != 0 || (trailFlags[next] & 2) != 0);
					Color color = tick ? tickColor : trailColor;

					// draw every other segment
					if ((trailFlags[index] & 1) != 0)
					{
						if (j == 0)
						{
							// draw segment from current position to first trail point
							if (drawer != null) drawer.LineAlpha(curPosition, trailVertices[index], color, 1);
						}
						else
						{
							// draw trail segments with opacity decreasing with age
							const float minO = 0.05f; // minimum opacity
							float fraction = (float)j / trailVertexCount;
							float opacity = (fraction * (1 - minO)) + minO;
							if (drawer != null) drawer.LineAlpha(trailVertices[index], trailVertices[next], color, opacity);
						}
					}
					index = next;
				}
			}
		}

		// set trail parameters: the amount of time it represents and the
		// number of samples along its length.  re-allocates internal buffers.
		public void SetTrailParameters(float duration, int vertexCount)
		{
			// record new parameters
			trailDuration = duration;
			trailVertexCount = vertexCount;

			// reset other internal trail state
			trailIndex = 0;
			trailLastSampleTime = 0;
			trailSampleInterval = trailDuration / trailVertexCount;
			trailDottedPhase = 1;

			// prepare trailVertices array: free old one if needed, allocate new one
			trailVertices = null;
			trailVertices = new Vec3[trailVertexCount];

			// prepare trailFlags array: free old one if needed, allocate new one
			trailFlags = null;
			trailFlags = new byte[trailVertexCount];

			// initializing all flags to zero means "do not draw this segment"
			for (int i = 0; i < trailVertexCount; i++) trailFlags[i] = 0;
		}

		// forget trail history: used to prevent long streaks due to teleportation
		public void ClearTrailHistory()
		{
			// brute force implementation, reset everything
			SetTrailParameters(trailDuration, trailVertexCount);
		}

		// ------------------------------------------------------------------------
		// drawing of lines, circles and (filled) disks to annotate steering
		// behaviors.  When called during OpenSteerDemo's simulation update phase,
		// these functions call a "deferred draw" routine which buffer the
		// arguments for use during the redraw phase.
		//
		// note: "circle" means unfilled
		//       "disk" means filled
		//       "XZ" means on a plane parallel to the X and Z axes (perp to Y)
		//       "3d" means the circle is perpendicular to the given "axis"
		//       "segments" is the number of line segments used to draw the circle

		// draw an opaque colored line segment between two locations in space
		public void AnnotationLine(Vec3 startPoint, Vec3 endPoint, Color color)
		{
			if (isAnnotationEnabled == true && drawer != null)
			{
				drawer.Line(startPoint, endPoint, color);
			}
		}

		// draw a circle on the XZ plane
		public void AnnotationXZCircle(float radius, Vec3 center, Color color, int segments)
		{
			AnnotationXZCircleOrDisk(radius, center, color, segments, false);
		}

		// draw a disk on the XZ plane
		public void AnnotationXZDisk(float radius, Vec3 center, Color color, int segments)
		{
			AnnotationXZCircleOrDisk(radius, center, color, segments, true);
		}

		// draw a circle perpendicular to the given axis
		public void Annotation3dCircle(float radius, Vec3 center, Vec3 axis, Color color, int segments)
		{
			Annotation3dCircleOrDisk(radius, center, axis, color, segments, false);
		}

		// draw a disk perpendicular to the given axis
		public void Annotation3dDisk(float radius, Vec3 center, Vec3 axis, Color color, int segments)
		{
			Annotation3dCircleOrDisk(radius, center, axis, color, segments, true);
		}

		// ------------------------------------------------------------------------
		// support for annotation circles
		public void AnnotationXZCircleOrDisk(float radius, Vec3 center, Color color, int segments, bool filled)
		{
			AnnotationCircleOrDisk(radius,
									Vec3.Zero,
									center,
									color,
									segments,
									filled,
									false); // "not in3d" -> on XZ plane
		}

		public void Annotation3dCircleOrDisk(float radius, Vec3 center, Vec3 axis, Color color, int segments, bool filled)
		{
			AnnotationCircleOrDisk(radius,
									axis,
									center,
									color,
									segments,
									filled,
									true); // "in3d"
		}

		public void AnnotationCircleOrDisk(float radius, Vec3 axis, Vec3 center, Color color, int segments, bool filled, bool in3d)
		{
			if (isAnnotationEnabled == true && drawer != null)
			{
				drawer.CircleOrDisk(radius, axis, center, color, segments, filled, in3d);
			}
		}
	}
}
