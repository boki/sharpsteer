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

namespace Bnoerj.SharpSteer.Ctf
{
	public class CtfEnemy : CtfBase
	{
		// constructor
		public CtfEnemy()
		{
			Reset();
		}

		// reset state
		public override void Reset()
		{
			base.Reset();
			BodyColor = new Color((byte)(255.0f * 0.6f), (byte)(255.0f * 0.4f), (byte)(255.0f * 0.4f)); // redish
		}

		// per frame simulation update
		public void Update(float currentTime, float elapsedTime)
		{
			// determine upper bound for pursuit prediction time
			float seekerToGoalDist = Vec3.Distance(Globals.HomeBaseCenter, Globals.Seeker.Position);
			float adjustedDistance = seekerToGoalDist - Radius - Globals.HomeBaseRadius;
			float seekerToGoalTime = ((adjustedDistance < 0) ? 0 : (adjustedDistance / Globals.Seeker.Speed));
			float maxPredictionTime = seekerToGoalTime * 0.9f;

			// determine steering (pursuit, obstacle avoidance, or braking)
			Vec3 steer = Vec3.Zero;
			if (Globals.Seeker.State == SeekerState.Running)
			{
				Vec3 avoidance = SteerToAvoidObstacles(Globals.AvoidancePredictTimeMin, AllObstacles);

				// saved for annotation
				Avoiding = (avoidance == Vec3.Zero);

				if (Avoiding)
					steer = SteerForPursuit(Globals.Seeker, maxPredictionTime);
				else
					steer = avoidance;
			}
			else
			{
				ApplyBrakingForce(Globals.BrakingRate, elapsedTime);
			}
			ApplySteeringForce(steer, elapsedTime);

			// annotation
			AnnotationVelocityAcceleration();
			RecordTrailVertex(currentTime, Position);

			// detect and record interceptions ("tags") of seeker
			float seekerToMeDist = Vec3.Distance(Position, Globals.Seeker.Position);
			float sumOfRadii = Radius + Globals.Seeker.Radius;
			if (seekerToMeDist < sumOfRadii)
			{
				if (Globals.Seeker.State == SeekerState.Running) Globals.Seeker.State = SeekerState.Tagged;

				// annotation:
				if (Globals.Seeker.State == SeekerState.Tagged)
				{
					Color color = new Color((byte)(255.0f * 0.8f), (byte)(255.0f * 0.5f), (byte)(255.0f * 0.5f));
					AnnotationXZDisk(sumOfRadii, (Position + Globals.Seeker.Position) / 2, color, 20);
				}
			}
		}
	}
}
