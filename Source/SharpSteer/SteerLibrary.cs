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
using Microsoft.Xna.Framework.Graphics;

namespace Bnoerj.AI.Steering
{
	//FIXME: this class should not be abstract
	public abstract class SteerLibrary : Annotation
	{
		// Constructor: initializes state
		public SteerLibrary()
		{
			// set inital state
			Reset();
		}

		// reset state
		public virtual void Reset()
		{
			// initial state of wander behavior
			WanderSide = 0;
			WanderUp = 0;

			// default to non-gaudyPursuitAnnotation
			GaudyPursuitAnnotation = false;
		}

		// -------------------------------------------------- steering behaviors

		// Wander behavior
		public float WanderSide;
		public float WanderUp;

		public Vec3 SteerForWander(float dt)
		{
			// random walk WanderSide and WanderUp between -1 and +1
			float speed = 12 * dt; // maybe this (12) should be an argument?
			WanderSide = Utilities.ScalarRandomWalk(WanderSide, speed, -1, +1);
			WanderUp = Utilities.ScalarRandomWalk(WanderUp, speed, -1, +1);

			// return a pure lateral steering vector: (+/-Side) + (+/-Up)
			return (this.Side * WanderSide) + (this.Up * WanderUp);
		}

		// Seek behavior
		public Vec3 SteerForSeek(Vec3 target)
		{
			Vec3 desiredVelocity = target - this.Position;
			return desiredVelocity - this.Velocity;
		}

		// Flee behavior
		public Vec3 SteerForFlee(Vec3 target)
		{
			Vec3 desiredVelocity = this.Position - target;
			return desiredVelocity - this.Velocity;
		}

		// xxx proposed, experimental new seek/flee [cwr 9-16-02]
		public Vec3 SteerForFlee2(Vec3 target)
		{
			//  const Vec3 offset = position - target;
			Vec3 offset = this.Position - target;
			Vec3 desiredVelocity = offset.TruncateLength(this.MaxSpeed); //xxxnew
			return desiredVelocity - this.Velocity;
		}

		public Vec3 SteerForSeek2(Vec3 target)
		{
			//  const Vec3 offset = target - position;
			Vec3 offset = target - this.Position;
			Vec3 desiredVelocity = offset.TruncateLength(this.MaxSpeed); //xxxnew
			return desiredVelocity - this.Velocity;
		}

		// Path Following behaviors
		public Vec3 SteerToFollowPath(int direction, float predictionTime, Pathway path)
		{
			// our goal will be offset from our path distance by this amount
			float pathDistanceOffset = direction * predictionTime * this.Speed;

			// predict our future position
			Vec3 futurePosition = this.PredictFuturePosition(predictionTime);

			// measure distance along path of our current and predicted positions
			float nowPathDistance = path.MapPointToPathDistance(this.Position);
			float futurePathDistance = path.MapPointToPathDistance(futurePosition);

			// are we facing in the correction direction?
			bool rightway = ((pathDistanceOffset > 0) ?
								   (nowPathDistance < futurePathDistance) :
								   (nowPathDistance > futurePathDistance));

			// find the point on the path nearest the predicted future position
			// XXX need to improve calling sequence, maybe change to return a
			// XXX special path-defined object which includes two Vec3s and a 
			// XXX bool (onPath,tangent (ignored), withinPath)
			Vec3 tangent;
			float outside;
			Vec3 onPath = path.MapPointToPath(futurePosition, out tangent, out outside);

			// no steering is required if (a) our future position is inside
			// the path tube and (b) we are facing in the correct direction
			if ((outside < 0) && rightway)
			{
				// all is well, return zero steering
				return Vec3.Zero;
			}
			else
			{
				// otherwise we need to steer towards a target point obtained
				// by adding pathDistanceOffset to our current path position

				float targetPathDistance = nowPathDistance + pathDistanceOffset;
				Vec3 target = path.MapPathDistanceToPoint(targetPathDistance);

				AnnotatePathFollowing(futurePosition, onPath, target, outside);

				// return steering to seek target on path
				return SteerForSeek(target);
			}
		}

