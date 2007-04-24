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
	using SOG = List<SphericalObstacle>;  // spherical obstacle group

	public class CtfBase : SimpleVehicle
	{
		// constructor
		public CtfBase()
			: base()
		{
			Reset();
		}

		// reset state
		public override void Reset()
		{
			base.Reset();  // reset the vehicle 

			Speed = 3;             // speed along Forward direction.
			MaxForce = 3.0f;        // steering force is clipped to this magnitude
			MaxSpeed = 3.0f;        // velocity is clipped to this magnitude

			Avoiding = false;         // not actively avoiding

			RandomizeStartingPositionAndHeading();  // new starting position

			ClearTrailHistory();     // prevent long streaks due to teleportation
		}

		// draw this character/vehicle into the scene
		public virtual void Draw()
		{
			Drawing.DrawBasic2dCircularVehicle(this, BodyColor);
			DrawTrail();
		}

		// annotate when actively avoiding obstacles
		// xxx perhaps this should be a call to a general purpose annotation
		// xxx for "local xxx axis aligned box in XZ plane" -- same code in in
		// xxx Pedestrian.cpp
		public override void AnnotateAvoidObstacle(float minDistanceToCollision)
		{
			Vec3 boxSide = Side * Radius;
			Vec3 boxFront = Forward * minDistanceToCollision;
			Vec3 FR = Position + boxFront - boxSide;
			Vec3 FL = Position + boxFront + boxSide;
			Vec3 BR = Position - boxSide;
			Vec3 BL = Position + boxSide;
			AnnotationLine(FR, FL, Color.White);
			AnnotationLine(FL, BL, Color.White);
			AnnotationLine(BL, BR, Color.White);
			AnnotationLine(BR, FR, Color.White);
		}

		public void DrawHomeBase()
		{
			Vec3 up = new Vec3(0, 0.01f, 0);
			Color atColor = new Color((byte)(255.0f * 0.3f), (byte)(255.0f * 0.3f), (byte)(255.0f * 0.5f));
			Color noColor = Color.Gray;
			bool reached = Globals.CtfSeeker.State == CtfSeeker.SeekerState.AtGoal;
			Color baseColor = (reached ? atColor : noColor);
			Drawing.DrawXZDisk(Globals.HomeBaseRadius, Globals.HomeBaseCenter, baseColor, 40);
			Drawing.DrawXZDisk(Globals.HomeBaseRadius / 15, Globals.HomeBaseCenter + up, Color.Black, 20);
		}

		public void RandomizeStartingPositionAndHeading()
		{
			// randomize position on a ring between inner and outer radii
			// centered around the home base
			float rRadius = Utilities.Random(Globals.MinStartRadius, Globals.MaxStartRadius);
			Vec3 randomOnRing = Vec3.RandomUnitVectorOnXZPlane() * rRadius;
			Position = (Globals.HomeBaseCenter + randomOnRing);

			// are we are too close to an obstacle?
			if (MinDistanceToObstacle(Position) < Radius * 5)
			{
				// if so, retry the randomization (this recursive call may not return
				// if there is too little free space)
				RandomizeStartingPositionAndHeading();
			}
			else
			{
				// otherwise, if the position is OK, randomize 2D heading
				RandomizeHeadingOnXZPlane();
			}
		}

		public enum SeekerState
		{
			Running,
			Tagged,
			AtGoal
		}

		// for draw method
		public Color BodyColor;

		// xxx store steer sub-state for anotation
		public bool Avoiding;

		// dynamic obstacle registry
		public static void InitializeObstacles()
		{
			// start with 40% of possible obstacles
			if (obstacleCount == -1)
			{
				obstacleCount = 0;
				for (int i = 0; i < (maxObstacleCount * 0.4); i++) AddOneObstacle();
			}
		}

		public static void AddOneObstacle()
		{
			if (obstacleCount < maxObstacleCount)
			{
				// pick a random center and radius,
				// loop until no overlap with other obstacles and the home base
				float r;
				Vec3 c;
				float minClearance;
				float requiredClearance = Globals.Seeker.Radius * 4; // 2 x diameter
				do
				{
					r = Utilities.Random(1.5f, 4);
					c = Vec3.RandomVectorOnUnitRadiusXZDisk() * Globals.MaxStartRadius * 1.1f;
					minClearance = float.MaxValue;
					for (int so = 0; so < AllObstacles.Count; so++)
					{
						minClearance = TestOneObstacleOverlap(minClearance, r, AllObstacles[so].Radius, c, AllObstacles[so].Center);
					}

					minClearance = TestOneObstacleOverlap(minClearance, r, Globals.HomeBaseRadius - requiredClearance, c, Globals.HomeBaseCenter);
				}
				while (minClearance < requiredClearance);

				// add new non-overlapping obstacle to registry
				AllObstacles.Add(new SphericalObstacle(r, c));
				obstacleCount++;
			}
		}

		public static void RemoveOneObstacle()
		{
			if (obstacleCount > 0)
			{
				obstacleCount--;
				AllObstacles.RemoveAt(obstacleCount);
			}
		}

		public float MinDistanceToObstacle(Vec3 point)
		{
			float r = 0;
			Vec3 c = point;
			float minClearance = float.MaxValue;
			for (int so = 0; so < AllObstacles.Count; so++)
			{
				minClearance = TestOneObstacleOverlap(minClearance, r, AllObstacles[so].Radius, c, AllObstacles[so].Center);
			}
			return minClearance;
		}

		static float TestOneObstacleOverlap(float minClearance, float r, float radius, Vec3 c, Vec3 center)
		{
			float d = Vec3.Distance(c, center);
			float clearance = d - (r + radius);
			if (minClearance > clearance) minClearance = clearance;
			return minClearance;
		}

		protected static int obstacleCount = -1;
		protected const int maxObstacleCount = 100;
		public static SOG AllObstacles = new SOG();
	}
}
