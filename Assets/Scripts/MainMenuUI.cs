using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    private void Awake()
    {
        PokerButtonTheme.EnsureController();
        IncreaseVerticalButtonSpacing();
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