		public Vec3 SteerToStayOnPath(float predictionTime, Pathway path)
		{
			// predict our future position
			Vec3 futurePosition = this.PredictFuturePosition(predictionTime);

			// find the point on the path nearest the predicted future position
			Vec3 tangent;
			float outside;
			Vec3 onPath = path.MapPointToPath(futurePosition, out tangent, out outside);

			if (outside < 0)
			{
				// our predicted future position was in the path,
				// return zero steering.
				return Vec3.Zero;
			}
			else
			{
				// our predicted future position was outside the path, need to
				// steer towards it.  Use onPath projection of futurePosition
				// as seek target
				AnnotatePathFollowing(futurePosition, onPath, onPath, outside);
				return SteerForSeek(onPath);
			}
		}

		// ------------------------------------------------------------------------
		// Obstacle Avoidance behavior
		//
		// Returns a steering force to avoid a given obstacle.  The purely
		// lateral steering force will turn our this towards a silhouette edge
		// of the obstacle.  Avoidance is required when (1) the obstacle
		// intersects the this's current path, (2) it is in front of the
		// this, and (3) is within minTimeToCollision seconds of travel at the
		// this's current velocity.  Returns a zero vector value (Vec3::zero)
		// when no avoidance is required.
		public Vec3 SteerToAvoidObstacle(float minTimeToCollision, IObstacle obstacle)
		{
			Vec3 avoidance = obstacle.SteerToAvoid(this, minTimeToCollision);

			// XXX more annotation modularity problems (assumes spherical obstacle)
			if (avoidance != Vec3.Zero)
				AnnotateAvoidObstacle(minTimeToCollision * this.Speed);

			return avoidance;
		}

		// avoids all obstacles in an ObstacleGroup
		public Vec3 SteerToAvoidObstacles<Obstacle>(float minTimeToCollision, List<Obstacle> obstacles)
			where Obstacle : IObstacle
		{
			Vec3 avoidance = Vec3.Zero;
			PathIntersection nearest = new PathIntersection();
			float minDistanceToCollision = minTimeToCollision * this.Speed;

			// test all obstacles for intersection with my forward axis,
			// select the one whose point of intersection is nearest
			foreach (Obstacle o in obstacles)
			{
				//FIXME: this should be a generic call on Obstacle, rather than this code which presumes the obstacle is spherical
				PathIntersection next = FindNextIntersectionWithSphere(o as SphericalObstacle);

				if (nearest.intersect == false || (next.intersect != false && next.distance < nearest.distance))
					nearest = next;
			}

			// when a nearest intersection was found
			if ((nearest.intersect != false) && (nearest.distance < minDistanceToCollision))
			{
				// show the corridor that was checked for collisions
				AnnotateAvoidObstacle(minDistanceToCollision);

				// compute avoidance steering force: take offset from obstacle to me,
				// take the component of that which is lateral (perpendicular to my
				// forward direction), set length to maxForce, add a bit of forward
				// component (in capture the flag, we never want to slow down)
				Vec3 offset = this.Position - nearest.obstacle.Center;
				avoidance = offset.PerpendicularComponent(this.Forward);
				avoidance = avoidance.Normalize();
				avoidance *= this.MaxForce;
				avoidance += this.Forward * this.MaxForce * 0.75f;
			}

			return avoidance;
		}

