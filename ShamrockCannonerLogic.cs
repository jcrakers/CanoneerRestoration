using System;
using UnityEngine;

namespace CanoneerRestoration;

public class ShamrockCanoneerLogic : MonoBehaviour
{
	// Fields
	public GameObject player;
	public GameObject explosion;
	public GameObject trigger;
	public GameObject spawnControllerObject;
	public Transform cannonMouth;
	public AudioSource audioSource;
	public AudioClip fireCannon;
	public AudioClip moveSound;
	public int health = 6;
	public bool detected = false; // Here for parity with ShamrockSpike
	public bool trackPlayer = true; // Here for parity with ShamrockSpike
	public bool preparingShot = true;
	public bool spawning;
	public float detectionRange = 80f;
	
	
	
	private readonly float extraGravity = 15f;

	private readonly float tooCloseRange = 15f;
	private readonly float tooFarRange = 25f;

	private readonly float knockbackForce = 5f;

	private float moveTimer = 0f;
	private readonly float moveCooldown = 0.3f;
	private readonly float movementForce = 16f;
	private readonly float movementJumpForce = 17f;
	private readonly float avoidWallAngle = 35f;
	private readonly float wallCheckDistance = 8f;
	private readonly float wallCheckAngle = 45f;

	private float stunnedTimer = 0f;
	private readonly float stunnedDuration = 2f;

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
		cannonMouthObj.transform.localPosition = new Vector3(0f, 2f, -0.5f);
		cannonMouthObj.transform.localRotation = Quaternion.Euler(-40f, 0f, 0f);
		cannonMouth = cannonMouthObj.transform;
		player = GameObject.Find("Player");
		audioSource = GetComponent<AudioSource>();

		// Load audio file
		string audioPath = System.IO.Path.Combine(CanoneerRestoration.AudioPath, "fireCannon.wav");
		if (System.IO.File.Exists(audioPath))
		{
			StartCoroutine(LoadAudio(audioPath, "fireCannon"));
		}
		
