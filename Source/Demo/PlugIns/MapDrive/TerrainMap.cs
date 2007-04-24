// Copyright (c) 2002-2003, Sony Computer Entertainment America
// Copyright (c) 2002-2003, Craig Reynolds <craig_reynolds@playstation.sony.com>
// Copyright (C) 2007 Bjoern Graf <bjoern.graf@gmx.net>
// All rights reserved.
//
// This software is licensed as described in the file license.txt, which
// you should have received as part of this distribution. The terms
// are also available at http://www.codeplex.com/SharpSteer/Project/License.aspx.

using System;
using System.Collections.Specialized;
using System.Collections.Generic;
using Bnoerj.AI.Steering;
using Microsoft.Xna.Framework.Graphics;

namespace Bnoerj.SharpSteer.MapDrive
{
	public class TerrainMap
	{
		public TerrainMap(Vec3 c, float x, float z, int r)
		{
			center = c;
			xSize = x;
			zSize = z;
			resolution = r;
			outsideValue = false;

			map = new bool[resolution * resolution];
			for (int i = 0; i < resolution * resolution; i++)
			{
				map[i] = false;
			}
		}

		// clear the map (to false)
		public void Clear()
		{
			for (int i = 0; i < resolution; i++)
				for (int j = 0; j < resolution; j++)
					SetMapBit(i, j, false);
		}

		// get and set a bit based on 2d integer map index
		public bool GetMapBit(int i, int j)
		{
			return map[MapAddress(i, j)];
		}

		public bool SetMapBit(int i, int j, bool value)
		{
			return map[MapAddress(i, j)] = value;
		}

		// get a value based on a position in 3d world space
		public bool GetMapValue(Vec3 point)
		{
			Vec3 local = point - center;
			Vec3 localXZ = local.SetYToZero();

			float hxs = xSize / 2;
			float hzs = zSize / 2;

			float x = localXZ.X;
			float z = localXZ.Z;

			bool isOut = (x > +hxs) || (x < -hxs) || (z > +hzs) || (z < -hzs);

			if (isOut)
			{
				return outsideValue;
			}
			else
			{
				float r = (float)resolution; // prevent VC7.1 warning
				int i = (int)Utilities.RemapInterval(x, -hxs, hxs, 0.0f, r);
				int j = (int)Utilities.RemapInterval(z, -hzs, hzs, 0.0f, r);
				return GetMapBit(i, j);
			}
		}

		public void xxxDrawMap()
		{
			float xs = xSize / (float)resolution;
			float zs = zSize / (float)resolution;
			Vec3 alongRow = new Vec3(xs, 0, 0);
			Vec3 nextRow = new Vec3(-xSize, 0, zs);
			Vec3 g = new Vec3((xSize - xs) / -2, 0, (zSize - zs) / -2);
			g += center;
			for (int j = 0; j < resolution; j++)
			{
				for (int i = 0; i < resolution; i++)
				{
					if (GetMapBit(i, j))
					{
						// spikes
						// Vec3 spikeTop (0, 5.0f, 0);
						// drawLine (g, g+spikeTop, gWhite);

						// squares
						float rockHeight = 0;
						Vec3 v1 = new Vec3(+xs / 2, rockHeight, +zs / 2);
						Vec3 v2 = new Vec3(+xs / 2, rockHeight, -zs / 2);
						Vec3 v3 = new Vec3(-xs / 2, rockHeight, -zs / 2);
						Vec3 v4 = new Vec3(-xs / 2, rockHeight, +zs / 2);
						// Vec3 redRockColor (0.6f, 0.1f, 0.0f);
						Color orangeRockColor = new Color((byte)(255.0f * 0.5f), (byte)(255.0f * 0.2f), (byte)(255.0f * 0.0f));
						Drawing.DrawQuadrangle(g + v1, g + v2, g + v3, g + v4, orangeRockColor);

						// pyramids
						// Vec3 top (0, xs/2, 0);
						// Vec3 redRockColor (0.6f, 0.1f, 0.0f);
						// Vec3 orangeRockColor (0.5f, 0.2f, 0.0f);
						// drawTriangle (g+v1, g+v2, g+top, redRockColor);
						// drawTriangle (g+v2, g+v3, g+top, orangeRockColor);
						// drawTriangle (g+v3, g+v4, g+top, redRockColor);
						// drawTriangle (g+v4, g+v1, g+top, orangeRockColor);
					}
					g += alongRow;
				}
				g += nextRow;
			}
		}

		public float MinSpacing()
		{
			return Math.Min(xSize, zSize) / (float)resolution;
		}

		// used to detect if vehicle body is on any obstacles
		public bool ScanLocalXZRectangle(ILocalSpace localSpace, float xMin, float xMax, float zMin, float zMax)
		{
			float spacing = MinSpacing() / 2;

			for (float x = xMin; x < xMax; x += spacing)
			{
				for (float z = zMin; z < zMax; z += spacing)
				{
					Vec3 sample = new Vec3(x, 0, z);
					Vec3 global = localSpace.GlobalizePosition(sample);
					if (GetMapValue(global)) return true;
				}
			}
			return false;
		}

		// Scans along a ray (directed line segment) on the XZ plane, sampling
		// the map for a "true" cell.  Returns the index of the first sample
		// that gets a "hit", or zero if no hits found.
		public int ScanXZray(Vec3 origin, Vec3 sampleSpacing, int sampleCount)
		{
			Vec3 samplePoint = new Vec3(origin);

			for (int i = 1; i <= sampleCount; i++)
			{
				samplePoint += sampleSpacing;
				if (GetMapValue(samplePoint)) return i;
			}

			return 0;
		}

		public int Cellwidth() { return resolution; }  // xxx cwr
		public int Cellheight() { return resolution; }  // xxx cwr
		public bool IsPassable(Vec3 point) { return !GetMapValue(point); }


		public Vec3 center;
		public float xSize;
		public float zSize;
		public int resolution;

		public bool outsideValue;

		int MapAddress(int i, int j) { return i + (j * resolution); }

		bool[] map;
	}
}
