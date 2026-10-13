using System;
using UnityEngine;

namespace CanoneerRestoration;

public class ShamrockCanoneerLogic : MonoBehaviour
{
	// Fields
	public GameObject player;
	public GameObject explosion;
	public GameObject trigger;
	public GameObject spawnController;
	public Transform cannonMouth;
	public AudioSource audioSource;
	public AudioClip fireCannon;
	public float fireCooldown;
	public float fireDelay = 2f;
	public float detectDistance = 80f;
	public int health = 6;
	public bool detected; // Here for parity with ShamrockSpike
	public bool trackPlayer = true; // Here for parity with ShamrockSpike
	public bool preparingShot = true;
	public bool spawning;
	public float verticalVelocity = 5f;
	public float detectionRange;
	public float tooCloseRange = 15f;
	public float tooFarRange = 25f;

	private Vector3 LockedPosition;
	private enum State { Idle, TooClose, TooFar, PreparingShot, PostShot }
	private State currentState = State.Idle;

	public ShamrockCanoneerLogic()
	{

	}

	public void Start()
	{
		GameObject cannonMouthObj = new GameObject("CannonMouth");
		cannonMouthObj.transform.parent = transform;
		cannonMouthObj.transform.position = transform.position + new Vector3(0f, 2f, -0.5f);
		cannonMouthObj.transform.rotation = Quaternion.Euler(-60f, 180f, 0f);
		cannonMouth = cannonMouthObj.transform;
		player = GameObject.Find("Player");
		audioSource = GetComponent<AudioSource>();

		// Load audio file
		string audioPath = System.IO.Path.Combine(CanoneerRestoration.AudioPath, "fireCannon.wav");
		if (System.IO.File.Exists(audioPath))
		{
			StartCoroutine(LoadAudio(audioPath));
		}
	}

	private System.Collections.IEnumerator LoadAudio(string path)
	{
		WWW www = new WWW("file://" + path);
		yield return www;
		if (www.error == null)
		{
			fireCannon = www.audioClip;
		}
	}

	public void OnDestroy()
	{
		if (cannonMouth != null && cannonMouth.gameObject != null)
		{
			Destroy(cannonMouth.gameObject);
		}
	}

	public void Update()
	{
		if (player == null)
		{
			return;
		}

		float distance = Vector3.Distance(transform.position, player.transform.position);
		if (distance > detectDistance)
		{
			detected = false;
			return;
		}
		detected = true;

		if (distance < tooCloseRange || distance > tooFarRange)
		{
			preparingShot = false;
			if (rigidbody != null)
			{
				rigidbody.isKinematic = false;
			}
		}
		else
		{
			PrepareShot();
		}

		Vector3 targetPos = new Vector3(player.transform.position.x, transform.position.y, player.transform.position.z);
		Quaternion targetRotation = Quaternion.LookRotation(targetPos - transform.position);

		fireCooldown -= Time.deltaTime;

		if (health <= 0)
		{
			Kill();
		}

		if (trackPlayer && detected && preparingShot)
		{
			transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
			transform.position = new Vector3(LockedPosition.x, transform.position.y, LockedPosition.z);

			if (Quaternion.Angle(transform.rotation, targetRotation) < 1f)
			{
				if (fireCooldown <= 0f)
				{
					FireShot();
				}
			}
		}
	}

	private void PrepareShot()
	{
		LockedPosition = transform.position;
		preparingShot = true;
		if (rigidbody != null)
		{
			rigidbody.isKinematic = true;
		}
	}

	public Vector3 BallisticVel(Transform target, float gravity)
	{
		if (cannonMouth == null)
		{
			return Vector3.zero;
		}

		Vector3 position = target.position;
		position.y = transform.position.y;

		float distance = Vector3.Distance(cannonMouth.position, position);
		float heightDiff = target.position.y - cannonMouth.position.y;
		float angle = 0.7853982f; // ~45 degrees in radians

		float velocity = Mathf.Sqrt(0.5f * gravity * distance * (Mathf.Tan(angle) * Mathf.Tan(angle) + 1f) / (Mathf.Tan(angle) - heightDiff / distance));

		return velocity * cannonMouth.forward;
	}

	public void FireShot()
	{
		if (player == null || cannonMouth == null)
		{
			return;
		}

		Vector3 velocity = BallisticVel(player.transform, 9.81f);
		CannonballFactory.SpawnCannonball(cannonMouth.position, cannonMouth.rotation, velocity);

		if (audioSource != null && fireCannon != null)
		{
			audioSource.PlayOneShot(fireCannon, 15f);
		}

		// Create smoke prefab
		GameObject smoke = CannonballFactory.CreateSmokePrefab();
		smoke.transform.position = cannonMouth.position;
		smoke.transform.rotation = cannonMouth.rotation;
		// Don't parent to cannonMouth so it can auto-destroy independently
		// smoke.transform.parent = cannonMouth;

		fireCooldown = 3f;
		preparingShot = false;
		if (rigidbody != null)
		{
			rigidbody.isKinematic = false;
		}
	}

	public void OnCollisionEnter(Collision col)
	{
		if (col.gameObject.tag == "PlayerBullet" && col.gameObject.name != "Shot2(Clone)")
		{
			if (audioSource != null)
			{
				audioSource.Play();
			}
			health--;
			if (spawnController != null)
			{
				spawnController.SendMessage("LostHealth");
			}
		}
	}

	public void Kill()
	{
		if (spawning && spawnController != null)
		{
			spawnController.SendMessage("Increment");
		}
		if (explosion != null)
		{
			Instantiate(explosion, transform.position, transform.rotation);
		}
		if (trigger != null)
		{
			Destroy(trigger);
		}
		Destroy(gameObject);
	}

	public void OnTriggerEnter(Collider col)
	{
		if (col.gameObject.tag == "Respawn")
		{
			if (animation != null)
			{
				animation.Play("FlyAway");
			}
		}
	}

	public void Main()
	{
		// TODO: Implement main logic if needed
	}
}
