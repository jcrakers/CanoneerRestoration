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
	private float stunnedTimer = 0f;
	private readonly float stunnedDuration = 1f;
	private float preparationTimer = 0f;
	private readonly float preparationDuration = 1f;
	private enum State { Idle, Stunned, TooClose, TooFar, PreparingShot }
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
		// Check health and kill if needed
		if (health <= 0)
		{
			Kill();
			return;
		}

		// Update state transitions
		UpdateStateTransitions();

		// Execute behavior based on current state
		switch (currentState)
		{
			case State.Idle:
				HandleIdleState();
				break;
			case State.Stunned:
				HandleStunnedState();
				break;
			case State.TooClose:
				HandleTooCloseState();
				break;
			case State.TooFar:
				HandleTooFarState();
				break;
			case State.PreparingShot:
				HandlePreparingShotState();
				break;
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

		preparationTimer = 0f;
		preparingShot = false;
		if (rigidbody != null)
		{
			rigidbody.isKinematic = false;
			currentState = State.Idle;
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
			currentState = State.Stunned;
			stunnedTimer = 0f;
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

	private void UpdateStateTransitions()
	{
		// Don't transition out of stunned state until timer expires
		if (currentState == State.Stunned && stunnedTimer < stunnedDuration)
		{
			return;
		}

		// Don't transition out of PreparingShot state - only exit when firing or getting hit
		if (currentState == State.PreparingShot)
		{
			return;
		}

		if (player == null)
		{
			currentState = State.Idle;
			return;
		}



		float distance = Vector3.Distance(transform.position, player.transform.position);

		// Check if player is out of detection range
		if (distance > detectDistance)
		{
			currentState = State.Idle;
			return;
		}

		// Check distance-based transitions
		if (distance < tooCloseRange)
		{
			currentState = State.TooClose;
		}
		else if (distance > tooFarRange)
		{
			currentState = State.TooFar;
		}
		else
		{
			currentState = State.PreparingShot;
			preparationTimer = 0f;
		}
	}

	private void HandleIdleState()
	{
		preparingShot = false;
		if (rigidbody != null)
		{
			rigidbody.isKinematic = false;
		}
	}

	private void HandleStunnedState()
	{
		// Stunned behavior - enemy is temporarily disabled
		preparingShot = false;
		if (rigidbody != null)
		{
			rigidbody.isKinematic = false;
		}

		// Track stunned duration and recover
		stunnedTimer += Time.deltaTime;
		if (stunnedTimer >= stunnedDuration)
		{
			stunnedTimer = 0f;
			currentState = State.Idle;
		}
	}

	private void HandleTooCloseState()
	{
		detected = true;
		if (rigidbody != null)
		{
			rigidbody.isKinematic = false;
		}
	}

	private void HandleTooFarState()
	{
		detected = true;
		if (rigidbody != null)
		{
			rigidbody.isKinematic = false;
		}
	}

	private void HandlePreparingShotState()
	{
		detected = true;

		LockedPosition = transform.position;
		preparingShot = true;
		if (rigidbody != null)
		{
			rigidbody.isKinematic = true;
		}

		Vector3 targetPos = new Vector3(player.transform.position.x, transform.position.y, player.transform.position.z);
		Quaternion targetRotation = Quaternion.LookRotation(targetPos - transform.position);

		preparationTimer += Time.deltaTime;

		if (trackPlayer)
		{
			transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
			transform.position = new Vector3(LockedPosition.x, transform.position.y, LockedPosition.z);

			if (Quaternion.Angle(transform.rotation, targetRotation) < 5f)
			{
				if (preparationTimer >= preparationDuration)
				{
					FireShot();
				}
			}
		}
	}

	public void Main()
	{
		// TODO: Implement main logic if needed
	}
}