		// ------------------------------------------------------------------------
		// Unaligned collision avoidance behavior: avoid colliding with other
		// nearby vehicles moving in unconstrained directions.  Determine which
		// (if any) other other this we would collide with first, then steers
		// to avoid the site of that potential collision.  Returns a steering
		// force vector, which is zero length if there is no impending collision.
		public Vec3 SteerToAvoidNeighbors<TVehicle>(float minTimeToCollision, List<TVehicle> others)
			where TVehicle : IVehicle
		{
			// first priority is to prevent immediate interpenetration
			Vec3 separation = SteerToAvoidCloseNeighbors(0, others);
			if (separation != Vec3.Zero) return separation;

			// otherwise, go on to consider potential future collisions
			float steer = 0;
			IVehicle threat = null;

			// Time (in seconds) until the most immediate collision threat found
			// so far.  Initial value is a threshold: don't look more than this
			// many frames into the future.
			float minTime = minTimeToCollision;

			// xxx solely for annotation
			Vec3 xxxThreatPositionAtNearestApproach = Vec3.Zero;
			Vec3 xxxOurPositionAtNearestApproach = Vec3.Zero;

			// for each of the other vehicles, determine which (if any)
			// pose the most immediate threat of collision.
			foreach (IVehicle other in others)
			{
				if (other != this/*this*/)
				{
					// avoid when future positions are this close (or less)
					float collisionDangerThreshold = this.Radius * 2;

					// predicted time until nearest approach of "this" and "other"
					float time = PredictNearestApproachTime(other);

					// If the time is in the future, sooner than any other
					// threatened collision...
					if ((time >= 0) && (time < minTime))
					{
						// if the two will be close enough to collide,
						// make a note of it
						if (ComputeNearestApproachPositions(other, time) < collisionDangerThreshold)
						{
							minTime = time;
							threat = other;
							xxxThreatPositionAtNearestApproach = hisPositionAtNearestApproach;
							xxxOurPositionAtNearestApproach = ourPositionAtNearestApproach;
						}
					}
				}
			}

			// if a potential collision was found, compute steering to avoid
			if (threat != null)
			{
				// parallel: +1, perpendicular: 0, anti-parallel: -1
				float parallelness = this.Forward.Dot(threat.Forward);
				float angle = 0.707f;

				if (parallelness < -angle)
				{
					// anti-parallel "head on" paths:
					// steer away from future threat position
					Vec3 offset = xxxThreatPositionAtNearestApproach - this.Position;
					float sideDot = offset.Dot(this.Side);
					steer = (sideDot > 0) ? -1.0f : 1.0f;
				}
				else
				{
					if (parallelness > angle)
					{
						// parallel paths: steer away from threat
						Vec3 offset = threat.Position - this.Position;
						float sideDot = offset.Dot(this.Side);
						steer = (sideDot > 0) ? -1.0f : 1.0f;
					}
					else
					{
						// perpendicular paths: steer behind threat
						// (only the slower of the two does this)
						if (threat.Speed <= this.Speed)
						{
							float sideDot = this.Side.Dot(threat.Velocity);
							steer = (sideDot > 0) ? -1.0f : 1.0f;
						}
					}
				}

				AnnotateAvoidNeighbor(threat, steer, xxxOurPositionAtNearestApproach, xxxThreatPositionAtNearestApproach);
			}

			return this.Side * steer;
		}

		// Given two vehicles, based on their current positions and velocities,
		// determine the time until nearest approach
		public float PredictNearestApproachTime(IVehicle other)
		{
			// imagine we are at the origin with no velocity,
			// compute the relative velocity of the other this
			Vec3 myVelocity = this.Velocity;
			Vec3 otherVelocity = other.Velocity;
			Vec3 relVelocity = otherVelocity - myVelocity;
			float relSpeed = relVelocity.Length();

			// for parallel paths, the vehicles will always be at the same distance,
			// so return 0 (aka "now") since "there is no time like the present"
			if (relSpeed == 0) return 0;

			// Now consider the path of the other this in this relative
			// space, a line defined by the relative position and velocity.
			// The distance from the origin (our this) to that line is
			// the nearest approach.

			// Take the unit tangent along the other this's path
			Vec3 relTangent = relVelocity / relSpeed;

			// find distance from its path to origin (compute offset from
			// other to us, find length of projection onto path)
			Vec3 relPosition = this.Position - other.Position;
			float projection = relTangent.Dot(relPosition);

			return projection / relSpeed;
		}

		// Given the time until nearest approach (predictNearestApproachTime)
		// determine position of each this at that time, and the distance
		// between them
		public float ComputeNearestApproachPositions(IVehicle other, float time)
		{
			Vec3 myTravel = this.Forward * this.Speed * time;
			Vec3 otherTravel = other.Forward * other.Speed * time;

			Vec3 myFinal = this.Position + myTravel;
			Vec3 otherFinal = other.Position + otherTravel;

			// xxx for annotation
			ourPositionAtNearestApproach = myFinal;
			hisPositionAtNearestApproach = otherFinal;

			return Vec3.Distance(myFinal, otherFinal);
		}

		/// XXX globals only for the sake of graphical annotation
		Vec3 hisPositionAtNearestApproach;
		Vec3 ourPositionAtNearestApproach;

