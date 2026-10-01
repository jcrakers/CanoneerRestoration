using System;
using System.Linq;
using UnityEngine;

public static class CannonballFactory
{
    private static GameObject _shrapnelPrefab;

    public static GameObject SpawnCannonball(Vector3 position, Quaternion rotation, Vector3 initialVelocity)
    {
        // 1. Create primitive sphere container
        GameObject cannonball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        cannonball.name = "Cannonball";
        cannonball.transform.position = position;
        cannonball.transform.rotation = rotation;

        // 2. Configure Collider
        SphereCollider collider = cannonball.GetComponent<SphereCollider>();
        if (collider != null)
        {
            collider.isTrigger = false;
            collider.radius = 0.5f;
            collider.center = Vector3.zero;
        }

        // 3. Configure Rigidbody
        Rigidbody rb = cannonball.AddComponent<Rigidbody>();
        rb.mass = 1f;
        rb.drag = 0f;
        rb.angularDrag = 0f;
        rb.useGravity = false; // Internal CannonballLogic handles gravity calculation
        rb.isKinematic = false;
        rb.interpolation = RigidbodyInterpolation.None;
        rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
        rb.velocity = initialVelocity;

        // 4. Configure Specular "ShinyBlack" Material
        Shader specularShader = Shader.Find("Specular") 
            ?? Shader.Find("Legacy Shaders/Specular") 
            ?? Shader.Find("Standard");

        Material shinyBlack = new Material(specularShader);
        shinyBlack.color = new Color(0.05f, 0.05f, 0.05f, 1f);
        if (shinyBlack.HasProperty("_SpecColor"))
        {
            shinyBlack.SetColor("_SpecColor", new Color(0.5f, 0.5f, 0.5f, 1f));
        }
        cannonball.GetComponent<Renderer>().material = shinyBlack;

        // 5. Attach CannonballLogic and populate public fields
        CannonballLogic logic = cannonball.AddComponent<CannonballLogic>();
        logic.lifeTime = 10f;
        logic.useGravity = true;
        logic.gravity = 9.81f;
        
        // Link the synthesized CannonShrapnel prefab
        logic.shrapnel = GetOrCreateShrapnelPrefab();

        return cannonball;
    }

    private static GameObject GetOrCreateShrapnelPrefab()
    {
        if (_shrapnelPrefab != null) return _shrapnelPrefab;

        // Create a hidden template object to act as the prefab
        _shrapnelPrefab = new GameObject("CannonShrapnel_Template");
        _shrapnelPrefab.SetActive(false); // Kept inactive so Instantiate() makes clean runtime copies
        UnityEngine.Object.DontDestroyOnLoad(_shrapnelPrefab);

        // Add 3D AudioSource setup
        AudioSource audio = _shrapnelPrefab.AddComponent<AudioSource>();
        audio.playOnAwake = true;
        audio.panLevel = 1f;
        audio.minDistance = 1f;
        audio.maxDistance = 50f;
        audio.rolloffMode = AudioRolloffMode.Logarithmic;
        audio.volume = 1f;
        audio.pitch = 1f;
        audio.priority = 128;

        // Find loaded 'explosion' AudioClip from memory
        AudioClip explosionClip = Resources.FindObjectsOfTypeAll<AudioClip>()
            .FirstOrDefault(c => c.name.Equals("explosion", StringComparison.OrdinalIgnoreCase));

        if (explosionClip != null)
        {
            audio.clip = explosionClip;
        }

        // Add DestroyAfterXSeconds script to handle lifetime cleanup
        DestroyAfterXSeconds cleanup = _shrapnelPrefab.AddComponent<DestroyAfterXSeconds>();
        cleanup.lifetime = 3f; // Destroys the spawned shrapnel gameobject after 3 seconds

        return _shrapnelPrefab;
    }
}