// Copyright (c) 2002-2003, Sony Computer Entertainment America
// Copyright (c) 2002-2003, Craig Reynolds <craig_reynolds@playstation.sony.com>
// Copyright (C) 2007 Bjoern Graf <bjoern.graf@gmx.net>
// All rights reserved.
//
// This software is licensed as described in the file license.txt, which
// you should have received as part of this distribution. The terms
// are also available at http://www.codeplex.com/SharpSteer/Project/License.aspx.

using System;
using Microsoft.Xna.Framework.Graphics;

namespace Bnoerj.AI.Steering
{
	/// <summary>
	/// LocalSpaceMixin is a mixin layer, a class template with a paramterized base
	/// class.  Allows "LocalSpace-ness" to be layered on any class.
	/// </summary>
	public class LocalSpace : ILocalSpace
	{
		// transformation as three orthonormal unit basis vectors and the
		// origin of the local space.  These correspond to the "rows" of
		// a 3x4 transformation matrix with [0 0 0 1] as the final column

		Vec3 side;     //    side-pointing unit basis vector
		Vec3 up;       //  upward-pointing unit basis vector
		Vec3 forward;  // forward-pointing unit basis vector
		Vec3 position; // origin of local space

		// accessors (get and set) for side, up, forward and position
		public Vec3 Side
		{
			get { return side; }
			set { side = value; }
		}
		public Vec3 Up
		{
			get { return up; }
			set { up = value; }
		}
		public Vec3 Forward
		{
			get { return forward; }
			set { forward = value; }
		}
		public Vec3 Position
		{
			get { return position; }
			set { position = value; }
		}

		public Vec3 SetUp(float x, float y, float z)
		{
			return up.Set(x, y, z);
		}
		public Vec3 SetForward(float x, float y, float z)
		{
			return forward.Set(x, y, z);
		}
		public Vec3 SetPosition(float x, float y, float z)
		{
			return position.Set(x, y, z);
		}

		// ------------------------------------------------------------------------
		// Global compile-time switch to control handedness/chirality: should
		// LocalSpace use a left- or right-handed coordinate system?  This can be
		// overloaded in derived types (e.g. vehicles) to change handedness.
		public bool IsRightHanded { get { return true; } }

		// ------------------------------------------------------------------------
		// constructors
		public LocalSpace()
		{
			ResetLocalSpace();
		}

		public LocalSpace(Vec3 side, Vec3 up, Vec3 forward, Vec3 position)
		{
			this.side = side;
			this.up = up;
			this.forward = forward;
			this.position = position;
		}

		public LocalSpace(Vec3 up, Vec3 forward, Vec3 position)
		{
			this.up = up;
			this.forward = forward;
			this.position = position;
			SetUnitSideFromForwardAndUp();
		}

		// ------------------------------------------------------------------------
		// reset transform: set local space to its identity state, equivalent to a
		// 4x4 homogeneous transform like this:
		//
		//     [ X 0 0 0 ]
		//     [ 0 1 0 0 ]
		//     [ 0 0 1 0 ]
		//     [ 0 0 0 1 ]
		//
		// where X is 1 for a left-handed system and -1 for a right-handed system.
		public void ResetLocalSpace()
		{
			forward.Set(0, 0, 1);
			side = LocalRotateForwardToSide(Forward);
			up.Set(0, 1, 0);
			position.Set(0, 0, 0);
		}

		// ------------------------------------------------------------------------
		// transform a direction in global space to its equivalent in local space
		public Vec3 LocalizeDirection(Vec3 globalDirection)
		{
			// dot offset with local basis vectors to obtain local coordiantes
			return new Vec3(globalDirection.Dot(side), globalDirection.Dot(up), globalDirection.Dot(forward));
		}

		// ------------------------------------------------------------------------
		// transform a point in global space to its equivalent in local space
		public Vec3 LocalizePosition(Vec3 globalPosition)
		{
			// global offset from local origin
			Vec3 globalOffset = globalPosition - position;

			// dot offset with local basis vectors to obtain local coordiantes
			return LocalizeDirection(globalOffset);
		}

		// ------------------------------------------------------------------------
		// transform a point in local space to its equivalent in global space
		public Vec3 GlobalizePosition(Vec3 localPosition)
		{
			return position + GlobalizeDirection(localPosition);
		}

		// ------------------------------------------------------------------------
		// transform a direction in local space to its equivalent in global space
		public Vec3 GlobalizeDirection(Vec3 localDirection)
		{
			return ((side * localDirection.X) +
					(up * localDirection.Y) +
					(forward * localDirection.Z));
		}

		// ------------------------------------------------------------------------
		// set "side" basis vector to normalized cross product of forward and up
		public void SetUnitSideFromForwardAndUp()
		{
			// derive new unit side basis vector from forward and up
			if (IsRightHanded)
				side.Cross(forward, up);
			else
				side.Cross(up, forward);
			side = side.Normalize();
		}

		// ------------------------------------------------------------------------
		// regenerate the orthonormal basis vectors given a new forward
		//(which is expected to have unit length)
		public void RegenerateOrthonormalBasisUF(Vec3 newUnitForward)
		{
			forward = newUnitForward;

			// derive new side basis vector from NEW forward and OLD up
			SetUnitSideFromForwardAndUp();

			// derive new Up basis vector from new Side and new Forward
			//(should have unit length since Side and Forward are
			// perpendicular and unit length)
			if (IsRightHanded)
				up.Cross(side, forward);
			else
				up.Cross(forward, side);
		}

		// for when the new forward is NOT know to have unit length
		public void RegenerateOrthonormalBasis(Vec3 newForward)
		{
			RegenerateOrthonormalBasisUF(newForward.Normalize());
		}

		// for supplying both a new forward and and new up
		public void RegenerateOrthonormalBasis(Vec3 newForward, Vec3 newUp)
		{
			up = newUp;
			RegenerateOrthonormalBasis(newForward.Normalize());
		}

		// ------------------------------------------------------------------------
		// rotate, in the canonical direction, a vector pointing in the
		// "forward"(+Z) direction to the "side"(+/-X) direction
		public Vec3 LocalRotateForwardToSide(Vec3 vector)
		{
			return new Vec3(IsRightHanded ? -vector.Z : +vector.Z, vector.Y, vector.X);
		}

		// not currently used, just added for completeness
		public Vec3 GlobalRotateForwardToSide(Vec3 globalForward)
		{
			Vec3 localForward = LocalizeDirection(globalForward);
			Vec3 localSide = LocalRotateForwardToSide(localForward);
			return GlobalizeDirection(localSide);
		}
	}
}
