// HappyHarvest/BasicAnimalMovement.cs
using Template2DCommon;
using UnityEngine;
using Random = UnityEngine.Random;


namespace HappyHarvest
{
    /// <summary>
    /// Simply make a GameObject "roam" inside a given collider 2D bound
    /// </summary>
    public class BasicAnimalMovement : MonoBehaviour
    {
        public Collider2D Area;

        [Min(0)] public float MinIdleTime;
        [Min(0)] public float MaxIdleTime;

        [Min(0)] public float Speed = 2.0f;

        [Header("Audio")] public AudioClip[] AnimalSound;
        public float MinRandomSoundTime;
        public float MaxRandomSoundTime;

        private float m_IdleTimer;
        private float m_CurrentIdleTarget;

        private float m_SoundTimer;

        private Vector3 m_CurrentTarget;

        private bool m_IsIdle;

        private Animator m_Animator;
        private int SpeedHash = Animator.StringToHash("Speed");

        private void Start()
        {
            if (MaxIdleTime <= MinIdleTime)
                MaxIdleTime = MinIdleTime + 0.1f;

            m_Animator = GetComponentInChildren<Animator>();

            m_SoundTimer = Random.Range(MinRandomSoundTime, MaxRandomSoundTime);

            m_IsIdle = true;
            PickNewIdleTime();
        }

        private void Update()
        {
            m_SoundTimer -= Time.deltaTime;
            if (m_SoundTimer <= 0.0f)
            {
                // ADD THIS NULL CHECK:
                if (SoundManager.Instance != null && AnimalSound != null && AnimalSound.Length > 0)
                {
                    SoundManager.Instance.PlaySFXAt(transform.position, AnimalSound[Random.Range(0, AnimalSound.Length)], true);
                }
                else
                {
                    // Optional: Log a warning if SoundManager isn't ready or sounds are missing
                    // Debug.LogWarning("SoundManager.Instance is null or AnimalSound array is empty/null. Cannot play animal sound.");
                }
                m_SoundTimer = Random.Range(MinRandomSoundTime, MaxRandomSoundTime);
            }

            if (m_IsIdle)
            {
                m_IdleTimer += Time.deltaTime;

                if (m_IdleTimer >= m_CurrentIdleTarget)
                {
                    PickNewTarget();
                }
            }
            else
            {
                transform.position = Vector3.MoveTowards(transform.position, m_CurrentTarget, Speed * Time.deltaTime);
                if (transform.position == m_CurrentTarget)
                {
                    PickNewIdleTime();
                }
            }
        }

        void PickNewIdleTime()
        {
            if (m_Animator != null)
                m_Animator.SetFloat(SpeedHash, 0.0f);

            m_IsIdle = true;
            m_CurrentIdleTarget = Random.Range(MinIdleTime, MaxIdleTime);
            m_IdleTimer = 0.0f;
        }

        void PickNewTarget()
        {
            m_IsIdle = false;
            var dir = Quaternion.Euler(0, 0, 360.0f * Random.Range(0.0f, 1.0f)) * Vector2.up;

            dir *= Random.Range(1.0f, 10.0f);

            var pos = (Vector2)transform.position;
            var pts = pos + (Vector2)dir;

            // ADD THIS NULL CHECK:
            if (Area == null)
            {
                Debug.LogError("BasicAnimalMovement: 'Area' Collider2D is not assigned in the Inspector! Cannot pick new target.");
                // Fallback to idle if Area is null, or just return to prevent further errors
                PickNewIdleTime();
                return;
            }

            if (!Area.OverlapPoint(pts))
            {
                pts = Area.ClosestPoint(pts);
            }

            m_CurrentTarget = pts;
            var toTarget = m_CurrentTarget - transform.position;

            bool flipped = toTarget.x < 0;
            transform.localScale = new Vector3(flipped ? -1 : 1, 1, 1);

            if (m_Animator != null)
                m_Animator.SetFloat(SpeedHash, Speed);
        }
    }
}