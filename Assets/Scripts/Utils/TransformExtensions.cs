using UnityEngine;

/// <summary>
/// Recursively searches all descendants of a transform, at any depth, for the first
/// child whose GameObject name exactly matches. Unlike Transform.Find, this isn't
/// limited to direct children or an exact slash-separated path.
/// </summary>
public static class TransformExtensions
{
    public static Transform FindDeepChild(this Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name)
            {
                return child;
            }

            Transform found = child.FindDeepChild(name);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }
}
