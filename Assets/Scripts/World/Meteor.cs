using UnityEngine;
using System;

namespace SkyRingTerrarium.World
{
    /// <summary>
    /// Meteor that falls during meteor shower events, potentially dropping special resources.
    /// </summary>
    public class Meteor : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float speed = 15f;
        [SerializeField] private float rotationSpeed = 180f;

        [Header("Visuals")]
        [SerializeField] private TrailRenderer trail;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private ParticleSystem impactParticles;
        [SerializeField] private Color meteorColor = new Color(1f, 0.6f, 0.2f);

        [Header("Impact")]
        [SerializeField] private float impactRadius = 2f;
        [SerializeField] private float lifetime = 5f;

        private Vector2 direction;
        private Action<Vector2> onLandedCallback;
        private float age;
        private bool hasImpacted;

        public void Initialize(Vector2 moveDirection, Action<Vector2> onLanded = null)
        {
            direction = moveDirection.normalized;
            onLandedCallback = onLanded;
            
            if (spriteRenderer != null)
            {
                spriteRenderer.color = meteorColor;
            }
            
            if (trail != null)
            {
                trail.startColor = meteorColor;
                trail.endColor = new Color(meteorColor.r, meteorColor.g, meteorColor.b, 0f);
            }
        }

        private void Update()
        {
            if (hasImpacted) return;

            age += Time.deltaTime;
            if (age >= lifetime)
            {
                Destroy(gameObject);
                return;
            }

            transform.position += (Vector3)(direction * speed * Time.deltaTime);
            transform.Rotate(0, 0, rotationSpeed * Time.deltaTime);

            CheckImpact();
        }

        private void CheckImpact()
        {
            // Check if meteor has reached the ring surface (radius ~20)
            float distFromCenter = ((Vector2)transform.position).magnitude;
            
            if (distFromCenter <= 22f)
            {
                Impact();
            }
        }

        private void Impact()
        {
            hasImpacted = true;
            
            if (impactParticles != null)
            {
                impactParticles.transform.SetParent(null);
                impactParticles.Play();
                Destroy(impactParticles.gameObject, 3f);
            }

            onLandedCallback?.Invoke(transform.position);
            
            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = false;
            }
            
            if (trail != null)
            {
                trail.emitting = false;
            }

            Destroy(gameObject, 0.5f);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, impactRadius);
        }
    }
}