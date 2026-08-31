using UnityEngine;

public class HotSeatOrientationLock : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void LockPortraitBeforeFirstScene()
    {
        LockPortrait();
    }

    private void Awake()
    {
        LockPortrait();
    }

    private void Start()
    {
        LockPortrait();
    }

    public static void LockPortrait()
    {
        Screen.orientation = ScreenOrientation.Portrait;

        Screen.autorotateToPortrait = true;
        Screen.autorotateToPortraitUpsideDown = false;
        Screen.autorotateToLandscapeLeft = false;
        Screen.autorotateToLandscapeRight = false;
    }
}
