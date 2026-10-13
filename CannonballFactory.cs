using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

namespace CanoneerRestoration;

public static class CannonballFactory
{
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

        cannonball.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);

        return cannonball;
    }

    // For when the Cannonball explodes, create a shrapnel prefab
    public static GameObject CreateShrapnelPrefab()
    {
        // 1. Root Container
        GameObject explosion = new GameObject("ProceduralExplosion");

        // 2. Point Light Flash
        Light light = explosion.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1.0f, 0.45f, 0.1f); // Warm fire orange
        light.range = 8.0f;
        light.intensity = 5.0f;

        // 3. Particle System
        ParticleSystem ps = explosion.AddComponent<ParticleSystem>();
        ParticleSystemRenderer psr = explosion.GetComponent<ParticleSystemRenderer>();

        string texturePath = Path.Combine(CanoneerRestoration.TexturesPath, "fire.png");
        byte[] fileData = File.ReadAllBytes(texturePath);
        Texture2D texture = new Texture2D(2, 2);
        texture.LoadImage(fileData);

        // Use a transparent shader for smoke particles
        Shader transparentShader = Shader.Find("Particles/Alpha Blended") 
            ?? Shader.Find("Transparent/Diffuse")
            ?? Shader.Find("Mobile/Particles/Alpha Blended");
        psr.material.shader = transparentShader;
        psr.material.mainTexture = texture;

        // Unity 4.6.1 Legacy Particle Assignment
        ps.startLifetime = 0.4f;
        ps.startSpeed = 1.5f;
        ps.startSize = 0.8f;
        ps.startColor = new Color(1.0f, 0.5f, 0.5f, 0.8f);
        ps.gravityModifier = -1f;

        // Emission happens in ExplosionController.Start()
        // ps.Emit(60);

        // 4. Sphere Collider for hurt detection
        SphereCollider sphereCollider = explosion.AddComponent<SphereCollider>();
        sphereCollider.isTrigger = true;

        // 5. Audio Source for explosion sound
        AudioSource audioSource = explosion.AddComponent<AudioSource>();

        // 6. Explosion Logic Controller (Auto-destructs & fades light)
        ExplosionController controller = explosion.AddComponent<ExplosionController>();
        controller.duration = 0.6f;
        controller.lightIntensity = 3.0f;
        controller.explosionRadius = 4.0f;
        controller.explosionForce = 500.0f;
        controller.audioSource = audioSource;
        controller.sphereCollider = sphereCollider;

        // Hide master template in memory until cloned
        //explosion.SetActive(false);
        return explosion;
    }


    public class ExplosionController : MonoBehaviour
    {
        public float duration = 0.6f;
        public float lightIntensity = 3.0f;
        public float explosionRadius = 4.0f;
        public float explosionForce = 500.0f;
        public AudioSource audioSource;
        public SphereCollider sphereCollider;

        private Light flashLight;
        private float timer = 0f;
        private AudioClip explosionClip;

        void Start()
        {
            flashLight = GetComponent<Light>();

            if (sphereCollider != null)
            {
                sphereCollider.radius = explosionRadius;
                sphereCollider.isTrigger = true;
                sphereCollider.tag = "HurtPlayer";
            }

            // Emit particles when the explosion actually happens
            ParticleSystem ps = GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ps.transform.rotation = Quaternion.Euler(-90, 0, 0);
                ps.Emit(60);
            }

            // Load explosion audio
            string audioPath = Path.Combine(CanoneerRestoration.AudioPath, "bigexplosion.wav");
            if (File.Exists(audioPath))
            {
                StartCoroutine(LoadAudio(audioPath));
            }

            // Apply physical push to nearby rigidbodies on spawn
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, explosionRadius);
            foreach (Collider hit in hitColliders)
            {
                Rigidbody rb = hit.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.AddExplosionForce(explosionForce, transform.position, explosionRadius);
                }

                // Example: Apply damage if entity has a Hurt method
                // hit.gameObject.SendMessage("Hurt", SendMessageOptions.DontRequireReceiver);
            }
        }

        private IEnumerator LoadAudio(string path)
        {
            WWW www = new WWW("file://" + path);
            yield return www;
            if (www.error == null)
            {
                explosionClip = www.audioClip;
                if (audioSource != null && explosionClip != null)
                {
                    audioSource.PlayOneShot(explosionClip, 5f);
                }
            }
        }

        void Update()
        {
            timer += Time.deltaTime;

            // Fade out the light flash over time
            if (flashLight != null)
            {
                flashLight.intensity = Mathf.Lerp(lightIntensity, 0.0f, timer / duration);
            }

            // Clean up object once visual lifetime expires
            if (timer >= duration)
            {
                Destroy(gameObject);
            }
        }
    }

    // For when the Cannonball is fired, create a smoke prefab
    public static GameObject CreateSmokePrefab()
    {
        // 1. Root Container
        GameObject smoke = new GameObject("ProceduralSmoke");

        // 3. Particle System
        ParticleSystem ps = smoke.AddComponent<ParticleSystem>();
        ParticleSystemRenderer psr = smoke.GetComponent<ParticleSystemRenderer>();
        
        string texturePath = Path.Combine(CanoneerRestoration.TexturesPath, "smoke.png");
        byte[] fileData = File.ReadAllBytes(texturePath);
        Texture2D texture = new Texture2D(2, 2);
        texture.LoadImage(fileData);
        
        // Use a transparent shader for smoke particles
        Shader transparentShader = Shader.Find("Particles/Alpha Blended") 
            ?? Shader.Find("Transparent/Diffuse")
            ?? Shader.Find("Mobile/Particles/Alpha Blended");
        psr.material.shader = transparentShader;
        psr.material.mainTexture = texture;
        
        // Unity 4.6.1 Legacy Particle Assignment
        ps.startLifetime = 0.6f;
        ps.startSpeed = 1.0f;
        ps.startSize = 0.5f;
        ps.startColor = new Color(1.0f, 1.0f, 1.0f, 0.8f);
        ps.gravityModifier = -0.5f; // Negative makes smoke rise

        // Emission happens when prefab is instantiated
        // ps.Emit(20);

        // Add controller to emit particles on spawn
        SmokeController smokeController = smoke.AddComponent<SmokeController>();

        // Hide master template in memory until cloned
        //explosion.SetActive(false);
        return smoke;
    }

    public class SmokeController : MonoBehaviour
    {
        void Start()
        {
            ParticleSystem ps = GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ps.Emit(20);
            }

            // Auto-destroy after particles finish (longer since particles rise)
            Destroy(gameObject, 2.0f);
        }
    }
}
