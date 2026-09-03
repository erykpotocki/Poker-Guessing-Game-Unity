using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    private void Awake()
    {
        PokerButtonTheme.EnsureController();
        NormalizeMenuButtonGroup();
        IncreaseVerticalButtonSpacing();
    }

    private void NormalizeMenuButtonGroup()
    {
        Button[] buttons = FindObjectsByType<Button>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        Transform group = null;
        foreach (Button button in buttons)
        {
            if (button != null &&
                button.transform.parent != null &&
                button.transform.parent.name.StartsWith("Panel przycisk"))
            {
                group = button.transform.parent;
                break;
            }
        }

        if (group == null)
            return;

        // The scene group was authored with a very large non-uniform scale.
        // Keep a slightly more decorative menu variant, but bring its real
        // footprint much closer to the compact Hot Seat controls.
        group.localScale = new Vector3(3.25f, 2.5f, 1f);
    }

    private void IncreaseVerticalButtonSpacing()
    {
        Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (buttons.Length < 2)
            return;

        float centerY = 0f;
        int count = 0;
        foreach (Button button in buttons)
        {
            if (button.transform is RectTransform rect)
            {
                centerY += rect.anchoredPosition.y;
                count++;
            }
        }

        if (count == 0)
            return;

        centerY /= count;
        foreach (Button button in buttons)
        {
            if (!(button.transform is RectTransform rect))
                continue;

            Vector2 position = rect.anchoredPosition;
            position.y = centerY + (position.y - centerY) * 1.14f;
            rect.anchoredPosition = position;
        }
    }

    public void GoCreateRoom()
    {
        SceneManager.LoadScene("GameModeSelect");
    }

    public void GoJoinRoom()
    {
        SceneManager.LoadScene("JoinRoom");
    }

    public void GoHotSeat()
    {
        Screen.orientation = ScreenOrientation.Portrait;

        Screen.autorotateToPortrait = true;
        Screen.autorotateToPortraitUpsideDown = false;
        Screen.autorotateToLandscapeLeft = false;
        Screen.autorotateToLandscapeRight = false;

        SceneManager.LoadScene("Hot Seat");
    }
}
