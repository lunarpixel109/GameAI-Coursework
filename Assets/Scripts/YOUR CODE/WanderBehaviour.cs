using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class WanderBehaviour : SteeringBehaviour
{
	[SerializeField]
	private float targetSearchRadius = 40.0f;

	[SerializeField]
	private float waypointArrivalDistance = 0.35f;

	private readonly System.Random random = new System.Random();
	private List<Vector3> path;
	private int waypointIndex;
	private Vector3 currentTarget;
	private float nextPursuitPathUpdate;

	protected override void Start()
	{
		base.Start();
		ChooseNewTarget();
	}

	public override Vector3 UpdateBehaviour(SteeringAgent steeringAgent)
	{
		if (GameData.Instance == null || GameData.Instance.Map == null)
		{
			return Vector3.zero;
		}

		SteeringAgent pursuitTarget = AllyAgent.SharedEnemyTarget;
		if (pursuitTarget != null)
		{
			if (pursuitTarget.Health <= 0.0f || Time.time >= GetSharedTargetExpiry())
			{
				pursuitTarget = null;
			}
		}

		if (pursuitTarget != null)
		{
			float distanceToEnemy = (pursuitTarget.transform.position - transform.position).sqrMagnitude;
			float stopDistance = Attack.AllyGunData.range * 0.75f;

			if (distanceToEnemy <= stopDistance * stopDistance)
			{
				desiredVelocity = Vector3.zero;
				steeringVelocity = desiredVelocity - steeringAgent.CurrentVelocity;
				return steeringVelocity;
			}

			if (Time.time >= nextPursuitPathUpdate || path == null)
			{
				UpdatePursuitPath(pursuitTarget.transform.position);
				nextPursuitPathUpdate = Time.time + 0.25f;
			}
		}
		else
		{
			nextPursuitPathUpdate = 0.0f;
		}

		if (path == null || waypointIndex >= path.Count)
		{
			ChooseNewTarget();
		}

		if (path != null)
		{
			while (waypointIndex < path.Count &&
				Vector3.SqrMagnitude(transform.position - path[waypointIndex]) <= waypointArrivalDistance * waypointArrivalDistance)
			{
				++waypointIndex;
			}

			if (waypointIndex >= path.Count)
			{
				ChooseNewTarget();
			}
		}

		if (path == null || waypointIndex >= path.Count)
		{
			desiredVelocity = Vector3.zero;
			steeringVelocity = desiredVelocity - steeringAgent.CurrentVelocity;
			return steeringVelocity;
		}

		Vector3 targetPosition = path[waypointIndex];
		desiredVelocity = Vector3.Normalize(targetPosition - transform.position) * SteeringAgent.MaxCurrentSpeed;
		steeringVelocity = desiredVelocity - steeringAgent.CurrentVelocity;
		return steeringVelocity;
	}

	private void ChooseNewTarget()
	{
		path = null;
		waypointIndex = 0;

		Map map = GameData.Instance.Map;
		int startX = Mathf.Clamp(Mathf.RoundToInt(transform.position.x), 0, Map.MapWidth - 1);
		int startY = Mathf.Clamp(Mathf.RoundToInt(transform.position.y), 0, Map.MapHeight - 1);

		for (int attempt = 0; attempt < 100; ++attempt)
		{
			int targetX = random.Next(0, Map.MapWidth);
			int targetY = random.Next(0, Map.MapHeight);
			if (!map.IsNavigatable(targetX, targetY))
			{
				continue;
			}

			List<Vector3> candidatePath = AStarPathfinder.FindPath(map, startX, startY, targetX, targetY);
			if (candidatePath == null || candidatePath.Count <= 1)
			{
				continue;
			}

			path = candidatePath;
			waypointIndex = 1;
			currentTarget = new Vector3(targetX, targetY, 0.0f);
			return;
		}

		currentTarget = transform.position;
	}

	private void UpdatePursuitPath(Vector3 pursuitPosition)
	{
		Map map = GameData.Instance.Map;
		int startX = Mathf.Clamp(Mathf.RoundToInt(transform.position.x), 0, Map.MapWidth - 1);
		int startY = Mathf.Clamp(Mathf.RoundToInt(transform.position.y), 0, Map.MapHeight - 1);
		int targetX = Mathf.Clamp(Mathf.RoundToInt(pursuitPosition.x), 0, Map.MapWidth - 1);
		int targetY = Mathf.Clamp(Mathf.RoundToInt(pursuitPosition.y), 0, Map.MapHeight - 1);

		path = AStarPathfinder.FindPath(map, startX, startY, targetX, targetY);
		waypointIndex = path != null && path.Count > 1 ? 1 : 0;
		currentTarget = pursuitPosition;
	}

	private static float GetSharedTargetExpiry()
	{
		return AllyAgent.SharedTargetExpiryTime;
	}

	public override void DebugDraw(SteeringAgent steeringAgent)
	{
		base.DebugDraw(steeringAgent);

		if (path == null)
		{
			return;
		}

		for (int i = 0; i < path.Count - 1; ++i)
		{
			DebugDrawLine("DebugWanderPath " + i + " " + GetType().Name, path[i], path[i + 1], Color.green);
		}

		if (currentTarget != transform.position)
		{
			DebugDrawCircle("DebugWanderTarget " + GetType().Name, currentTarget, 0.25f, Color.green);
		}
	}
}
