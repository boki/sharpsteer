// Copyright (c) 2002-2003, Sony Computer Entertainment America
// Copyright (c) 2002-2003, Craig Reynolds <craig_reynolds@playstation.sony.com>
// Copyright (C) 2007 Bjoern Graf <bjoern.graf@gmx.net>
// Copyright (C) 2007 Michael Coles <michael@digini.com>
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
		Vector3 Side { get; set; }
		Vector3 Up { get; set; }
        Vector3 Forward { get; set; }
		Vector3 Position { get; set; }

        // use right-(or left-)handed coordinate space
		bool IsRightHanded { get; }

        // reset transform to identity
        void ResetLocalSpace();

        // transform a direction in global space to its equivalent in local space
        Vector3 LocalizeDirection(Vector3 globalDirection);

        // transform a point in global space to its equivalent in local space
        Vector3 LocalizePosition(Vector3 globalPosition);

        // transform a point in local space to its equivalent in global space
        Vector3 GlobalizePosition(Vector3 localPosition);

        // transform a direction in local space to its equivalent in global space
        Vector3 GlobalizeDirection(Vector3 localDirection);

        // set "side" basis vector to normalized cross product of forward and up
        void SetUnitSideFromForwardAndUp();

        // regenerate the orthonormal basis vectors given a new forward
        //(which is expected to have unit length)
        void RegenerateOrthonormalBasisUF(Vector3 newUnitForward);

        // for when the new forward is NOT of unit length
        void RegenerateOrthonormalBasis(Vector3 newForward);

        // for supplying both a new forward and and new up
        void RegenerateOrthonormalBasis(Vector3 newForward, Vector3 newUp);

        // rotate 90 degrees in the direction implied by rightHanded()
        Vector3 LocalRotateForwardToSide(Vector3 v);
        Vector3 GlobalRotateForwardToSide(Vector3 globalForward);
	}
}
