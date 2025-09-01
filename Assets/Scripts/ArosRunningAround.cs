using UnityEngine;
using System.Collections;
using RamRoutes.Model;
using RamRoutes.Services;

public class ArosRunningAround : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float moveRange = 10f;  // How far to move horizontally from start position
    [SerializeField] private float appearInterval = 10f;
    
    [Header("Animation")]
    [SerializeField] private float fadeInDuration = 0.5f;
    [SerializeField] private float fadeOutDuration = 0.5f;

    [Header("Audio")]
    [SerializeField] private AudioClip moveSound;
    [Range(0f, 1f)]
    [SerializeField] private float moveSoundVolume = 0.5f;
    [SerializeField] private Stage stageToRenderIn;
    private AudioSource audioSource;
 private UIManager uiManager;

    private SpriteRenderer spriteRenderer;
    private bool isMoving = false;
    private Stage currentStage;
    private Vector3 startPosition;
    private bool movingRight = true;
    private UnityEngine.Rendering.Universal.Light2D light2D;
    private bool isActive = false;

    void Start()
    {
          if (uiManager == null)
        {
            uiManager = FindObjectOfType<UIManager>();
            if (uiManager == null)
            {
                uiManager = UIManager.Instance;
            }
        }
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }
        
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        light2D = GetComponentInChildren<UnityEngine.Rendering.Universal.Light2D>();
        if (light2D != null)
        {
            light2D.intensity = 0f;
        }
        
        // Hide initially
        SetAlpha(0);
        isActive = false;
        
        // Start appearance cycle only in Pedmall
        CheckStageAndInitialize();
    }

    private void CheckStageAndInitialize()
    {
        var stage = GameStageService.LoadStageFromPrefs();
        if (stage != null && (stage.area == stageToRenderIn))
        {
            currentStage = stage.area;
            StartCoroutine(AppearanceCycle());
        }
        else
        {
            // Disable if not in Pedmall
            enabled = false;
            gameObject.SetActive(false);
        }
    }

    private IEnumerator AppearanceCycle()
    {
        while (true)
        {
            // Wait for interval
            yield return new WaitForSeconds(appearInterval);
            
            // Start a new appearance
            yield return StartCoroutine(Appear());
            
            // Move through waypoints
            yield return StartCoroutine(MoveRoutine());
            
            // Disappear
            yield return StartCoroutine(Disappear());
        }
    }

    private IEnumerator Appear()
    {
        float elapsed = 0;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(0, 1, elapsed / fadeInDuration);
            SetAlpha(alpha);
            yield return null;
        }
        SetAlpha(1);
        isMoving = true;
    }

    private IEnumerator Disappear()
    {
        isMoving = false;
        float elapsed = 0;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1, 0, elapsed / fadeOutDuration);
            SetAlpha(alpha);
            yield return null;
        }
        SetAlpha(0);
        
        // Reset position, direction and sprite orientation
        transform.position = startPosition;
        movingRight = true;
        spriteRenderer.flipX = false;

        // Show dialog after disappearing
        if (uiManager != null)
        {
            uiManager.ShowDialog("Eros is near!", 10f, false);
        }
    }

    private IEnumerator MoveRoutine()
    {
        startPosition = transform.position;
        
        while (isMoving)
        {
            float targetX;
            if (movingRight)
            {
                targetX = startPosition.x + moveRange;
                spriteRenderer.flipX = false;
            }
            else
            {
                targetX = startPosition.x;
                spriteRenderer.flipX = true;
            }

            // Move until we reach the target X position
            while ((movingRight && transform.position.x < targetX) || 
                   (!movingRight && transform.position.x > targetX))
            {
                float direction = movingRight ? 1 : -1;
                Vector3 movement = new Vector3(moveSpeed * direction * Time.deltaTime, 0, 0);
                transform.position += movement;
                yield return null;
            }

            // Switch direction
            movingRight = !movingRight;
            
            // If we're back at start, complete the cycle
            if (!movingRight)
            {
                // Let it return to start position before stopping
            }
            else
            {
                isMoving = false;
            }
        }
    }

    private void SetAlpha(float alpha)
    {
        if (spriteRenderer != null)
        {
            Color color = spriteRenderer.color;
            color.a = alpha;
            spriteRenderer.color = color;
        }
        
        if (light2D != null)
        {
            light2D.intensity = alpha;
        }

        // Update active state based on visibility
        isActive = alpha > 0;
    }

    private void PlayMoveSound()
    {
        if (audioSource != null && moveSound != null)
        {
            audioSource.PlayOneShot(moveSound, moveSoundVolume);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && isActive && enabled)
        {
            PlayMoveSound();
        }
    }

    void OnValidate()
    {
        // Ensure range is positive
        if (moveRange <= 0)
        {
            Debug.LogWarning("Move range should be greater than 0");
            moveRange = 1f;
        }
    }
}
