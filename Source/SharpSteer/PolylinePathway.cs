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
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Bnoerj.AI.Steering
{
	/// <summary>
	/// PolylinePathway: a simple implementation of the Pathway protocol.  The path
	/// is a "polyline" a series of line segments between specified points.  A
	/// radius defines a volume for the path which is the union of a sphere at each
	/// point and a cylinder along each segment.
	/// </summary>
	public class PolylinePathway : Pathway
	{
		public int pointCount;
		public Vec3[] points;
		public float radius;
		public bool cyclic;

		public PolylinePathway()
		{ }

		// construct a PolylinePathway given the number of points (vertices),
		// an array of points, and a path radius.
		public PolylinePathway(int pointCount, Vec3[] points, float radius, bool cyclic)
		{
			Initialize(pointCount, points, radius, cyclic);
		}

		// utility for constructors in derived classes
		public void Initialize(int pointCount, Vec3[] points, float radius, bool cyclic)
		{
			// set data members, allocate arrays
			this.radius = radius;
			this.cyclic = cyclic;
			this.pointCount = pointCount;
			this.totalPathLen = 0;
			if (this.cyclic)
				this.pointCount++;
			this.lengths = new float[this.pointCount];
			this.points = new Vec3[this.pointCount];
			this.normals = new Vec3[this.pointCount];

			// loop over all points
			for (int i = 0; i < this.pointCount; i++)
			{
				// copy in point locations, closing cycle when appropriate
				bool closeCycle = this.cyclic && (i == this.pointCount - 1);
				int j = closeCycle ? 0 : i;
				this.points[i] = points[j];

				// for the end of each segment
				if (i > 0)
				{
					// compute the segment length
					normals[i] = this.points[i] - this.points[i - 1];
					lengths[i] = normals[i].Length();

					// find the normalized vector parallel to the segment
					normals[i] *= 1 / lengths[i];

					// keep running total of segment lengths
					totalPathLen += lengths[i];
				}
			}
		}

		// Given an arbitrary point ("A"), returns the nearest point ("P") on
		// this path.  Also returns, via output arguments, the path tangent at
		// P and a measure of how far A is outside the Pathway's "tube".  Note
		// that a negative distance indicates A is inside the Pathway.
		public override Vec3 MapPointToPath(Vec3 point, out Vec3 tangent, out float outside)
		{
			float d;
			float minDistance = float.MaxValue;
			Vec3 onPath = Vec3.Zero;
			tangent = Vec3.Zero;

			// loop over all segments, find the one nearest to the given point
			for (int i = 1; i < pointCount; i++)
			{
				segmentLength = lengths[i];
				segmentNormal = normals[i];
				d = PointToSegmentDistance(point, points[i - 1], points[i]);
				if (d < minDistance)
				{
					minDistance = d;
					onPath = chosen;
					tangent = segmentNormal;
				}
			}

			// measure how far original point is outside the Pathway's "tube"
			outside = Vec3.Distance(onPath, point) - radius;

			// return point on path
			return onPath;
		}

		// given an arbitrary point, convert it to a distance along the path
		public override float MapPointToPathDistance(Vec3 point)
		{
			float d;
			float minDistance = float.MaxValue;
			float segmentLengthTotal = 0;
			float pathDistance = 0;

			for (int i = 1; i < pointCount; i++)
			{
				segmentLength = lengths[i];
				segmentNormal = normals[i];
				d = PointToSegmentDistance(point, points[i - 1], points[i]);
				if (d < minDistance)
				{
					minDistance = d;
					pathDistance = segmentLengthTotal + segmentProjection;
				}
				segmentLengthTotal += segmentLength;
			}

			// return distance along path of onPath point
			return pathDistance;
		}

		// given a distance along the path, convert it to a point on the path
		public override Vec3 MapPathDistanceToPoint(float pathDistance)
		{
			// clip or wrap given path distance according to cyclic flag
			float remaining = pathDistance;
			if (cyclic)
			{
				remaining = pathDistance % totalPathLen;//FIXME: (float)fmod(pathDistance, totalPathLength);
			}
			else
			{
				if (pathDistance < 0) return points[0];
				if (pathDistance >= totalPathLen) return points[pointCount - 1];
			}

			// step through segments, subtracting off segment lengths until
			// locating the segment that contains the original pathDistance.
			// Interpolate along that segment to find 3d point value to return.
			Vec3 result = Vec3.Zero;
			for (int i = 1; i < pointCount; i++)
			{
				segmentLength = lengths[i];
				if (segmentLength < remaining)
				{
					remaining -= segmentLength;
				}
				else
				{
					float ratio = remaining / segmentLength;
					result = Utilities.Interpolate(ratio, points[i - 1], points[i]);
					break;
				}
			}
			return result;
		}

		// utility methods

		// compute minimum distance from a point to a line segment
		public float PointToSegmentDistance(Vec3 point, Vec3 ep0, Vec3 ep1)
		{
			// convert the test point to be "local" to ep0
			local = point - ep0;

			// find the projection of "local" onto "segmentNormal"
			segmentProjection = segmentNormal.Dot(local);

			// handle boundary cases: when projection is not on segment, the
			// nearest point is one of the endpoints of the segment
			if (segmentProjection < 0)
			{
				chosen = ep0;
				segmentProjection = 0;
				return Vec3.Distance(point, ep0);
			}
			if (segmentProjection > segmentLength)
			{
				chosen = ep1;
				segmentProjection = segmentLength;
				return Vec3.Distance(point, ep1);
			}

			// otherwise nearest point is projection point on segment
			chosen = segmentNormal * segmentProjection;
			chosen += ep0;
			return Vec3.Distance(point, chosen);
		}

		// assessor for total path length;
		public float TotalPathLength
		{
			get { return totalPathLen; }
		}

		// XXX removed the "private" because it interfered with derived
		// XXX classes later this should all be rewritten and cleaned up
		// private:

		// xxx shouldn't these 5 just be local variables?
		// xxx or are they used to pass secret messages between calls?
		// xxx seems like a bad design
		protected float segmentLength;
		protected float segmentProjection;
		protected Vec3 local;
		protected Vec3 chosen;
		protected Vec3 segmentNormal;

		protected float[] lengths;
		protected Vec3[] normals;
		protected float totalPathLen;
	}
}
