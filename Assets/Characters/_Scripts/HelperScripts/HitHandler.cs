using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

public class HitHandler : MonoBehaviour
{
    [SerializeField]
    private Animator m_animator;
    [SerializeField]
    private float m_rotationDuration = 0.5f; // Duration of rotation in seconds
    [SerializeField]
    private AudioSource m_audioSource;
    [SerializeField]
    private AudioClip m_shieldClip;
    [SerializeField]
    private float m_shieldSoundDelay = 0.5f;
    [SerializeField]
    private CameraShakeEffect m_cameraShakeEffect;
    [FormerlySerializedAs("m_playerMover")]
    [SerializeField]
    private PlayerMovement m_playerMovement;
    [SerializeField, Range(0, 1)]
    private float m_volume = 0.8f;

    [SerializeField]
    private ParticleSystem m_impactParticles;

    public void GetHit(GameObject sender)
    {
        if (m_animator != null)
        {
            // Start rotation
            StopAllCoroutines();
            if (m_playerMovement != null)
            {
                m_playerMovement.StopMovement();
            }
            StartCoroutine(StartRotation(sender));
            StartCoroutine(PlayFeedback());
            StartCoroutine(PlayParticles());
            m_animator.SetTrigger("GetHit");
        }

    }

    private IEnumerator PlayParticles()
    {
        yield return new WaitForSeconds(m_shieldSoundDelay);
        if (m_impactParticles != null)
        {
            m_impactParticles.Play();
        }
    }

    private IEnumerator PlayFeedback()
    {
        yield return new WaitForSeconds(m_shieldSoundDelay);
        if (m_audioSource != null && m_shieldClip != null)
        {
            m_audioSource.volume = m_volume;
            m_audioSource.PlayOneShot(m_shieldClip);

        }
        if (m_cameraShakeEffect != null)
        {
            m_cameraShakeEffect.StartShake();
        }
    }

    private IEnumerator StartRotation(GameObject sender)
    {
        // Store the starting rotation
        Quaternion m_startRotation = transform.rotation;
        // Calculate direction to this enemy (the attacker)
        Vector3 directionToEnemy = (sender.transform.position - transform.position).normalized;
        directionToEnemy.y = 0; // Keep rotation only in XZ plane
                                // Calculate target rotation
        Quaternion m_targetRotation = Quaternion.LookRotation(directionToEnemy);
        float m_rotationTime = 0;
        //Perform rotation
        while (m_rotationTime < m_rotationDuration)
        {
            m_rotationTime += Time.deltaTime;
            float progress = m_rotationTime / m_rotationDuration;
            transform.rotation = Quaternion.Lerp(m_startRotation, m_targetRotation, progress);
            yield return null;
        }
        // Ensure we end at exact target rotation
        transform.rotation = m_targetRotation;
    }
}
