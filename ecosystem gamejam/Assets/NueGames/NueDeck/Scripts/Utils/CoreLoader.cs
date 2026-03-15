using System;
using NueGames.NueDeck.Scripts.Managers;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NueGames.NueDeck.Scripts.Utils
{
    [DefaultExecutionOrder(-11)]
    public class CoreLoader : MonoBehaviour
    {
        private void Awake()
        {
            try
            {
                if (!GameManager.Instance)
                    SceneManager.LoadScene("NueCore", LoadSceneMode.Additive);
                Destroy(gameObject);
            }
            catch (Exception ex)
            {
                Debug.LogError("You should add NueCore scene to build settings! " + ex.Message);
                throw;
            }
           
        }
    }
}
