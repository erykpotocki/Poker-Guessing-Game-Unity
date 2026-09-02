using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuUI : MonoBehaviour
{
    private void Awake()
    {
        PokerButtonTheme.EnsureController();
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
