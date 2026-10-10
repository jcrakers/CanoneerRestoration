using UnityEngine;

namespace CanoneerRestoration;

public class ShamrockCanoneerLogic : MonoBehaviour
{
	// Fields
	public GameObject[] projectiles;
	public GameObject player;
	public GameObject explosion;
	public GameObject crosshairs;
	public GameObject trigger;
	public Transform cannonMouth;
	public float fireCooldown;
	public int health;
	public bool dying;
	public bool canFire;
	public bool activated;
	public float verticalVelocity;

	public ShamrockCanoneerLogic()
	{
		health = 6;
		canFire = true;
		verticalVelocity = 5f;
	}

	public void Start()
	{
		player = GameObject.Find("Player");
	}

	public void Update()
	{
		Vector3 targetPos = new Vector3(player.transform.position.x, transform.position.y, player.transform.position.z);
		Quaternion targetRotation = Quaternion.LookRotation(targetPos - transform.position);

		if (health <= 0 && !dying)
		{
			dying = true;
			Kill();
		}

		if (activated)
		{
			transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);

			if (canFire)
			{
				canFire = false;
				FireShot();
			}
		}
	}

	public Vector3 BallisticVel(Transform target, float gravity)
	{
		Vector3 position = target.position;
		position.y = transform.position.y;

		float distance = Vector3.Distance(cannonMouth.transform.position, position);
		float heightDiff = cannonMouth.transform.position.y - target.position.y;
		float angle = 1.0471976f; // ~60 degrees in radians

		float velocity = Mathf.Sqrt(gravity) * Mathf.Sqrt(distance) * Mathf.Sqrt(Mathf.Tan(angle) * Mathf.Tan(angle) + 1f) / Mathf.Sqrt(2f * Mathf.Tan(angle) - 2f * gravity * heightDiff / distance);

		return velocity * cannonMouth.transform.forward;
	}

	public void FireShot()
	{
		// TODO: Implement actual firing logic
		// For now, just reset the fire cooldown
		canFire = true;
	}

	public void Hurt()
	{
		health--;
	}

	public void Kill()
	{
		Instantiate(explosion, transform.position, transform.rotation);
		Destroy(trigger);
		Destroy(gameObject);
	}

	public void Main()
	{
		// TODO: Implement main logic if needed
	}
}
