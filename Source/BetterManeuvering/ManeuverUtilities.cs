﻿using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace BetterManeuvering
{
	public static class ManeuverUtilities
	{
		public static double parseTime(string s)
		{
			double UT = 0;

			UT += parseTime(s, 'y') * KSPUtil.dateTimeFormatter.Year;

			UT += parseTime(s, 'd') * KSPUtil.dateTimeFormatter.Day;

			UT += parseTime(s, 'h') * 3600;

			UT += parseTime(s, 'm') * 60;

			UT += parseTime(s, 's');

			UT += Planetarium.GetUniversalTime();

			return UT;
		}

		private static double parseTime(string s, char c)
		{
			string literal = @"\d+(?=" + c + ")";

			Regex time = new Regex(@literal);

			s = time.Match(s).Value;

			double d = 0;

			if (double.TryParse(s, out d))
				return d;

			return d;
		}

		public static Orbit getRefPatch(double UT, PatchedConicSolver pcs)
		{
			if (pcs == null)
				return null;

			if (pcs.flightPlan.Count <= 0)
				return pcs.FindPatchContainingUT(UT);

			int l = pcs.flightPlan.Count;
			int start = 0;

			for (int i = 0; i < l; i++)
			{
				Orbit patch = pcs.flightPlan[i];

				if (patch.patchStartTransition != Orbit.PatchTransitionType.MANEUVER)
					continue;

				start = i;
				break;
			}

			for (int i = start; i < l; i++)
			{
				Orbit patch = pcs.flightPlan[i];

				double nextStart = patch.nextPatch == null ? patch.EndUT : patch.nextPatch.StartUT;

				//ManeuverController.maneuverLog("Patch: {0} - Start UT: {1:F2} - End UT: {2:F2} - Next Start UT: {3:F2}", logLevels.log, i, patch.StartUT, patch.EndUT, nextStart);

				if (UT >= patch.StartUT && UT < nextStart)
					return patch;
				else if (patch.patchEndTransition == Orbit.PatchTransitionType.FINAL)
					return patch;
			}

			return null;
		}

		public static bool parseApo(double UT, Orbit o, double min, double max, out double apo)
		{
			apo = o.timeToAp + o.StartUT;

			//ManeuverController.maneuverLog("Apo: {0:F2} - SOI UT: {1:F2}", logLevels.log, apo, o.UTsoi);

			if (o.eccentricity >= 1)
				return false;

			if (o.ApR > o.referenceBody.sphereOfInfluence)
				return false;

			if (o.timeToAp < 0)
				return false;

			if (o.UTsoi > 0 && apo > o.UTsoi)
				return false;

			if (apo >= min && apo <= max)
				return true;

			return false;
		}

		public static bool parsePeri(double UT, Orbit o, double min, double max, out double peri)
		{
			peri = o.timeToPe + o.StartUT;

			//ManeuverController.maneuverLog("Peri: {0:F2} - SOI UT: {1:F2}", logLevels.log, peri, o.UTsoi);

			if (o.PeR < 0)
				return false;

			if (o.timeToPe < 0)
				return false;

			if (o.UTsoi > 0 && peri > o.UTsoi)
				return false;

			if (peri >= min && peri <= max)
				return true;

			return false;
		}

		public static bool parseEqAsc(double UT, Orbit o, double min, double max, out double eqAsc)
		{
			if (Math.Abs(o.inclination) < 0.001)
			{
				eqAsc = 0;
				return false;
			}

			eqAsc = EqAscTime(o);

			//ManeuverController.maneuverLog("EqAsc: {0:F2} - SOI UT: {1:F2}", logLevels.log, eqAsc, o.UTsoi);

			if ((o.UTsoi > 0 && eqAsc > o.UTsoi) || eqAsc < o.StartUT)
				return false;

			if (eqAsc >= min && eqAsc <= max)
				return true;

			return false;
		}

		public static double EqAscTime(Orbit o)
		{
			double eqAsc = o.GetDTforTrueAnomaly((360 - o.argumentOfPeriapsis) * Mathf.Deg2Rad, o.period);

			if (eqAsc < 0)
				eqAsc += o.period;

			return eqAsc + o.StartUT;
		}

		public static bool parseEqDesc(double UT, Orbit o, double min, double max, out double eqDesc)
		{
			if (Math.Abs(o.inclination) < 0.001)
			{
				eqDesc = 0;
				return false;
			}

			eqDesc = EqDescTime(o);

			//ManeuverController.maneuverLog("EqDesc: {0:F2} - SOI UT: {1:F2}", logLevels.log, eqDesc, o.UTsoi);

			if ((o.UTsoi > 0 && eqDesc > o.UTsoi) || eqDesc < o.StartUT)
				return false;

			if (eqDesc >= min && eqDesc <= max)
				return true;

			return false;
		}

		public static double EqDescTime(Orbit o)
		{
			double eqDesc = o.GetDTforTrueAnomaly((180 - o.argumentOfPeriapsis) * Mathf.Deg2Rad, o.period);

			if (eqDesc < 0)
				eqDesc += o.period;

			return eqDesc + o.StartUT;
		}

		public static bool parseRelAsc(double UT, Orbit o, ITargetable tgt, double min, double max, out double relAsc)
		{
			relAsc = RelAscTime(o, tgt);

			//ManeuverController.maneuverLog("RelAsc: {0:F2} - SOI UT: {1:F2}", logLevels.log, relAsc, o.UTsoi);

			if ((o.UTsoi > 0 && relAsc > o.UTsoi) || relAsc < o.StartUT)
				return false;

			if (relAsc >= min && relAsc <= max)
				return true;

			return false;
		}

		public static double RelAscTime(Orbit o, ITargetable tgt)
		{
			Vector3d node = Vector3d.Cross(tgt.GetOrbit().GetOrbitNormal(), o.GetOrbitNormal());

			double anomaly = o.GetTrueAnomalyOfZupVector(node);

			double relAsc = o.GetDTforTrueAnomaly(anomaly, o.period);

			if (relAsc < 0)
				relAsc += o.period;

			return relAsc + o.StartUT;
		}

		public static bool parseRelDesc(double UT, Orbit o, ITargetable tgt, double min, double max, out double relDesc)
		{
			relDesc = RelDescTime(o, tgt);

			//ManeuverController.maneuverLog("RelDesc: {0:F2} - SOI UT: {1:F2}", logLevels.log, relDesc, o.UTsoi);

			if ((o.UTsoi > 0 && relDesc > o.UTsoi) || relDesc < o.StartUT)
				return false;

			if (relDesc >= min && relDesc <= max)
				return true;

			return false;
		}

		public static double RelDescTime(Orbit o, ITargetable tgt)
		{
			Vector3d node = Vector3d.Cross(o.GetOrbitNormal(), tgt.GetOrbit().GetOrbitNormal());

			double anomaly = o.GetTrueAnomalyOfZupVector(node);

			double relDesc = o.GetDTforTrueAnomaly(anomaly, o.period);

			if (relDesc < 0)
				relDesc += o.period;

			return relDesc + o.StartUT;
		}

		public static bool parseApproach(double UT, Orbit o, Orbit tgt, bool vessel, double min, double max, out double app)
		{
			if (vessel)
			{
				app = closestVessel(UT, o, tgt, false, min, max);

				if ((o.UTsoi > 0 && app > o.UTsoi) || app < o.StartUT)
					return false;

				if (app >= min && app <= max)
					return true;
			}

			if (o.closestTgtApprUT > 0)
			{
				app = o.closestTgtApprUT;

				if ((o.UTsoi > 0 && app > o.UTsoi) || app < o.StartUT)
					return false;

				if (app >= min && app <= max)
					return true;
			}

			app = 0;

			return false;
		}

		public static double closestVessel(double UT, Orbit o, Orbit tgt, bool closest, double min, double max)
		{
			double appUT = 0;

			if (o.referenceBody != tgt.referenceBody)
				return 0;

			if (!Orbit.PeApIntersects(o, tgt, 20000))
				return 0;

			double d1 = 0;
			double d2 = 0;
			double dT1 = 0;
			double d4 = 0;
			double dT2 = 0;
			double d6 = 0;
			int i1 = 0;

			int intersects = Orbit.FindClosestPoints(o, tgt, ref d1, ref d2, ref dT1, ref d4, ref dT2, ref d6, 0.001, 10, ref i1);

			double UT1 = o.StartUT + o.GetDTforTrueAnomaly(dT1, 0);
			double UT2 = o.StartUT + o.GetDTforTrueAnomaly(dT2, 0);

			if (intersects > 1)
			{
				double dist1 = double.MaxValue;

				if (PatchedConics.TAIsWithinPatchBounds(UT1, o))
				{
					Vector3d refClosest1 = o.getRelativePositionAtUT(UT1);
					Vector3d tgtClosest1 = tgt.getRelativePositionAtUT(UT1);

					dist1 = (refClosest1 - tgtClosest1).magnitude;
				}

				double dist2 = double.MaxValue;

				if (PatchedConics.TAIsWithinPatchBounds(UT2, o))
				{
					Vector3d refClosest2 = o.getRelativePositionAtUT(UT2);
					Vector3d tgtClosest2 = tgt.getRelativePositionAtUT(UT2);

					dist2 = (refClosest2 - tgtClosest2).magnitude;
				}

				if (dist1 > double.MaxValue - 1000 && dist2 > double.MaxValue - 1000)
					return 0;

				appUT = dist1 < dist2 ? UT1 : UT2;

				if (closest)
					return appUT;

				if (appUT >= min && appUT <= max)
					return appUT;

				appUT = dist1 < dist2 ? UT2 : UT1;

				if (appUT >= min && appUT <= max)
					return appUT;
			}
			else
			{
				if (!PatchedConics.TAIsWithinPatchBounds(UT1, o))
					UT1 = double.MaxValue;

				if (!PatchedConics.TAIsWithinPatchBounds(UT2, o))
					UT2 = double.MaxValue;

				if (UT1 > double.MaxValue - 1000 && UT2 > double.MaxValue - 1000)
					return 0;

				appUT = UT1 < UT2 ? UT1 : UT2;

				return appUT;
			}

			return 0;
		}

	}
}
