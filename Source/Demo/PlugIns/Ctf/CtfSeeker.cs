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

namespace Bnoerj.SharpSteer.Ctf
{
	public class CtfSeeker : CtfBase
	{
		// constructor
		public CtfSeeker()
			: base()
		{
			Reset();
		}

		// reset state
		public override void Reset()
		{
			base.Reset();
			BodyColor = new Color((byte)(255.0f * 0.4f), (byte)(255.0f * 0.4f), (byte)(255.0f * 0.6f)); // blueish
			Globals.Seeker = this;
			State = SeekerState.Running;
			evading = false;
		}

		// per frame simulation update
		public void Update(float currentTime, float elapsedTime)
		{
			// do behavioral state transitions, as needed
			UpdateState(currentTime);

			// determine and apply steering/braking forces
			Vec3 steer = Vec3.Zero;
			if (State == SeekerState.Running)
			{
				steer = SteeringForSeeker();
			}
			else
			{
				ApplyBrakingForce(Globals.BrakingRate, elapsedTime);
			}
			ApplySteeringForce(steer, elapsedTime);

			// annotation
			AnnotationVelocityAcceleration();
			RecordTrailVertex(currentTime, Position);
		}

		// is there a clear path to the goal?
		public bool IsPathToGoalClear()
		{
			float sideThreshold = Radius * 8.0f;
			float behindThreshold = Radius * 2.0f;

			Vec3 goalOffset = Globals.HomeBaseCenter - Position;
			float goalDistance = goalOffset.Length();
			Vec3 goalDirection = goalOffset / goalDistance;

			bool goalIsAside = IsAside(Globals.HomeBaseCenter, 0.5f);

			// for annotation: loop over all and save result, instead of early return 
			bool xxxReturn = true;

			// loop over enemies
			for (int i = 0; i < Globals.CtfEnemyCount; i++)
			{
				// short name for this enemy
				CtfEnemy e = Globals.CtfEnemies[i];
				float eDistance = Vec3.Distance(Position, e.Position);
				float timeEstimate = 0.3f * eDistance / e.Speed; //xxx
				Vec3 eFuture = e.PredictFuturePosition(timeEstimate);
				Vec3 eOffset = eFuture - Position;
				float alongCorridor = goalDirection.Dot(eOffset);
				bool inCorridor = ((alongCorridor > -behindThreshold) && (alongCorridor < goalDistance));
				float eForwardDistance = Forward.Dot(eOffset);

				// xxx temp move this up before the conditionals
				AnnotationXZCircle(e.Radius, eFuture, Globals.ClearPathColor, 20); //xxx

				// consider as potential blocker if within the corridor
				if (inCorridor)
				{
					Vec3 perp = eOffset - (goalDirection * alongCorridor);
					float acrossCorridor = perp.Length();
					if (acrossCorridor < sideThreshold)
					{
						// not a blocker if behind us and we are perp to corridor
						float eFront = eForwardDistance + e.Radius;

						//annotationLine (position, forward*eFront, gGreen); // xxx
						//annotationLine (e.position, forward*eFront, gGreen); // xxx

						// xxx
						// std::ostringstream message;
						// message << "eFront = " << std::setprecision(2)
						//         << std::setiosflags(std::ios::fixed) << eFront << std::ends;
						// draw2dTextAt3dLocation (*message.str(), eFuture, gWhite);

						bool eIsBehind = eFront < -behindThreshold;
						bool eIsWayBehind = eFront < (-2 * behindThreshold);
						bool safeToTurnTowardsGoal = ((eIsBehind && goalIsAside) || eIsWayBehind);

						if (!safeToTurnTowardsGoal)
						{
							// this enemy blocks the path to the goal, so return false
							AnnotationLine(Position, e.Position, Globals.ClearPathColor);
							// return false;
							xxxReturn = false;
						}
					}
				}
			}

			// no enemies found along path, return true to indicate path is clear
			// clearPathAnnotation (sideThreshold, behindThreshold, goalDirection);
			// return true;
			//if (xxxReturn)
			ClearPathAnnotation(sideThreshold, behindThreshold, goalDirection);
			return xxxReturn;
		}

