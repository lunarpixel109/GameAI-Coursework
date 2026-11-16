using System.Collections.Generic;
using UnityEngine;

public abstract class SteeringAgent : MonoBehaviour
{
	/// <summary>Size of collision radius of agent</summary>
	public const float CollisionRadius = 0.45f;

	/// <summary>The maximum speed the agent can have</summary>
	public const float MaxCurrentSpeed = 6.0f;

	/// <summary>The maximum steering speed that will be applied to the overall steering velocity affter cooperative arbitration</summary>
	public const float MaxSteeringSpeed = 20.0f;

	/// <summary>Multiplier of speed of agent in water</summary>
	public const float WaterSpeedFactor = 0.25f;

	/// <summary>Multiplier of speed of agent in mud</summary>
	public const float MudSpeedFactor = 0.5f;

	/// <summary>Default time to before next attack can be done</summary>
	public const float DefaultTimeToNextAttack = 0.75f;

	/// <summary>Default time to execute cooperative arbitration. Less than oe equal to 0 means run every frame</summary>
	public const float DefaultUpdateTimeInSecondsForAI = 0.0f;



	/// <summary>
	/// Returns the nearest agent from the list specified. Note this can return null if no agent dound or they are all dead.
	/// </summary>
	/// <param name="position"></param>
	/// <param name="agents"></param>
	/// <returns></returns>
	public static SteeringAgent GetNearestAgent(Vector3 position, List<SteeringAgent> agents)
	{
		SteeringAgent nearestAgent = null;
		float nearestSquareDistance = float.MaxValue;
		foreach (var agent in agents)
		{
			if (agent.Health <= 0f)
			{
				continue;
			}

			var squareDistance = (agent.transform.position - position).sqrMagnitude;
			if (squareDistance < nearestSquareDistance)
			{
				nearestSquareDistance = squareDistance;
				nearestAgent = agent;
			}
		}
		return nearestAgent;
	}

	/// <summary>
	/// Limits the vector to the specified magnitude
	/// </summary>
	/// <param name="vector">Vector to limit</param>
	/// <param name="magnitude">Amount to limit to</param>
	/// <returns>New Vector that has been limited</returns>
	public static Vector3 LimitVector(Vector3 vector, float magnitude)
	{
		// This limits the velocity to max speed. sqrMagnitude is used rather than magnitude as in magnitude a square root must be computed which is a slow operation.
		// By using sqrMagnitude and comparing with maxSpeed squared we can get around using the expensive square root operation.
		if (vector.sqrMagnitude > magnitude * magnitude)
		{
			vector.Normalize();
			vector *= magnitude;
		}
		return vector;
	}

	/// <summary>
	/// Returns the maximum speed allowed for the agent based on the terrain it is currently on.
	/// </summary>
	/// <param name="agent"></param>
	/// <returns>Speed in world units per second</returns>
	public static float GetMaxSpeedAllowed(SteeringAgent agent)
	{
		return GetMaxSpeedAllowed((int)agent.transform.position.x, (int)agent.transform.position.y);
	}

	/// <summary>
	/// Returns the maximum speed allowed for the agent based on the terrain it is currently on.
	/// </summary>
	/// <param name="mapX"></param>
	/// <param name="mapY"></param>
	/// <returns>Speed in world units per second</returns>
	public static float GetMaxSpeedAllowed(int mapX, int mapY)
	{
		var terrain = GameData.Instance.Map.GetTerrainAt(mapX, mapY);
		var maxSpeed = MaxCurrentSpeed;
		switch (terrain)
		{
			case Map.Terrain.Water:
				maxSpeed *= WaterSpeedFactor;
				break;

			case Map.Terrain.Mud:
				maxSpeed *= MudSpeedFactor;
				break;
		}
		return maxSpeed;
	}


	/// <summary>Returns the current velocity (speed and direction) of the Agent</summary>
	public Vector3 CurrentVelocity { get; protected set; } = Vector3.zero;

	/// <summary>Returns the steering velocity of the Agent</summary>
	public Vector3 SteeringVelocity { get; protected set; } = Vector3.zero;

	/// <summary>Stores the transform.position fron the previous game frame</summary>
	public Vector3 PreviousPosition { get; private set; } = Vector3.zero;

	/// <summary>Stores the health pf the agent</summary>
	public float TimeToNextAttack { get; private set; } = 0.0f;

	/// <summary>
	/// Adjusts the frequency time in seconds of when the AI will update its logic
	/// </summary>
	[field: SerializeField, Range(0.0f, 5.0f)]
	public float MaxUpdateTimeInSecondsForAI { get; protected set; } = 0.0f;

	/// <summary>Convenience getter for the health of the agent</summary>
	public float Health => GameData.Instance.GetAgentHealth(this);

	protected abstract void InitialiseFromAwake();

	protected virtual void InitialiseFromStart()
	{
	}

	protected virtual void Died()
	{
		Debug.Log(gameObject.name + " is dead");
	}

