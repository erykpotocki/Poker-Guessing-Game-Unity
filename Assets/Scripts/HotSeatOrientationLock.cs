using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class HotSeatOrientationLock : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void LockPortraitBeforeFirstScene()
    {
        LockPortrait();
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
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

    public static void LockLandscape()
    {
        Screen.autorotateToPortrait = false;
        Screen.autorotateToPortraitUpsideDown = false;
        Screen.autorotateToLandscapeLeft = true;
        Screen.autorotateToLandscapeRight = true;
        Screen.orientation = ScreenOrientation.LandscapeLeft;
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        bool landscape = scene.name == "Game" || scene.name == "BootLoading";
        if (landscape)
            LockLandscape();
        else
            LockPortrait();

        ConfigureCanvases(landscape);
    }

    private static void ConfigureCanvases(bool landscape)
    {
        CanvasScaler[] scalers = Object.FindObjectsByType<CanvasScaler>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (CanvasScaler scaler in scalers)
        {
            if (scaler == null)
                continue;

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = landscape
                ? new Vector2(1920f, 1080f)
                : new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
        }

        OverscanFullScreenBackgrounds();
    }

    private static void OverscanFullScreenBackgrounds()
    {
        Image[] images = Object.FindObjectsByType<Image>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (Image image in images)
        {
            if (image == null ||
                (image.name != "Background" && image.name != "BackgroundGame" && image.name != "Czarne"))
                continue;

            RectTransform rect = image.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(-32f, -32f);
            rect.offsetMax = new Vector2(32f, 32f);
            image.preserveAspect = false;
        }
    }
}
