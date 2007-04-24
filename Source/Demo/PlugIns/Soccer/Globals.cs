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
using System.Text;

namespace Bnoerj.SharpSteer.Soccer
{
	class Globals
	{
		public static Vec3[] PlayerPosition = new Vec3[] {
			new Vec3(4,0,0),
			new Vec3(7,0,-5),
			new Vec3(7,0,5),
			new Vec3(10,0,-3),
			new Vec3(10,0,3),
			new Vec3(15,0, -8),
			new Vec3(15,0,0),
			new Vec3(15,0,8),
			new Vec3(4,0,0)
		};
	}
}