	public bool CanAttack(Attack.AttackType attack)
	{
		if(TimeToNextAttack > 0.0f)
		{
			return false;
		}

		if(this is EnemyAgent)
		{
			if ((attack == Attack.AttackType.EnemyRocket && GameData.Instance.EnemyRocketsAvailable <= 0) ||
				(attack == Attack.AttackType.Grenade && GameData.Instance.EnemyGrenadesAvailable <= 0))
			{
				return false;
			}
		}
		else
		{
			// Prevent Ally from attacking with Enemy attacks or Ally Rocket if no rockets available
			if (attack == Attack.AttackType.EnemyGun ||
				attack == Attack.AttackType.EnemyRocket ||
				(attack == Attack.AttackType.AllyRocket && GameData.Instance.AllyRocketsAvailable <= 0) ||
				(attack == Attack.AttackType.Grenade && GameData.Instance.AllyGrenadesAvailable <= 0))
			{
				return false;
			}


		}

		bool isEnemy = this is EnemyAgent;
		if(isEnemy && attack == Attack.AttackType.EnemyRocket && GameData.Instance.EnemyRocketsAvailable <= 0)
		{
			return false;
		}

		if(!isEnemy && attack == Attack.AttackType.AllyRocket && GameData.Instance.AllyRocketsAvailable <= 0)
		{
			return false;
		}
	
		return true;
	}

	public bool AttackWith(Attack.AttackType attack)
	{
		IsAttackInProgress = true;
		if (GameData.Instance.CreateAttack(attack, this))
		{
			TimeToNextAttack = DefaultTimeToNextAttack;
			IsAttackInProgress = false;
			return true;
		}
		IsAttackInProgress = false;

		return false;
	}

	public bool ShowDebug
	{
		get
		{
			GetComponents<SteeringBehaviour>(steeringBehvaiours);
			foreach (SteeringBehaviour currentBehaviour in steeringBehvaiours)
			{
				if (currentBehaviour.ShowDebugLines)
				{
					return true;
				}
			}

			return false;
		}

		set
		{
			GetComponents<SteeringBehaviour>(steeringBehvaiours);
			foreach (SteeringBehaviour currentBehaviour in steeringBehvaiours)
			{
				currentBehaviour.ShowDebugLines = value;
			}
		}
	}


	#region Private interface

	/// <summary>No need to check or use this for internal use only</summary>
	public bool IsAttackInProgress { get; private set; } = false;

	/// <summary>Stores the transform.position fron the end of update</summary>
	private Vector3 currentPosition;

	/// <summary>Stores a list of all steering behaviours that are on a SteeringAgent GameObject, regardless if they are enabled or not</summary>
	private List<SteeringBehaviour> steeringBehvaiours = new List<SteeringBehaviour>();

	/// <summary>
	/// Tracks how many seconds have elapsed since the last CooperativeArbitration function has run
	/// </summary>
	public float UpdateTimeInSecondsForAI { get; protected set; }

	private Map map;

	private float previousHealth;

	private void Awake()
	{
		var gameData = GameData.Instance;
		map = gameData.Map;

		InitialiseFromAwake();
	}

	private void Start()
	{
		currentPosition = transform.position;
		previousHealth = Health;
		transform.position = currentPosition;
		PreviousPosition = transform.position;

		InitialiseFromStart();
	}

	/// <summary>
	/// Called once per frame
	/// </summary>
	private void Update()
	{
		if(Health <= 0.0f)
		{
			if(previousHealth != Health)
			{
				previousHealth = Health;
				Died();
			}
			gameObject.SetActive(false);
			return;
		}
		previousHealth = Health;

		transform.position = currentPosition;
		PreviousPosition = transform.position;

		TimeToNextAttack -= Time.deltaTime;
		if(TimeToNextAttack < 0.0f)
		{
			TimeToNextAttack = 0.0f;
		}

		UpdateTimeInSecondsForAI += Time.deltaTime;
		if (MaxUpdateTimeInSecondsForAI <= 0)
		{
			// If MaxUpdateTimeInSecondsForAI <= 0 then run the CooperativeArbitration logic every frame
			CooperativeArbitration(UpdateTimeInSecondsForAI);
			UpdateTimeInSecondsForAI = 0;
		}
		else
		{
			// If MaxUpdateTimeInSecondsForAI > 0 then run the CooperativeArbitration at the interval specified. This means that
			// the AI will not update its logic every frame but rather at a slower rate which can be useful for performance reasons
			while (UpdateTimeInSecondsForAI >= MaxUpdateTimeInSecondsForAI)
			{
				CooperativeArbitration(MaxUpdateTimeInSecondsForAI);
				UpdateTimeInSecondsForAI -= MaxUpdateTimeInSecondsForAI;
			}
		}

		UpdatePosition();
		UpdateDirection();

		// Show debug lines in scene view
		foreach (SteeringBehaviour currentBehaviour in steeringBehvaiours)
		{
			currentBehaviour.DebugDraw(this);
		}
	}

