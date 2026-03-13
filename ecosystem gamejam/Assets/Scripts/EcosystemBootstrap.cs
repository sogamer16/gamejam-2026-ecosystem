using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

public static class EcosystemBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InitializeBootstrap()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        EnsureControllerForActiveScene();
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureControllerForActiveScene();
    }

    private static void EnsureControllerForActiveScene()
    {
        if (Object.FindFirstObjectByType<EcosystemController>() != null || Object.FindFirstObjectByType<SetupSceneController>() != null)
        {
            return;
        }

        if (Object.FindFirstObjectByType<EventSystem>() == null)
        {
            GameObject eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<InputSystemUIInputModule>();
        }

        string activeScenePath = SceneManager.GetActiveScene().path;
        GameObject gameObject = new GameObject(activeScenePath == "Assets/main.unity" ? "SetupSceneController" : "EcosystemController");
        if (activeScenePath == "Assets/main.unity")
        {
            gameObject.AddComponent<SetupSceneController>();
        }
        else
        {
            gameObject.AddComponent<EcosystemController>();
        }
    }
}
