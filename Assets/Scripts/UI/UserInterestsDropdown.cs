using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using Firebase.Auth;
using RamRoutes.Services;

/// <summary>
/// Attach to a GameObject that also has a UnityEngine.UI.Dropdown component.
/// Tag vocabulary mirrors Admin/scraper/tagger.js's TAGS keys - the scraper tags
/// events with these same strings, and Admin/scraper/recommend.js scores events
/// against this user field, so they must stay in sync.
/// </summary>
public class UserInterestsDropdown : MonoBehaviour
{
    private static readonly string[] AvailableTags =
    {
        "academic", "sports", "arts", "social", "food",
        "career", "cultural", "spiritual", "tech", "fitness", "mental-health"
    };

    [SerializeField] private Dropdown dropdown;

    private readonly HashSet<string> selectedTags = new HashSet<string>();
    private UserService userService;
    private string userId;

    async void Start()
    {
        if (dropdown == null)
        {
            dropdown = GetComponent<Dropdown>();
        }

        userService = new UserService();
        userId = FirebaseAuth.DefaultInstance?.CurrentUser?.UserId;

        if (!string.IsNullOrEmpty(userId))
        {
            var user = await userService.RetrieveUserById(userId);
            if (user != null && user.interests != null)
            {
                foreach (string tag in user.interests)
                {
                    selectedTags.Add(tag);
                }
            }
        }

        RefreshOptions();
        dropdown.onValueChanged.AddListener(OnItemSelected);
    }

    private void OnItemSelected(int index)
    {
        if (index < 0 || index >= AvailableTags.Length) return;

        string tag = AvailableTags[index];
        if (!selectedTags.Add(tag))
        {
            selectedTags.Remove(tag);
        }

        RefreshOptions();
        SaveInterests();
    }

    private void RefreshOptions()
    {
        dropdown.ClearOptions();

        List<string> labels = AvailableTags
            .Select(tag => (selectedTags.Contains(tag) ? "✓ " : "") + tag)
            .ToList();

        // Trailing dummy option, never a real tag. Dropdown.Set() clamps whatever
        // value you give it into [0, options.Count - 1], so there's no out-of-range
        // sentinel we can park on - any negative value just gets clamped back down to
        // a real tag's index (always index 0, "academic"), permanently reproducing the
        // "value == m_Value" no-op guard for that one tag. Parking on this extra slot
        // instead is always a valid, in-range index that's guaranteed distinct from
        // every real tag, so every tag click reliably differs from the current value.
        labels.Add("");
        dropdown.AddOptions(labels);
        dropdown.SetValueWithoutNotify(AvailableTags.Length);

        dropdown.captionText.text = selectedTags.Count > 0
            ? string.Join(", ", OrderedSelectedTags())
            : "Select interests";
    }

    // HashSet iteration order is unspecified, so any join over selectedTags directly
    // won't consistently match either the dropdown's own list order or what gets
    // saved to Firestore. Always project through AvailableTags's fixed order instead.
    private IEnumerable<string> OrderedSelectedTags()
    {
        return AvailableTags.Where(selectedTags.Contains);
    }

    private async void SaveInterests()
    {
        if (string.IsNullOrEmpty(userId)) return;

        await userService.UpdateUserInterests(userId, OrderedSelectedTags().ToList());
    }

    void OnDestroy()
    {
        if (dropdown != null)
        {
            dropdown.onValueChanged.RemoveListener(OnItemSelected);
        }
    }
}
