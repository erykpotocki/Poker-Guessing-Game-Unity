#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
internal static class PokerButtonThemePreview
{
    private static bool refreshQueued;

    static PokerButtonThemePreview()
    {
        EditorApplication.hierarchyChanged += QueueRefresh;
        EditorSceneManager.sceneOpened += (_, _) => QueueRefresh();
        EditorSceneManager.sceneSaving += (_, _) => ClearMainMenuPreview();
        EditorSceneManager.sceneSaved += _ => QueueRefresh();
        QueueRefresh();
    }

    private static void QueueRefresh()
    {
        if (Application.isPlaying || refreshQueued)
            return;

        refreshQueued = true;
        EditorApplication.delayCall += Refresh;
    }

    private static void Refresh()
    {
        refreshQueued = false;
        if (!Application.isPlaying)
        {
            PokerButtonTheme.RefreshEditorPreview();
            foreach (MainMenuUI menu in Object.FindObjectsByType<MainMenuUI>(
                FindObjectsInactive.Include, FindObjectsSortMode.None))
                menu.RefreshEditorPreview();
            foreach (GameModeSelectUI modeScreen in Object.FindObjectsByType<GameModeSelectUI>(
                FindObjectsInactive.Include, FindObjectsSortMode.None))
                modeScreen.RefreshEditorPreview();
        }
    }

    private static void ClearMainMenuPreview()
    {
        foreach (MainMenuUI menu in Object.FindObjectsByType<MainMenuUI>(
            FindObjectsInactive.Include, FindObjectsSortMode.None))
            menu.ClearEditorPreview();
    }
}
#endif
