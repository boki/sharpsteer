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
using Bnoerj.AI.Steering;
using Microsoft.Xna.Framework.Graphics;

namespace Bnoerj.SharpSteer.LowSpeedTurn
{
	public class LowSpeedTurn : SimpleVehicle
	{
		// constructor
		public LowSpeedTurn()
		{
			Reset();
		}

		// reset state
		public override void Reset()
		{
			// reset vehicle state
			base.Reset();

			// speed along Forward direction.
			Speed = startSpeed;

			// initial position along X axis
			SetPosition(startX, 0, 0);

			// steering force clip magnitude
			MaxForce = 0.3f;

			// velocity  clip magnitude
			MaxSpeed = 1.5f;

			// for next instance: step starting location
			startX += 2;

			// for next instance: step speed
			startSpeed += 0.15f;

			// 15 seconds and 150 points along the trail
			SetTrailParameters(15, 150);
		}

		// draw into the scene
		public void Draw()
		{
			Drawing.DrawBasic2dCircularVehicle(this, Color.Gray);
			DrawTrail();
		}

		// per frame simulation update
		public void Update(float currentTime, float elapsedTime)
		{
			ApplySteeringForce(Steering, elapsedTime);

			// annotation
			AnnotationVelocityAcceleration();
			RecordTrailVertex(currentTime, Position);
		}

		// reset starting positions
		public static void ResetStarts()
		{
			startX = 0;
			startSpeed = 0;
		}

		// constant steering force
		public Vec3 Steering
		{
			get { return new Vec3(1, 0, -1); }
		}

		// for stepping the starting conditions for next vehicle
		static float startX;
		static float startSpeed;
	}
}
