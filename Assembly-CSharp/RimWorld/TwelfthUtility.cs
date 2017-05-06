using System;
using System.Collections.Generic;
using Verse;

namespace RimWorld
{
	public static class TwelfthUtility
	{
		public static Season GetSeason(this Twelfth twelfth, float latitude)
		{
			if (latitude >= 0f)
			{
				switch (twelfth)
				{
				case Twelfth.First:
					return Season.Spring;
				case Twelfth.Second:
					return Season.Spring;
				case Twelfth.Third:
					return Season.Spring;
				case Twelfth.Fourth:
					return Season.Summer;
				case Twelfth.Fifth:
					return Season.Summer;
				case Twelfth.Sixth:
					return Season.Summer;
				case Twelfth.Seventh:
					return Season.Fall;
				case Twelfth.Eighth:
					return Season.Fall;
				case Twelfth.Ninth:
					return Season.Fall;
				case Twelfth.Tenth:
					return Season.Winter;
				case Twelfth.Eleventh:
					return Season.Winter;
				case Twelfth.Twelfth:
					return Season.Winter;
				}
			}
			else
			{
				switch (twelfth)
				{
				case Twelfth.First:
					return Season.Fall;
				case Twelfth.Second:
					return Season.Fall;
				case Twelfth.Third:
					return Season.Fall;
				case Twelfth.Fourth:
					return Season.Winter;
				case Twelfth.Fifth:
					return Season.Winter;
				case Twelfth.Sixth:
					return Season.Winter;
				case Twelfth.Seventh:
					return Season.Spring;
				case Twelfth.Eighth:
					return Season.Spring;
				case Twelfth.Ninth:
					return Season.Spring;
				case Twelfth.Tenth:
					return Season.Summer;
				case Twelfth.Eleventh:
					return Season.Summer;
				case Twelfth.Twelfth:
					return Season.Summer;
				}
			}
			return Season.Undefined;
		}

		public static Quadrum GetQuadrum(this Twelfth twelfth)
		{
			switch (twelfth)
			{
			case Twelfth.First:
				return Quadrum.Aprimay;
			case Twelfth.Second:
				return Quadrum.Aprimay;
			case Twelfth.Third:
				return Quadrum.Aprimay;
			case Twelfth.Fourth:
				return Quadrum.Jugust;
			case Twelfth.Fifth:
				return Quadrum.Jugust;
			case Twelfth.Sixth:
				return Quadrum.Jugust;
			case Twelfth.Seventh:
				return Quadrum.Septober;
			case Twelfth.Eighth:
				return Quadrum.Septober;
			case Twelfth.Ninth:
				return Quadrum.Septober;
			case Twelfth.Tenth:
				return Quadrum.Decembary;
			case Twelfth.Eleventh:
				return Quadrum.Decembary;
			case Twelfth.Twelfth:
				return Quadrum.Decembary;
			default:
				return Quadrum.Undefined;
			}
		}

		public static Twelfth NextTwelfth(this Twelfth twelfth)
		{
			return (twelfth + 1) % Twelfth.Undefined;
		}

		public static float GetBeginningYearPct(this Twelfth twelfth)
		{
			return (float)twelfth / 12f;
		}

		public static Twelfth FindStartingWarmTwelfth(int tile)
		{
			Twelfth twelfth = GenTemperature.EarliestTwelfthInAverageTemperatureRange(tile, 16f, 9999f);
			if (twelfth == Twelfth.Undefined)
			{
				twelfth = Season.Summer.GetFirstTwelfth(Find.WorldGrid.LongLatOf(tile).y);
			}
			return twelfth;
		}

		public static Twelfth GetLeftMostTwelfth(List<Twelfth> twelfths, Twelfth rootTwelfth)
		{
			if (twelfths.Count >= 12)
			{
				return Twelfth.Undefined;
			}
			Twelfth result;
			do
			{
				result = rootTwelfth;
				rootTwelfth = TwelfthUtility.TwelfthBefore(rootTwelfth);
			}
			while (twelfths.Contains(rootTwelfth));
			return result;
		}

		public static Twelfth GetRightMostTwelfth(List<Twelfth> twelfths, Twelfth rootTwelfth)
		{
			if (twelfths.Count >= 12)
			{
				return Twelfth.Undefined;
			}
			Twelfth m;
			do
			{
				m = rootTwelfth;
				rootTwelfth = TwelfthUtility.TwelfthAfter(rootTwelfth);
			}
			while (twelfths.Contains(rootTwelfth));
			return TwelfthUtility.TwelfthAfter(m);
		}

		public static Twelfth TwelfthBefore(Twelfth m)
		{
			if (m == Twelfth.First)
			{
				return Twelfth.Twelfth;
			}
			return (Twelfth)(m - Twelfth.Second);
		}

		public static Twelfth TwelfthAfter(Twelfth m)
		{
			if (m == Twelfth.Twelfth)
			{
				return Twelfth.First;
			}
			return m + 1;
		}
	}
}