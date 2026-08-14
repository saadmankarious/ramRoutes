using System.Collections;
using UnityEngine;

// Coroutine-based scale "pop" for select/deselect feedback on world sprites.
public static class PopAnimator
{
    public static IEnumerator PopIn(Transform target, Vector3 baseScale, float overshoot = 1.35f, float settle = 1.15f, float duration = 0.35f)
    {
        Vector3[] keyframes =
        {
            baseScale,
            baseScale * overshoot,
            baseScale * settle,
            baseScale * overshoot,
        };

        yield return AnimateKeyframes(target, keyframes, duration);
    }

    public static IEnumerator PopOut(Transform target, Vector3 baseScale, float duration = 0.2f)
    {
        if (target == null) yield break;
        yield return AnimateScale(target, target.localScale, baseScale, duration);
    }

    private static IEnumerator AnimateKeyframes(Transform target, Vector3[] keyframes, float totalDuration)
    {
        if (target == null || keyframes.Length < 2) yield break;

        float segmentDuration = totalDuration / (keyframes.Length - 1);
        for (int i = 0; i < keyframes.Length - 1; i++)
        {
            yield return AnimateScale(target, keyframes[i], keyframes[i + 1], segmentDuration);
        }
    }

    private static IEnumerator AnimateScale(Transform target, Vector3 from, Vector3 to, float duration)
    {
        if (target == null) yield break;

        if (duration <= 0f)
        {
            target.localScale = to;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            target.localScale = Vector3.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
            yield return null;
            if (target == null) yield break;
        }

        target.localScale = to;
    }
}
