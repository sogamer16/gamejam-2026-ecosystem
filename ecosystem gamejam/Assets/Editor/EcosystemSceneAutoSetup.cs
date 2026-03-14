#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

[InitializeOnLoad]
public static class EcosystemSceneAutoSetup
{
    static EcosystemSceneAutoSetup()
    {
        EditorApplication.delayCall += EnsureActiveSceneSetup;
        EditorSceneManager.sceneOpened += (_, _) => EditorApplication.delayCall += EnsureActiveSceneSetup;
    }

    private static void EnsureActiveSceneSetup()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        string scenePath = EditorSceneManager.GetActiveScene().path;
        if (scenePath == "Assets/main.unity")
        {
            EnsureSetupSceneObjects();
        }
        else if (scenePath == "Assets/Scenes/SampleScene.unity")
        {
            EnsureGameplaySceneObjects();
        }
    }

    private static void EnsureSetupSceneObjects()
    {
        bool changed = false;
        if (Object.FindFirstObjectByType<SetupSceneController>() == null)
        {
            GameObject controller = new GameObject("SetupSceneController");
            controller.AddComponent<SetupSceneController>();
            changed = true;
        }

        if (Object.FindFirstObjectByType<EventSystem>() == null)
        {
            GameObject eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<InputSystemUIInputModule>();
            changed = true;
        }

        if (changed)
        {
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }
    }

    private static void EnsureGameplaySceneObjects()
    {
        bool changed = false;
        if (Object.FindFirstObjectByType<EcosystemController>() == null)
        {
            GameObject controller = new GameObject("EcosystemController");
            controller.AddComponent<EcosystemController>();
            changed = true;
        }

        if (Object.FindFirstObjectByType<EventSystem>() == null)
        {
            GameObject eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<InputSystemUIInputModule>();
            changed = true;
        }

        if (changed)
        {
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }
    }
}
#endif