	/// <summary>
	/// This is responsible for how to deal with multiple behaviours and selecting which ones to use. Please see this link for some decent descriptions of below:
	/// https://alastaira.wordpress.com/2013/03/13/methods-for-combining-autonomous-steering-behaviours/
	/// Remember some options for choosing are:
	/// 1 Finite state machines which can be part of the steering behaviours or not (Not the best approach but quick)
	/// 2 Weighted Truncated Sum
	/// 3 Prioritised Weighted Truncated Sum
	/// 4 Prioritised Dithering
	/// 5 Context Behaviours: https://andrewfray.wordpress.com/2013/03/26/context-behaviours-know-how-to-share/
	/// 6 Any other approach you come up with
	/// </summary>
	///  <param name="deltaTime">The time in seconds since the last call to this function</param>
	protected virtual void CooperativeArbitration(float deltaTime)
	{
		SteeringVelocity = Vector3.zero;

		GetComponents<SteeringBehaviour>(steeringBehvaiours);
		foreach (SteeringBehaviour currentBehaviour in steeringBehvaiours)
		{
			if (currentBehaviour.enabled)
			{
				SteeringVelocity += currentBehaviour.UpdateBehaviour(this);
			}
		}
	}

	/// <summary>
	/// Updates the position of the GAmeObject via Teleportation. In Craig Reynolds architecture this would the Locomotion layer
	/// </summary>
	private void UpdatePosition()
	{
		// Limit steering velocity to supplied maximum so it can be used to adjust current velocity. Ensure to subtract this limnited
		// amount from the current value of the steering velocity so that it decreases as over multiple game frames to reach the target
		SteeringVelocity = LimitVector(SteeringVelocity, MaxSteeringSpeed * Time.deltaTime);

		// Set final velocity
		CurrentVelocity += SteeringVelocity;
		CurrentVelocity = LimitVector(CurrentVelocity, GetMaxSpeedAllowed(this));

		// Apply current velocity amount for this frame
		transform.position += CurrentVelocity * Time.deltaTime;
	}

	/// <summary>
	/// Using late update to correct any issues with agent positions being in incorrect places or moving too much in one frame.
	/// DO NOT change this and DO NOT override or hide the function in derived classes!
	/// </summary>
	private void LateUpdate()
	{
		var restrictPosition = transform.position;
		restrictPosition.x = Mathf.Clamp(transform.position.x, 0.0f, (float)(Map.MapWidth) - 0.01f);
		restrictPosition.y = Mathf.Clamp(transform.position.y, 0.0f, (float)(Map.MapHeight) - 0.01f);
		transform.position = restrictPosition;

		// Check for collision with obstacles and prevent movement, but allow sliding collision if possible. Note: Sliding collision only
		// works in this context as obstacles are aligned to othogonals. Proper way to do this would be finding out normals of collision
		// and working out tangent to move along, but this provides a good enough solution without the extra complexity
		if (map.IsNavigatable((int)transform.position.x, (int)transform.position.y) == false)
		{
			var distanceToApply = CurrentVelocity * Time.deltaTime;
			transform.position = PreviousPosition;
			var newX = transform.position.x + distanceToApply.x;
			if (newX < 0.0f || newX >= Map.MapWidth)
			{
				distanceToApply.x = 0.0f;
			}

			transform.position += new Vector3(distanceToApply.x, 0.0f, 0.0f);
			if (map.IsNavigatable((int)transform.position.x, (int)transform.position.y) == false)
			{
				transform.position = PreviousPosition;
				var newY = transform.position.y + distanceToApply.y;
				if (newY < 0.0f || newY >= Map.MapHeight)
				{
					distanceToApply.y = 0.0f;
				}

				transform.position += new Vector3(0.0f, distanceToApply.y, 0.0f);
				if (map.IsNavigatable((int)transform.position.x, (int)transform.position.y) == false)
				{
					transform.position = PreviousPosition;
				}
			}
		}

		// Prevent cheating by teleportation and log it as an error
		var maxSpeed = GetMaxSpeedAllowed((int)PreviousPosition.x, (int)PreviousPosition.y);
		var distanceTravelled = (transform.position - PreviousPosition).magnitude;
		if (distanceTravelled - 0.001f > maxSpeed * Time.deltaTime)
		{
			transform.position = PreviousPosition;
			Debug.LogWarning("Steering agent position has exceeded max steering speed! Distance allowed this frame: " + maxSpeed * Time.deltaTime + ", Actual distance: " + distanceTravelled);
		}

		currentPosition = transform.position;
	}

	/// <summary>
	/// Sets the direction of the agent to the direction it is moving in to give the illusion it is turning. Agents do not need 
	/// to face in the same direction they move in. Only change the direction of the agent never set 
	/// </summary>
	protected virtual void UpdateDirection()
	{
		// Don't set the direction if no direction
		if (CurrentVelocity.sqrMagnitude > 0.0f)
		{
			transform.up = Vector3.Normalize(new Vector3(CurrentVelocity.x, CurrentVelocity.y, 0.0f));
		}
	}

	#endregion
}