		// Load move sound
		string moveSoundPath = System.IO.Path.Combine(CanoneerRestoration.AudioPath, "cow.wav");
		if (System.IO.File.Exists(moveSoundPath))
		{
			StartCoroutine(LoadAudio(moveSoundPath, "cow"));
		}
	}

	private System.Collections.IEnumerator LoadAudio(string path, string audioName)
	{
		WWW www = new WWW("file://" + path);
		yield return www;
		if (www.error == null)
		{
			if (audioName == "fireCannon")
			{
				fireCannon = www.audioClip;
			}
			else if (audioName == "cow")
			{
				moveSound = www.audioClip;
			}
		}
	}

	public void OnDestroy()
	{
		if (cannonMouth != null && cannonMouth.gameObject != null)
		{
			Destroy(cannonMouth.gameObject);
		}
	}

	private bool IsWithinDetectionRange()
	{
		if (player == null)
		{
			return false;
		}
		
		float distance = Vector3.Distance(transform.position, player.transform.position);
		return distance <= detectionRange;
	}

	public void Update()
	{
		// Check health and kill if needed
		if (health <= 0)
		{
			Kill();
			return;
		}

		if (!IsGrounded())
		{
			rigidbody.AddForce(Vector3.down * extraGravity, ForceMode.Acceleration);
		}

		if (!detected)
		{
			detected = HasLineOfSight() && IsWithinDetectionRange();
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
		float angle = 0.6981317f; // ~40 degrees in radians

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
			currentState = State.Stunned;
			stunnedTimer = 0f;
		}

		// Apply movement force
		rigidbody?.AddForce(-transform.forward * 8, ForceMode.Impulse);
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

			if (currentState == State.Stunned)
			{
				currentState = State.Idle;
				stunnedTimer = 0f;
			}
			else if (currentState == State.PreparingShot)
			{
				currentState = State.Stunned;
				stunnedTimer = 0f;
				preparationTimer = 0f;
			}
			
			if (spawnControllerObject != null)
			{
				ShamrockSpawnController spawnController = spawnControllerObject.GetComponent<ShamrockSpawnController>();
				spawnController.LostHealth();
			}

			// Apply knockback force
			if (rigidbody != null)
			{
				rigidbody.isKinematic = false;
				Vector3 knockbackDirection = (transform.position - col.transform.position).normalized;
				rigidbody.AddForce(knockbackDirection * knockbackForce, ForceMode.Impulse);
			}
		}
	}

	public void Kill()
	{
		if (spawning && spawnControllerObject != null)
		{
			ShamrockSpawnController spawnController = spawnControllerObject.GetComponent<ShamrockSpawnController>();
			spawnController.Increment();
		}
		GameObject explosion = CannonballFactory.CreateShrapnelPrefab();
		explosion.transform.position = transform.position;

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
		if (currentState == State.Stunned)
		{
			stunnedTimer += Time.deltaTime;
			if (stunnedTimer >= stunnedDuration)
			{
				stunnedTimer = 0f;
				currentState = State.Idle;
			}
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
		if (distance > detectionRange)
		{
			currentState = State.Idle;
			return;
		}

		// Only transition out of idle if we have line of sight
		if (currentState == State.Idle && !detected)
		{
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
			if (IsGrounded())
			{
				currentState = State.PreparingShot;
				preparationTimer = 0f;
			}
		}
	}

	private bool HasLineOfSight()
	{
		if (player == null) return false;

		Vector3 direction = (player.transform.position - transform.position).normalized;
		float distance = Vector3.Distance(transform.position, player.transform.position);

		// Offset starting position by 1 unit along direction to step OUTSIDE this enemy's collider
		Vector3 rayStart = transform.position + (direction * 1.0f);

		RaycastHit hit;
		if (Physics.Raycast(rayStart, direction, out hit, distance - 1.0f))
		{
			// If it hit something other than the player, LOS is blocked
			if (hit.transform != player.transform)
			{
				return false;
			}
		}

		return true;
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
	}

	private void HandleTooCloseState()
	{
		if (rigidbody != null)
		{
			rigidbody.isKinematic = false;
		}
		HandleMovement(false);
	}

	private void HandleTooFarState()
	{
		if (rigidbody != null)
		{
			rigidbody.isKinematic = false;
		}
		HandleMovement(true);
	}

	private void HandlePreparingShotState()
	{
		if (!IsGrounded())
		{
			return;
		}

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

			if (Quaternion.Angle(transform.rotation, targetRotation) < 10f)
			{
				if (preparationTimer >= preparationDuration)
				{
					FireShot();
				}
			}
		}
	}

	private bool IsGrounded()
	{
		return Physics.Raycast(transform.position, Vector3.down, 1.1f);
	}

	private void HandleMovement(bool forward)
	{
		if (moveTimer >= moveCooldown)
		{
			moveTimer = 0f;
		}
		else
		{
			moveTimer += Time.deltaTime;
			return;
		}
		
		if (!IsGrounded())
		{
			return;
		}

		// 1. Check if there is a wall behind us closer than the player
		bool wallBehind = false;
		if (player != null)
		{
			float distToPlayer = Vector3.Distance(transform.position, player.transform.position);
			if (Physics.Raycast(transform.position, -transform.forward, out RaycastHit backHit, wallCheckDistance))
			{
				if (backHit.distance < distToPlayer)
				{
					wallBehind = true;
				}
			}
		}

		// Force forward movement if backed against a wall
		if (wallBehind)
		{
			forward = true;
		}

		if (player != null)
		{
			Vector3 targetPos = new Vector3(player.transform.position.x, transform.position.y, player.transform.position.z);
			Quaternion targetRotation = Quaternion.LookRotation(targetPos - transform.position);
			transform.rotation = targetRotation;
		}

		// 2. Determine base direction vector
		Vector3 baseDir = forward ? transform.forward : -transform.forward;

		// 3. Side wall checks & 35-degree deflection
		Vector3 leftDir = Quaternion.Euler(0f, wallCheckAngle, 0f) * transform.right;
		Vector3 rightDir = Quaternion.Euler(0f, -wallCheckAngle, 0f) * transform.right;

		bool hitLeft = Physics.Raycast(transform.position, leftDir, out RaycastHit leftHit, wallCheckDistance);
		bool hitRight = Physics.Raycast(transform.position, rightDir, out RaycastHit rightHit, wallCheckDistance);

		if (hitLeft || hitRight && !(hitLeft && hitRight))
		{
			if (hitLeft)
			{
				// Wall on the Left: Deflect 35 degrees to the Right
				baseDir = Quaternion.Euler(0f, avoidWallAngle, 0f) * baseDir;
			}
			else
			{
				// Wall on the Right: Deflect 35 degrees to the Left
				baseDir = Quaternion.Euler(0f, -avoidWallAngle, 0f) * baseDir;
			}
		}
		else if (hitLeft && hitRight)
		{
			// Compare which wall is closer
			if (leftHit.distance < rightHit.distance)
			{
				baseDir = Quaternion.Euler(0f, avoidWallAngle, 0f) * baseDir;
			}
			else
			{
				baseDir = Quaternion.Euler(0f, -avoidWallAngle, 0f) * baseDir;
			}
		}

		// 4. Combine horizontal direction with vertical arc force
		Vector3 jumpForce = (baseDir.normalized * movementForce) + (Vector3.up * movementJumpForce);

		if (rigidbody != null)
		{
			rigidbody.isKinematic = false;
			rigidbody.velocity = jumpForce;

			// Set angular velocity for rolling around the pitch axis
			float rollDirection = forward ? 1f : -1f;
			rigidbody.angularVelocity = transform.right * (rollDirection * 15f);
		}

		audioSource.PlayOneShot(moveSound, 5f);
	}

	public void Main()
	{
		// TODO: Implement main logic if needed
	}
}
