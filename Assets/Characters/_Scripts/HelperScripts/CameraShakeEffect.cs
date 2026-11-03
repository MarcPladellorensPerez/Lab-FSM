using System.Collections;
using UnityEngine;

public class CameraShakeEffect : MonoBehaviour
{
    private Vector3 m_originalPosition;
    private float m_shakeTime;
    private bool m_isShaking;

    [SerializeField]
    private float m_shakeDuration = 0.5f;    // Duration of the shake effect
    [SerializeField]
    private float m_shakeAmplitude = 0.5f;   // How strong the shake is
    [SerializeField]
    private float m_shakeFrequency = 1f;     // How fast the shake moves

    private void Awake()
    {
        m_originalPosition = transform.localPosition;
    }

    public void StartShake()
    {
        if (!m_isShaking)
        {
            StartCoroutine(ShakeCoroutine());
        }
        else
        {
            // If already shaking, reset the time to create a new shake
            m_shakeTime = 0;
        }
    }

    private IEnumerator ShakeCoroutine()
    {
        m_isShaking = true;
        m_shakeTime = 0;

        while (m_shakeTime < m_shakeDuration)
        {
            m_shakeTime += Time.deltaTime;
            float progress = m_shakeTime / m_shakeDuration;

            // Reduce shake intensity over time
            float currentAmplitude = m_shakeAmplitude * (1f - progress);

            // Generate perlin noise for more natural camera movement
            float offsetX = (Mathf.PerlinNoise(Time.time * m_shakeFrequency, 0) * 2 - 1) * currentAmplitude;
            float offsetY = (Mathf.PerlinNoise(0, Time.time * m_shakeFrequency) * 2 - 1) * currentAmplitude;

            // Apply the shake offset
            transform.localPosition = m_originalPosition + new Vector3(offsetX, offsetY, 0);

            yield return null;
        }

        // Return to original position
        transform.localPosition = m_originalPosition;
        m_isShaking = false;
    }

}