		// ------------------------------------------------------------------------
		// avoidance of "close neighbors" -- used only by steerToAvoidNeighbors
		//
		// XXX  Does a hard steer away from any other agent who comes withing a
		// XXX  critical distance.  Ideally this should be replaced with a call
		// XXX  to steerForSeparation.
		public Vec3 SteerToAvoidCloseNeighbors<TVehicle>(float minSeparationDistance, List<TVehicle> others)
			where TVehicle : IVehicle
		{
			// for each of the other vehicles...
			foreach (IVehicle other in others)
			{
				if (other != this/*this*/)
				{
					float sumOfRadii = this.Radius + other.Radius;
					float minCenterToCenter = minSeparationDistance + sumOfRadii;
					Vec3 offset = other.Position - this.Position;
					float currentDistance = offset.Length();

					if (currentDistance < minCenterToCenter)
					{
						AnnotateAvoidCloseNeighbor(other, minSeparationDistance);
						return (-offset).PerpendicularComponent(this.Forward);
					}
				}
			}

			// otherwise return zero
			return Vec3.Zero;
		}

		// ------------------------------------------------------------------------
		// used by boid behaviors
		public bool IsInBoidNeighborhood(IVehicle other, float minDistance, float maxDistance, float cosMaxAngle)
		{
			if (other == this)
			{
				return false;
			}
			else
			{
				Vec3 offset = other.Position - this.Position;
				float distanceSquared = offset.LengthSquared();

				// definitely in neighborhood if inside minDistance sphere
				if (distanceSquared < (minDistance * minDistance))
				{
					return true;
				}
				else
				{
					// definitely not in neighborhood if outside maxDistance sphere
					if (distanceSquared > (maxDistance * maxDistance))
					{
						return false;
					}
					else
					{
						// otherwise, test angular offset from forward axis
						Vec3 unitOffset = offset / (float)Math.Sqrt(distanceSquared);
						float forwardness = this.Forward.Dot(unitOffset);
						return forwardness > cosMaxAngle;
					}
				}
			}
		}

		// ------------------------------------------------------------------------
		// Separation behavior -- determines the direction away from nearby boids
		public Vec3 SteerForSeparation(float maxDistance, float cosMaxAngle, List<IVehicle> flock)
		{
			// steering accumulator and count of neighbors, both initially zero
			Vec3 steering = Vec3.Zero;
			int neighbors = 0;

			// for each of the other vehicles...
			for (int i = 0; i < flock.Count; i++)
			{
				IVehicle other = flock[i];
				if (IsInBoidNeighborhood(other, this.Radius * 3, maxDistance, cosMaxAngle))
				{
					// add in steering contribution
					// (opposite of the offset direction, divided once by distance
					// to normalize, divided another time to get 1/d falloff)
					Vec3 offset = other.Position - this.Position;
					float distanceSquared = offset.Dot(offset);
					steering += (offset / -distanceSquared);

					// count neighbors
					neighbors++;
				}
			}

			// divide by neighbors, then normalize to pure direction
			if (neighbors > 0) steering = (steering / (float)neighbors).Normalize();

			return steering;
		}

		// ------------------------------------------------------------------------
		// Alignment behavior
		public Vec3 SteerForAlignment(float maxDistance, float cosMaxAngle, List<IVehicle> flock)
		{
			// steering accumulator and count of neighbors, both initially zero
			Vec3 steering = Vec3.Zero;
			int neighbors = 0;

			// for each of the other vehicles...
			for (int i = 0; i < flock.Count; i++)
			{
				IVehicle other = flock[i];
				if (IsInBoidNeighborhood(other, this.Radius * 3, maxDistance, cosMaxAngle))
				{
					// accumulate sum of neighbor's heading
					steering += other.Forward;

					// count neighbors
					neighbors++;
				}
			}

			// divide by neighbors, subtract off current heading to get error-
			// correcting direction, then normalize to pure direction
			if (neighbors > 0) steering = ((steering / (float)neighbors) - this.Forward).Normalize();

			return steering;
		}

		// ------------------------------------------------------------------------
		// Cohesion behavior
		public Vec3 SteerForCohesion(float maxDistance, float cosMaxAngle, List<IVehicle> flock)
		{
			// steering accumulator and count of neighbors, both initially zero
			Vec3 steering = Vec3.Zero;
			int neighbors = 0;

			// for each of the other vehicles...
			for (int i = 0; i < flock.Count; i++)
			{
				IVehicle other = flock[i];
				if (IsInBoidNeighborhood(other, this.Radius * 3, maxDistance, cosMaxAngle))
				{
					// accumulate sum of neighbor's positions
					steering += other.Position;

					// count neighbors
					neighbors++;
				}
			}

			// divide by neighbors, subtract off current position to get error-
			// correcting direction, then normalize to pure direction
			if (neighbors > 0) steering = ((steering / (float)neighbors) - this.Position).Normalize();

			return steering;
		}

