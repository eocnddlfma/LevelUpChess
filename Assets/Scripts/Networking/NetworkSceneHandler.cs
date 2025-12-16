using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using LevelUpChess.Board;
using LevelUpChess.Core;
using LevelUpChess.Managers;
using LevelUpChess.UI;
using LevelUpChess.Upgrades;

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
            NetworkManager.Singleton.SceneManager.OnLoad += OnSceneLoad;
        }

        public void UnsubscribeFromSceneEvents()
        {
            if (NetworkManager.Singleton?.SceneManager != null)
            {
                NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnSceneLoadComplete;
                NetworkManager.Singleton.SceneManager.OnLoad -= OnSceneLoad;
            }
        }

        public void StartWaitingForClientsAndLoad(int maxPlayers, float timeout, bool isHost)
        {
            _isHost = isHost;
            
            if (isHost)
                StartCoroutine(WaitForClientsAndLoadScene(maxPlayers, timeout));
            else
            {
                NetworkLogUI.Log("[CLIENT] Waiting for host to load scene...");
                Debug.Log("[SceneHandler] Client waiting for scene load event from host");
            }
        }
        
        private void OnSceneLoad(ulong clientId, string sceneName, LoadSceneMode loadSceneMode, AsyncOperation asyncOperation)
        {
            Debug.Log($"[SceneHandler] OnSceneLoad called - Client: {clientId}, Scene: {sceneName}, Mode: {loadSceneMode}");
            
            if (sceneName == gameSceneName && !_isHost)
            {
                NetworkLogUI.Log($"[CLIENT] Loading {sceneName}...");
            }
        }

        private IEnumerator WaitForClientsAndLoadScene(int maxPlayers, float timeout)
        {
            NetworkLogUI.Log("[HOST] Waiting for players...");
            Debug.Log($"[SceneHandler] Current connections: {NetworkManager.Singleton.ConnectedClients.Count}/{maxPlayers}");
            
            float startTime = Time.time;

            while (NetworkManager.Singleton.ConnectedClients.Count < maxPlayers &&
                   Time.time - startTime < timeout)
            {
                yield return new WaitForSeconds(0.5f);
                Debug.Log($"[SceneHandler] Polling... {NetworkManager.Singleton.ConnectedClients.Count}/{maxPlayers}");
            }

            if (NetworkManager.Singleton.ConnectedClients.Count >= maxPlayers)
            {
                NetworkLogUI.Log("[HOST] All players connected! Starting game...");
                Debug.Log($"[SceneHandler] Loading scene now!");
                yield return new WaitForSeconds(1f);
                LoadGameScene();
            }
            else
            {
                int connections = NetworkManager.Singleton.ConnectedClients.Count;
                Debug.LogError($"[SceneHandler] Timeout: {connections}/{maxPlayers}");
                NetworkLogUI.Log($"Connection timeout ({connections}/{maxPlayers})");
            }
        }

        private void LoadGameScene()
        {
            Debug.Log($"[SceneHandler] LoadGameScene called by HOST!");
            
            if (NetworkManager.Singleton == null)
            {
                Debug.LogError("[SceneHandler] NetworkManager.Singleton is null!");
                return;
            }
            
            DontDestroyOnLoad(NetworkManager.Singleton.gameObject);
            
            // NetworkManager를 통해 씬 로드 (모든 클라이언트 동기화)
            Debug.Log($"[SceneHandler] Broadcasting scene load to all clients: {gameSceneName}");
            var status = NetworkManager.Singleton.SceneManager.LoadScene(gameSceneName, LoadSceneMode.Additive);
            
            if (status != SceneEventProgressStatus.Started)
            {
                Debug.LogError($"[SceneHandler] Failed to start scene load! Status: {status}");
            }
            else
            {
                Debug.Log($"[SceneHandler] Scene load started successfully!");
            }
        }

        private void OnSceneLoadComplete(string sceneName, LoadSceneMode loadSceneMode,
            List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
        {
            Debug.Log($"[SceneHandler] OnSceneLoadComplete: {sceneName} (mode: {loadSceneMode})");
            
            if (sceneName != gameSceneName) return;

            // 게임 씬을 활성 씬으로 먼저 설정
            Scene gameScene = SceneManager.GetSceneByName(gameSceneName);
            if (gameScene.isLoaded)
            {
                SceneManager.SetActiveScene(gameScene);
                Debug.Log($"[SceneHandler] Set active scene to: {gameSceneName}");
            }

            // 그 다음 이전 씬(MultiScene) 언로드
            Scene previousScene = SceneManager.GetSceneByName("MultiScene");
            if (previousScene.isLoaded && previousScene.name != gameSceneName)
            {
                Debug.Log($"[SceneHandler] Unloading previous scene: {previousScene.name}");
                SceneManager.UnloadSceneAsync(previousScene);
            }

            StartCoroutine(InitializeGameWithDelay());
            UnsubscribeFromSceneEvents();
        }

        private IEnumerator InitializeGameWithDelay()
        {
            yield return new WaitForSeconds(1f);

            FindFirstObjectByType<BoardGenerator>()?.InitializeExistingBoard();
            ServiceLocator.Get<NetworkGameManager>()?.SetTeamFromNetwork(_isHost);

            // UpgradeManager 확인
            var upgradeManager = FindFirstObjectByType<UpgradeManager>();
            if (upgradeManager != null)
            {
                Debug.Log($"[SceneHandler] UpgradeManager found: {upgradeManager.name}, Instance: {(UpgradeManager.Instance != null ? "Set" : "NULL")}");
            }
            else
            {
                Debug.LogError("[SceneHandler] UpgradeManager NOT FOUND in scene!");
            }

            OnSceneReady?.Invoke();
        }

        private void OnDestroy()
        {
            UnsubscribeFromSceneEvents();
        }
    }
}
