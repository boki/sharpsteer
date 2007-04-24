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
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Bnoerj.AI.Steering
{
	public abstract class PlugIn : IPlugIn
	{
		public abstract void Open();
		public abstract void Update(float currentTime, float elapsedTime);
		public abstract void Redraw(float currentTime, float elapsedTime);
		public abstract void Close();
		public abstract String Name { get; }
		public abstract List<IVehicle> Vehicles { get; }

		// constructor
		public PlugIn()
		{
			// save this new instance in the registry
			AddToRegistry();
		}

		// default reset method is to do a close then an open
		public virtual void Reset()
		{
			Close();
			Open();
		}

		// default sort key (after the "built ins")
		public virtual float SelectionOrderSortKey { get { return 1.0f; } }

		// default is to NOT request to be initially selected
		public virtual bool RequestInitialSelection { get { return false; } }

		// default function key handler: ignore all
		public virtual void HandleFunctionKeys(Keys key) { }

		// default "mini help": print nothing
		public virtual void PrintMiniHelpForFunctionKeys() { }

		// returns pointer to the next Plug-In in "selection order"
		public IPlugIn Next()
		{
			int i = registry.FindIndex(delegate(IPlugIn plugIn) { return plugIn == this; });
			if (i < registry.Count)
			{
				bool atEnd = (i == (registry.Count - 1));
				return registry[atEnd ? 0 : i + 1];
			}
			return null;
		}

		// format instance to characters for printing to stream
		public override string ToString()
		{
			return String.Format("<PlugIn \"{0}\">", Name);
		}

		// save this instance in the class's registry of instances
		void AddToRegistry()
		{
			registry.Add(this);
		}

		// CLASS FUNCTIONS

		// search the class registry for a Plug-In with the given name
		public static IPlugIn FindByName(String name)
		{
			if (String.IsNullOrEmpty(name) == false)
			{
				foreach (IPlugIn plugIn in registry)
				{
					String s = plugIn.Name;
					if (name == s)
					{
						return plugIn;
					}
				}
			}
			return null;
		}

		static int PlugInComparison(IPlugIn x, IPlugIn y)
		{
			float d = x.SelectionOrderSortKey - y.SelectionOrderSortKey;
			return d < 0 ? -1 : d > 0 ? 1 : 0;
		}

		// sort Plug-In registry by "selection order"
		public static void SortBySelectionOrder()
		{
			registry.Sort(PlugInComparison);
		}

		// returns pointer to default Plug-In (currently, first in registry)
		public static IPlugIn FindDefault()
		{
			// return NULL if no PlugIns exist
			if (registry.Count == 0)
				return null;

			// otherwise, return the first PlugIn that requests initial selection
			for (int i = 0; i < registry.Count; i++)
			{
				if (registry[i].RequestInitialSelection == true)
					return registry[i];
			}

			// otherwise, return the "first" PlugIn (in "selection order")
			return registry[0];
		}

		// This array stores a list of all PlugIns.  It is manipulated by the
		// constructor and destructor, and used in findByName and applyToAll.
		static List<IPlugIn> registry = new List<IPlugIn>();
	}
}
