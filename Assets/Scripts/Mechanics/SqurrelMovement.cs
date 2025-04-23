using System.Collections;
using UnityEngine;

public class SquirrelMovement : MonoBehaviour
{
    public float minMoveDistance = 1f;
    public float maxMoveDistance = 5f;
    public float moveSpeed = 2f;
    public float minIdleTime = 1f;
    public float maxIdleTime = 3f;

    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private bool isMoving = false;

    private void Start()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        StartCoroutine(SquirrelRoutine());
    }

    private IEnumerator SquirrelRoutine()
    {
        while (true)
        {
            // IDLE
            isMoving = false;
            animator.Play("Squrrel Idle");
            float idleTime = Random.Range(minIdleTime, maxIdleTime);
            yield return new WaitForSeconds(idleTime);

            // Decide direction and distance
            float distance = Random.Range(minMoveDistance, maxMoveDistance);
            int direction = Random.Range(0, 2) == 0 ? -1 : 1; // -1 = left, 1 = right

            // Flip sprite
            spriteRenderer.flipX = direction < 0;

            // WALK
            isMoving = true;
            animator.Play("Squrrel Run");

            float moved = 0f;
            while (moved < distance)
            {
                float step = moveSpeed * Time.deltaTime;
                transform.Translate(Vector2.right * step * direction);
                moved += step;
                yield return null;
            }

            isMoving = false;
            animator.Play("Squrrel Idle");
        }
    }
}
