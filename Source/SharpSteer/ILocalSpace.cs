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
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;
using Microsoft.Xna.Framework.Content;

namespace Bnoerj.AI.Steering
{
	public interface ILocalSpace
	{
        // accessors(get and set) for side, up, forward and position
		Vec3 Side { get; set; }
		Vec3 Up { get; set; }
        Vec3 Forward { get; set; }
		Vec3 Position { get; set; }

        // use right-(or left-)handed coordinate space
		bool IsRightHanded { get; }

        // reset transform to identity
        void ResetLocalSpace();

        // transform a direction in global space to its equivalent in local space
        Vec3 LocalizeDirection(Vec3 globalDirection);

        // transform a point in global space to its equivalent in local space
        Vec3 LocalizePosition(Vec3 globalPosition);

        // transform a point in local space to its equivalent in global space
        Vec3 GlobalizePosition(Vec3 localPosition);

        // transform a direction in local space to its equivalent in global space
        Vec3 GlobalizeDirection(Vec3 localDirection);

        // set "side" basis vector to normalized cross product of forward and up
        void SetUnitSideFromForwardAndUp();

        // regenerate the orthonormal basis vectors given a new forward
        //(which is expected to have unit length)
        void RegenerateOrthonormalBasisUF(Vec3 newUnitForward);

        // for when the new forward is NOT of unit length
        void RegenerateOrthonormalBasis(Vec3 newForward);

        // for supplying both a new forward and and new up
        void RegenerateOrthonormalBasis(Vec3 newForward, Vec3 newUp);

        // rotate 90 degrees in the direction implied by rightHanded()
        Vec3 LocalRotateForwardToSide(Vec3 vector);
        Vec3 GlobalRotateForwardToSide(Vec3 globalForward);
	}
}