		public Vec3 SteeringForSeeker()
		{
			// determine if obstacle avodiance is needed
			bool clearPath = IsPathToGoalClear();
			AdjustObstacleAvoidanceLookAhead(clearPath);
			Vec3 obstacleAvoidance = SteerToAvoidObstacles(Globals.AvoidancePredictTime, AllObstacles);

			// saved for annotation
			Avoiding = (obstacleAvoidance != Vec3.Zero);

			if (Avoiding)
			{
				// use pure obstacle avoidance if needed
				return obstacleAvoidance;
			}
			else
			{
				// otherwise seek home base and perhaps evade defenders
				Vec3 seek = SteerForSeek2(Globals.HomeBaseCenter);
				if (clearPath)
				{
					// we have a clear path (defender-free corridor), use pure seek

					// xxx experiment 9-16-02
					Vec3 s = Vec3.LimitMaxDeviationAngle(seek, 0.707f, Forward);

					AnnotationLine(Position, Position + (s * 0.2f), Globals.SeekColor);
					return s;
				}
				else
				{
#if TESTING_CODE
					if (false) // xxx testing new evade code xxx
					{
						// combine seek and (forward facing portion of) evasion
						Vec3 evade = steerToEvadeAllDefenders();
						Vec3 steer = seek + Vec3.limitMaxDeviationAngle(evade, 0.5f, forward());

						// annotation: show evasion steering force
						annotation.annotationLine(position(), position() + (steer * 0.2f), Globals.evadeColor);
						return steer;
					}
					else
#endif
					{
						Vec3 evade = XXXSteerToEvadeAllDefenders();
						Vec3 steer = Vec3.LimitMaxDeviationAngle(seek + evade, 0.707f, Forward);

						AnnotationLine(Position, Position + seek, Color.Red);
						AnnotationLine(Position, Position + evade, Color.Green);

						// annotation: show evasion steering force
						AnnotationLine(Position, Position + (steer * 0.2f), Globals.EvadeColor);
						return steer;
					}
				}
			}
		}

		public void UpdateState(float currentTime)
		{
			// if we reach the goal before being tagged, switch to atGoal state
			if (State == SeekerState.Running)
			{
				float baseDistance = Vec3.Distance(Position, Globals.HomeBaseCenter);
				if (baseDistance < (Radius + Globals.HomeBaseRadius)) State = SeekerState.AtGoal;
			}

			// update lastRunningTime (holds off reset time)
			if (State == SeekerState.Running)
			{
				lastRunningTime = currentTime;
			}
			else
			{
				float resetDelay = 4;
				float resetTime = lastRunningTime + resetDelay;
				if (currentTime > resetTime)
				{
					// xxx a royal hack (should do this internal to CTF):
					Demo.QueueDelayedResetPlugInXXX();
				}
			}
		}

		public override void Draw()
		{
			// first call the draw method in the base class
			base.Draw();

			// select string describing current seeker state
			String seekerStateString = "";
			switch (State)
			{
			case SeekerState.Running:
				if (Avoiding)
					seekerStateString = "avoid obstacle";
				else if (evading)
					seekerStateString = "seek and evade";
				else
					seekerStateString = "seek goal";
				break;
			case SeekerState.Tagged:
				seekerStateString = "tagged";
				break;
			case SeekerState.AtGoal:
				seekerStateString = "reached goal";
				break;
			}

			// annote seeker with its state as text
			Vec3 textOrigin = Position + new Vec3(0, 0.25f, 0);
			StringBuilder annote = new StringBuilder();
			annote.Append(seekerStateString);
			annote.AppendFormat("\n{0:0.00}", Speed);
			Drawing.Draw2dTextAt3dLocation(annote.ToString(), textOrigin, Color.White);

			// display status in the upper left corner of the window
			StringBuilder status = new StringBuilder();
			status.Append(seekerStateString);
			status.AppendFormat("\n{0} obstacles [F1/F2]", obstacleCount);
			status.AppendFormat("\n{0} restarts", Globals.ResetCount);
			Vec3 screenLocation = new Vec3(15, 50, 0);
			Drawing.Draw2dTextAt2dLocation(status.ToString(), screenLocation, Color.LightGray);
		}

