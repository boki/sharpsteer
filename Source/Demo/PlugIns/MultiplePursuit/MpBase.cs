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
	public class MpBase : SimpleVehicle
	{
		// constructor
		public MpBase()
		{
			Reset();
		}

		// reset state
		public override void Reset()
		{
			base.Reset();			// reset the vehicle 
			Speed = 0;            // speed along Forward direction.
			MaxForce = 5.0f;       // steering force is clipped to this magnitude
			MaxSpeed = 3.0f;       // velocity is clipped to this magnitude
			ClearTrailHistory();    // prevent long streaks due to teleportation 
			GaudyPursuitAnnotation = true; // select use of 9-color annotation
		}

		// draw into the scene
		public void Draw()
		{
			Drawing.DrawBasic2dCircularVehicle(this, bodyColor);
			DrawTrail();
		}

		// for draw method
		protected Color bodyColor;
	}
}
