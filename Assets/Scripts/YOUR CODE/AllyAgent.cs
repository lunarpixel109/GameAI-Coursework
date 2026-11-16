using UnityEngine;

public class AllyAgent : SteeringAgent
{
	private Attack.AttackType attackType = Attack.AttackType.AllyGun;

	protected override void InitialiseFromAwake()
	{
		gameObject.AddComponent<SeekToMouse>();
	}

	protected override void CooperativeArbitration(float deltaTime)
	{
		base.CooperativeArbitration(deltaTime);

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
			var seekToMouse = GetComponent<SeekToMouse>();
			seekToMouse.enabled = !seekToMouse.enabled;
		}
	}

	protected override void UpdateDirection()
	{
		if (GetComponent<SeekToMouse>().enabled)
		{
			base.UpdateDirection();
		}
		else
		{
			var mouseInWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
			mouseInWorld.z = 0.0f;
			transform.up = Vector3.Normalize(mouseInWorld - transform.position);
		}
	}
}
