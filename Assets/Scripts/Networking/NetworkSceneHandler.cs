using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using LevelUpChess.Board;
using LevelUpChess.Core;
using LevelUpChess.Managers;
using LevelUpChess.UI;

namespace LevelUpChess.Networking
{
    public class NetworkSceneHandler : MonoBehaviour
    {
        [SerializeField] private string gameSceneName = "ChessScene";
        
        private bool _isHost;

        public event System.Action OnSceneReady;

        public void SubscribeToSceneEvents()
        {
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnSceneLoadComplete;
        }

        public void UnsubscribeFromSceneEvents()
        {
            if (NetworkManager.Singleton?.SceneManager != null)
                NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnSceneLoadComplete;
        }

        public void StartWaitingForClientsAndLoad(int maxPlayers, float timeout, bool isHost)
        {
            _isHost = isHost;
            
            if (isHost)
                StartCoroutine(WaitForClientsAndLoadScene(maxPlayers, timeout));
            else
                NetworkLogUI.Log("[CLIENT] Waiting for host to load scene...");
        }

        private IEnumerator WaitForClientsAndLoadScene(int maxPlayers, float timeout)
        {
            NetworkLogUI.Log("[HOST] Waiting for players...");
            float startTime = Time.time;

            while (NetworkManager.Singleton.ConnectedClients.Count < maxPlayers &&
                   Time.time - startTime < timeout)
            {
                yield return new WaitForSeconds(0.5f);
            }

            if (NetworkManager.Singleton.ConnectedClients.Count >= maxPlayers)
            {
                NetworkLogUI.Log("[HOST] Starting game...");
                yield return new WaitForSeconds(2f);
                LoadGameScene();
            }
            else
            {
                int connections = NetworkManager.Singleton.ConnectedClients.Count;
                Debug.LogError($"[SceneHandler] Timeout: {connections}/{maxPlayers}");
            }
        }

        private void LoadGameScene()
        {
            if (NetworkManager.Singleton != null)
                DontDestroyOnLoad(NetworkManager.Singleton.gameObject);

            NetworkManager.Singleton.SceneManager.LoadScene(gameSceneName, LoadSceneMode.Additive);
        }

        private void OnSceneLoadComplete(string sceneName, LoadSceneMode loadSceneMode,
            List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
        {
            if (sceneName != gameSceneName) return;

            UnloadPreviousScenes();
            StartCoroutine(InitializeGameWithDelay());
            UnsubscribeFromSceneEvents();
        }

        private void UnloadPreviousScenes()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene.name != gameSceneName && scene.isLoaded)
                    SceneManager.UnloadSceneAsync(scene);
            }
        }

        private IEnumerator InitializeGameWithDelay()
        {
            yield return new WaitForSeconds(1f);

            FindFirstObjectByType<BoardGenerator>()?.InitializeExistingBoard();
            ServiceLocator.Get<NetworkGameManager>()?.SetTeamFromNetwork(_isHost);

            OnSceneReady?.Invoke();
        }

        private void OnDestroy()
        {
            UnsubscribeFromSceneEvents();
        }
    }
}
