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
using Microsoft.Xna.Framework.Graphics;

namespace Bnoerj.SharpSteer.OneTurning
{
	public class OneTurning : SimpleVehicle
	{
		// constructor
		public OneTurning()
		{
			Reset();
		}

		// reset state
		public override void Reset()
		{
			base.Reset(); // reset the vehicle 
			Speed = 1.5f;         // speed along Forward direction.
			MaxForce = 0.3f;      // steering force is clipped to this magnitude
			MaxSpeed = 5;         // velocity is clipped to this magnitude
			ClearTrailHistory();    // prevent long streaks due to teleportation 
		}

		// per frame simulation update
		public void Update(float currentTime, float elapsedTime)
		{
			ApplySteeringForce(new Vec3(-2, 0, -3), elapsedTime);
			AnnotationVelocityAcceleration();
			RecordTrailVertex(currentTime, Position);
		}

		// draw this character/vehicle into the scene
		public void Draw()
		{
			Drawing.DrawBasic2dCircularVehicle(this, Color.Gray);
			DrawTrail();
		}
	}
}