		// ------------------------------------------------------------------------
		// pursuit of another this (& version with ceiling on prediction time)
		public Vec3 SteerForPursuit(IVehicle quarry)
		{
			return SteerForPursuit(quarry, float.MaxValue);
		}

		public Vec3 SteerForPursuit(IVehicle quarry, float maxPredictionTime)
		{
			// offset from this to quarry, that distance, unit vector toward quarry
			Vec3 offset = quarry.Position - this.Position;
			float distance = offset.Length();
			Vec3 unitOffset = offset / distance;

			// how parallel are the paths of "this" and the quarry
			// (1 means parallel, 0 is pependicular, -1 is anti-parallel)
			float parallelness = this.Forward.Dot(quarry.Forward);

			// how "forward" is the direction to the quarry
			// (1 means dead ahead, 0 is directly to the side, -1 is straight back)
			float forwardness = this.Forward.Dot(unitOffset);

			float directTravelTime = distance / this.Speed;
			int f = Utilities.IntervalComparison(forwardness, -0.707f, 0.707f);
			int p = Utilities.IntervalComparison(parallelness, -0.707f, 0.707f);

			float timeFactor = 0;   // to be filled in below
			Color color = Color.Black; // to be filled in below (xxx just for debugging)

			// Break the pursuit into nine cases, the cross product of the
			// quarry being [ahead, aside, or behind] us and heading
			// [parallel, perpendicular, or anti-parallel] to us.
			switch (f)
			{
			case +1:
				switch (p)
				{
				case +1:          // ahead, parallel
					timeFactor = 4;
					color = Color.Black;
					break;
				case 0:           // ahead, perpendicular
					timeFactor = 1.8f;
					color = Color.Gray;
					break;
				case -1:          // ahead, anti-parallel
					timeFactor = 0.85f;
					color = Color.White;
					break;
				}
				break;
			case 0:
				switch (p)
				{
				case +1:          // aside, parallel
					timeFactor = 1;
					color = Color.Red;
					break;
				case 0:           // aside, perpendicular
					timeFactor = 0.8f;
					color = Color.Yellow;
					break;
				case -1:          // aside, anti-parallel
					timeFactor = 4;
					color = Color.Green;
					break;
				}
				break;
			case -1:
				switch (p)
				{
				case +1:          // behind, parallel
					timeFactor = 0.5f;
					color = Color.Cyan;
					break;
				case 0:           // behind, perpendicular
					timeFactor = 2;
					color = Color.Blue;
					break;
				case -1:          // behind, anti-parallel
					timeFactor = 2;
					color = Color.Magenta;
					break;
				}
				break;
			}

			// estimated time until intercept of quarry
			float et = directTravelTime * timeFactor;

			// xxx experiment, if kept, this limit should be an argument
			float etl = (et > maxPredictionTime) ? maxPredictionTime : et;

			// estimated position of quarry at intercept
			Vec3 target = quarry.PredictFuturePosition(etl);

			// annotation
			AnnotationLine(this.Position, target, GaudyPursuitAnnotation ? color : Color.DarkGray);

			return SteerForSeek(target);
		}

		// for annotation
		public bool GaudyPursuitAnnotation;

		// ------------------------------------------------------------------------
		// evasion of another this
		public Vec3 SteerForEvasion(IVehicle menace, float maxPredictionTime)
		{
			// offset from this to menace, that distance, unit vector toward menace
			Vec3 offset = menace.Position - this.Position;
			float distance = offset.Length();

			float roughTime = distance / menace.Speed;
			float predictionTime = ((roughTime > maxPredictionTime) ? maxPredictionTime : roughTime);

			Vec3 target = menace.PredictFuturePosition(predictionTime);

			return SteerForFlee(target);
		}

		// ------------------------------------------------------------------------
		// tries to maintain a given speed, returns a maxForce-clipped steering
		// force along the forward/backward axis
		public Vec3 SteerForTargetSpeed(float targetSpeed)
		{
			float mf = this.MaxForce;
			float speedError = targetSpeed - this.Speed;
			return this.Forward * Utilities.Clip(speedError, -mf, +mf);
		}

