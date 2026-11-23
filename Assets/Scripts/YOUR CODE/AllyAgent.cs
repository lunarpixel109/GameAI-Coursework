using UnityEngine;

public class AllyAgent : SteeringAgent
{
	private Attack.AttackType attackType = Attack.AttackType.AllyGun;
	private SteeringAgent nearestEnemy;

	public static SteeringAgent SharedEnemyTarget { get; private set; }
	public static float SharedTargetExpiryTime => sharedTargetExpiresAt;
	private static float sharedTargetExpiresAt;

	private const float SharedTargetDuration = 3.0f;

	protected override void InitialiseFromAwake()
	{
		gameObject.AddComponent<WanderBehaviour>();
		gameObject.AddComponent<SeekToMouse>().enabled = false;
	}

	protected override void CooperativeArbitration(float deltaTime)
	{
		base.CooperativeArbitration(deltaTime);

		nearestEnemy = GetNearestAgent(transform.position, GameData.Instance.enemies);
		if (nearestEnemy != null)
		{
			float distanceToEnemy = (nearestEnemy.transform.position - transform.position).magnitude;
			if (distanceToEnemy <= Attack.AllyGunData.range)
			{
				SharedEnemyTarget = nearestEnemy;
				sharedTargetExpiresAt = Time.time + SharedTargetDuration;
				AttackWith(Attack.AttackType.AllyGun);
			}
		}

		if (Input.GetKeyDown(KeyCode.Alpha1))
		{
			attackType = Attack.AttackType.Melee;
		}
		if (Input.GetKeyDown(KeyCode.Alpha2))
		{
			attackType = Attack.AttackType.AllyGun;
		}
		if (Input.GetKeyDown(KeyCode.Alpha3))
		{
			attackType = Attack.AttackType.AllyRocket;
		}
		if (Input.GetKeyDown(KeyCode.Alpha4))
		{
			attackType = Attack.AttackType.Grenade;
		}
		if (Input.GetKey(KeyCode.Space))
		{
			if(attackType == Attack.AttackType.AllyRocket && GameData.Instance.AllyRocketsAvailable <= 0)
			{
				attackType = Attack.AttackType.AllyGun;
			}

			AttackWith(attackType);
		}
		if(Input.GetMouseButtonDown(1))
		{
			SteeringVelocity = Vector3.zero;
			CurrentVelocity = Vector3.zero;
			//var seekToMouse = GetComponent<SeekToMouse>();
			//seekToMouse.enabled = !seekToMouse.enabled;
		}


	}

	protected override void UpdateDirection()
	{
		base.UpdateDirection();

		if (nearestEnemy == null)
		{
			return;
		}

		Vector3 difference = nearestEnemy.transform.position - transform.position;
		if (difference.sqrMagnitude <= Attack.AllyGunData.range * Attack.AllyGunData.range)
		{
			transform.up = Vector3.Normalize(new Vector3(difference.x, difference.y, 0.0f));
		}
	}
}
