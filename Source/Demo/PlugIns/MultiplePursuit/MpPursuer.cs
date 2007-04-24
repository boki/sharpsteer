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

namespace Bnoerj.SharpSteer.MultiplePursuit
{
	public class MpPursuer : MpBase
	{
		// constructor
		public MpPursuer(MpWanderer w)
		{
			wanderer = w;
			Reset();
		}

		// reset state
		public override void Reset()
		{
			base.Reset();
			bodyColor = new Color((byte)(255.0f * 0.6f), (byte)(255.0f * 0.4f), (byte)(255.0f * 0.4f)); // redish
			if(wanderer != null) RandomizeStartingPositionAndHeading();
		}

		// one simulation step
		public void Update(float currentTime, float elapsedTime)
		{
			// when pursuer touches quarry ("wanderer"), reset its position
			float d = Vec3.Distance(Position, wanderer.Position);
			float r = Radius + wanderer.Radius;
			if (d < r) Reset();

			const float maxTime = 20; // xxx hard-to-justify value
			ApplySteeringForce(SteerForPursuit(wanderer, maxTime), elapsedTime);

			// for annotation
			RecordTrailVertex(currentTime, Position);
		}

		// reset position
		public void RandomizeStartingPositionAndHeading()
		{
			// randomize position on a ring between inner and outer radii
			// centered around the home base
			const float inner = 20;
			const float outer = 30;
			float radius = Utilities.Random(inner, outer);
			Vec3 randomOnRing = Vec3.RandomUnitVectorOnXZPlane() * radius;
			Position = (wanderer.Position + randomOnRing);

			// randomize 2D heading
			RandomizeHeadingOnXZPlane();
		}

		MpWanderer wanderer;
	}
}
