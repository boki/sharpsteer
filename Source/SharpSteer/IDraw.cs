using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;

namespace Bnoerj.AI.Steering
{
	public interface IDraw
	{
		void Line(Vec3 startPoint, Vec3 endPoint, Color color);
		void LineAlpha(Vec3 startPoint, Vec3 endPoint, Color color, float alpha);
		void CircleOrDisk(float radius, Vec3 axis, Vec3 center, Color color, int segments, bool filled, bool in3d);
	}
}
