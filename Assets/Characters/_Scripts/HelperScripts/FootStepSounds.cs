using UnityEngine;

public class FootStepSounds : MonoBehaviour
{
    [SerializeField]
    private AudioSource m_audioSource;
    [SerializeField]
    private AudioClip[] m_footStepSounds;
    [SerializeField, Range(0, 1)]
    private float m_volume = 0.4f;

    public void FootStep()
    {
        if (m_audioSource != null && m_footStepSounds.Length > 0)
        {
            m_audioSource.volume = m_volume;
            m_audioSource.PlayOneShot(m_footStepSounds[Random.Range(0, m_footStepSounds.Length)]);
        }
    }
}