		// ----------------------------------------------------------- utilities
		// XXX these belong somewhere besides the steering library
		// XXX above AbstractVehicle, below SimpleVehicle
		// XXX ("utility this"?)

		// xxx cwr experimental 9-9-02 -- names OK?
		public bool IsAhead(Vec3 target)
		{
			return IsAhead(target, 0.707f);
		}
		public bool IsAside(Vec3 target)
		{
			return IsAside(target, 0.707f);
		}
		public bool IsBehind(Vec3 target)
		{
			return IsBehind(target, -0.707f);
		}

		public bool IsAhead(Vec3 target, float cosThreshold)
		{
			Vec3 targetDirection = (target - this.Position).Normalize();
			return this.Forward.Dot(targetDirection) > cosThreshold;
		}
		public bool IsAside(Vec3 target, float cosThreshold)
		{
			Vec3 targetDirection = (target - this.Position).Normalize();
			float dp = this.Forward.Dot(targetDirection);
			return (dp < cosThreshold) && (dp > -cosThreshold);
		}
		public bool IsBehind(Vec3 target, float cosThreshold)
		{
			Vec3 targetDirection = (target - this.Position).Normalize();
			return this.Forward.Dot(targetDirection) < cosThreshold;
		}

		// xxx cwr 9-6-02 temporary to support old code
		protected struct PathIntersection
		{
			public bool intersect;
			public float distance;
			public Vec3 surfacePoint;
			public Vec3 surfaceNormal;
			public SphericalObstacle obstacle;
		}

		// xxx experiment cwr 9-6-02
		protected PathIntersection FindNextIntersectionWithSphere(SphericalObstacle obs)
		{
			// xxx"SphericalObstacle& obs" should be "const SphericalObstacle&
			// obs" but then it won't let me store a pointer to in inside the
			// PathIntersection

			// This routine is based on the Paul Bourke's derivation in:
			//   Intersection of a Line and a Sphere (or circle)
			//   http://www.swin.edu.au/astronomy/pbourke/geometry/sphereline/

			float b, c, d, p, q, s;
			Vec3 lc;

			// initialize pathIntersection object
			PathIntersection intersection = new PathIntersection();
			intersection.intersect = false;
			intersection.obstacle = obs;

			// find "local center" (lc) of sphere in boid's coordinate space
			lc = this.LocalizePosition(obs.Center);

			// computer line-sphere intersection parameters
			b = -2 * lc.Z;
			c = Utilities.Square(lc.X) + Utilities.Square(lc.Y) + Utilities.Square(lc.Z) -
				Utilities.Square(obs.Radius + this.Radius);
			d = (b * b) - (4 * c);

			// when the path does not intersect the sphere
			if (d < 0)
				return intersection;

			// otherwise, the path intersects the sphere in two points with
			// parametric coordinates of "p" and "q".
			// (If "d" is zero the two points are coincident, the path is tangent)
			s = (float)Math.Sqrt(d);
			p = (-b + s) / 2;
			q = (-b - s) / 2;

			// both intersections are behind us, so no potential collisions
			if ((p < 0) && (q < 0))
				return intersection;

			// at least one intersection is in front of us
			intersection.intersect = true;
			intersection.distance =
				((p > 0) && (q > 0)) ?
				// both intersections are in front of us, find nearest one
				((p < q) ? p : q) :
				// otherwise only one intersections is in front, select it
				((p > 0) ? p : q);
			return intersection;
		}

		// ------------------------------------------------ graphical annotation

		// called when steerToAvoidObstacles decides steering is required
		// (default action is to do nothing, layered classes can overload it)
		public virtual void AnnotateAvoidObstacle(float minDistanceToCollision)
		{
		}

		// called when steerToFollowPath decides steering is required
		// (default action is to do nothing, layered classes can overload it)
		public virtual void AnnotatePathFollowing(Vec3 future, Vec3 onPath, Vec3 target, float outside)
		{
		}

		// called when steerToAvoidCloseNeighbors decides steering is required
		// (default action is to do nothing, layered classes can overload it)
		public virtual void AnnotateAvoidCloseNeighbor(IVehicle other, float additionalDistance)
		{
		}

		// called when steerToAvoidNeighbors decides steering is required
		// (default action is to do nothing, layered classes can overload it)
		public virtual void AnnotateAvoidNeighbor(IVehicle threat, float steer, Vec3 ourFuture, Vec3 threatFuture)
		{
		}
	}
}
