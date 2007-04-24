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

namespace Bnoerj.AI.Steering
{
	/// <summary>
	/// SphericalObstacle a simple concrete type of obstacle.
	/// </summary>
	public class SphericalObstacle : IObstacle
	{
		SeenFromState seenFrom;

		public float Radius;
		public Vec3 Center;

		// constructors
		public SphericalObstacle()
			: this(1, Vec3.Zero)
		{
		}
		public SphericalObstacle(float radius, Vec3 center)
		{
			Radius = radius;
			Center = center;
		}

		public SeenFromState SeenFrom
		{
			get { return seenFrom; }
			set { seenFrom = value; }
		}

		// XXX 4-23-03: Temporary work around (see comment above)
		//
		// Checks for intersection of the given spherical obstacle with a
		// volume of "likely future vehicle positions": a cylinder along the
		// current path, extending minTimeToCollision seconds along the
		// forward axis from current position.
		//
		// If they intersect, a collision is imminent and this function returns
		// a steering force pointing laterally away from the obstacle's center.
		//
		// Returns a zero vector if the obstacle is outside the cylinder
		//
		// xxx couldn't this be made more compact using localizePosition?

		public Vec3 SteerToAvoid(IVehicle vehicle, float minTimeToCollision)
		{
			// minimum distance to obstacle before avoidance is required
			float minDistanceToCollision = minTimeToCollision * vehicle.Speed;
			float minDistanceToCenter = minDistanceToCollision + Radius;

			// contact distance: sum of radii of obstacle and vehicle
			float totalRadius = Radius + vehicle.Radius;

			// obstacle center relative to vehicle position
			Vec3 localOffset = Center - vehicle.Position;

			// distance along vehicle's forward axis to obstacle's center
			float forwardComponent = localOffset.Dot(vehicle.Forward);
			Vec3 forwardOffset = vehicle.Forward * forwardComponent;

			// offset from forward axis to obstacle's center
			Vec3 offForwardOffset = localOffset - forwardOffset;

			// test to see if sphere overlaps with obstacle-free corridor
			bool inCylinder = offForwardOffset.Length() < totalRadius;
			bool nearby = forwardComponent < minDistanceToCenter;
			bool inFront = forwardComponent > 0;

			// if all three conditions are met, steer away from sphere center
			if (inCylinder && nearby && inFront)
			{
				return offForwardOffset * -1;
			}
			else
			{
				return Vec3.Zero;
			}
		}
	}
}