		public Vec3 SteerToEvadeAllDefenders()
		{
			Vec3 evade = Vec3.Zero;
			float goalDistance = Vec3.Distance(Globals.HomeBaseCenter, Position);

			// sum up weighted evasion
			for (int i = 0; i < Globals.CtfEnemyCount; i++)
			{
				CtfEnemy e = Globals.CtfEnemies[i];
				Vec3 eOffset = e.Position - Position;
				float eDistance = eOffset.Length();

				float eForwardDistance = Forward.Dot(eOffset);
				float behindThreshold = Radius * 2;
				bool behind = eForwardDistance < behindThreshold;
				if ((!behind) || (eDistance < 5))
				{
					if (eDistance < (goalDistance * 1.2)) //xxx
					{
						// const float timeEstimate = 0.5f * eDistance / e.speed;//xxx
						float timeEstimate = 0.15f * eDistance / e.Speed;//xxx
						Vec3 future = e.PredictFuturePosition(timeEstimate);

						AnnotationXZCircle(e.Radius, future, Globals.EvadeColor, 20); // xxx

						Vec3 offset = future - Position;
						Vec3 lateral = offset.PerpendicularComponent(Forward);
						float d = lateral.Length();
						float weight = -1000 / (d * d);
						evade += (lateral / d) * weight;
					}
				}
			}
			return evade;
		}

		public Vec3 XXXSteerToEvadeAllDefenders()
		{
			// sum up weighted evasion
			Vec3 evade = Vec3.Zero;
			for (int i = 0; i < Globals.CtfEnemyCount; i++)
			{
				CtfEnemy e = Globals.CtfEnemies[i];
				Vec3 eOffset = e.Position - Position;
				float eDistance = eOffset.Length();

				// xxx maybe this should take into account e's heading? xxx
				float timeEstimate = 0.5f * eDistance / e.Speed; //xxx
				Vec3 eFuture = e.PredictFuturePosition(timeEstimate);

				// annotation
				AnnotationXZCircle(e.Radius, eFuture, Globals.EvadeColor, 20);

				// steering to flee from eFuture (enemy's future position)
				Vec3 flee = SteerForFlee2(eFuture);

				float eForwardDistance = Forward.Dot(eOffset);
				float behindThreshold = Radius * -2;

				float distanceWeight = 4 / eDistance;
				float forwardWeight = ((eForwardDistance > behindThreshold) ? 1.0f : 0.5f);

				Vec3 adjustedFlee = flee * distanceWeight * forwardWeight;

				evade += adjustedFlee;
			}
			return evade;
		}

		public void AdjustObstacleAvoidanceLookAhead(bool clearPath)
		{
			if (clearPath)
			{
				evading = false;
				float goalDistance = Vec3.Distance(Globals.HomeBaseCenter, Position);
				bool headingTowardGoal = IsAhead(Globals.HomeBaseCenter, 0.98f);
				bool isNear = (goalDistance / Speed) < Globals.AvoidancePredictTimeMax;
				bool useMax = headingTowardGoal && !isNear;
				Globals.AvoidancePredictTime = (useMax ? Globals.AvoidancePredictTimeMax : Globals.AvoidancePredictTimeMin);
			}
			else
			{
				evading = true;
				Globals.AvoidancePredictTime = Globals.AvoidancePredictTimeMin;
			}
		}

		public void ClearPathAnnotation(float sideThreshold, float behindThreshold, Vec3 goalDirection)
		{
			Vec3 behindSide = Side * sideThreshold;
			Vec3 behindBack = Forward * -behindThreshold;
			Vec3 pbb = Position + behindBack;
			Vec3 gun = LocalRotateForwardToSide(goalDirection);
			Vec3 gn = gun * sideThreshold;
			Vec3 hbc = Globals.HomeBaseCenter;
			AnnotationLine(pbb + gn, hbc + gn, Globals.ClearPathColor);
			AnnotationLine(pbb - gn, hbc - gn, Globals.ClearPathColor);
			AnnotationLine(hbc - gn, hbc + gn, Globals.ClearPathColor);
			AnnotationLine(pbb - gn, pbb + gn, Globals.ClearPathColor);
			//AnnotationLine(pbb - behindSide, pbb + behindSide, Globals.clearPathColor);
		}

		public SeekerState State;
		bool evading; // xxx store steer sub-state for anotation
		float lastRunningTime; // for auto-reset
	}
}
