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
    /// <summary>
    /// 네트워크 씬 로딩/언로딩 관리
    /// </summary>
    public class NetworkSceneHandler
    {
        private readonly MonoBehaviour _coroutineRunner;
        private readonly string _gameSceneName;
        private bool _isHost;

        public event System.Action OnSceneReady;

        public NetworkSceneHandler(MonoBehaviour coroutineRunner, string gameSceneName)
        {
            _coroutineRunner = coroutineRunner;
            _gameSceneName = gameSceneName;
        }

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
                _coroutineRunner.StartCoroutine(WaitForClientsAndLoadScene(maxPlayers, timeout));
            else
                NetworkLogUI.Log("[CLIENT] Waiting for host to load scene...");
        }

        private IEnumerator WaitForClientsAndLoadScene(int maxPlayers, float timeout)
        {
            NetworkLogUI.Log("[HOST] Waiting for all players...");
            float startTime = Time.time;

            while (NetworkManager.Singleton.ConnectedClients.Count < maxPlayers &&
                   Time.time - startTime < timeout)
            {
                yield return new WaitForSeconds(0.5f);

                int connections = NetworkManager.Singleton.ConnectedClients.Count;
                float elapsed = Time.time - startTime;

                if (Time.frameCount % 30 == 0)
                    NetworkLogUI.Log($"[HOST] Players: {connections}/{maxPlayers} ({elapsed:F0}s)");
            }

            if (NetworkManager.Singleton.ConnectedClients.Count >= maxPlayers)
            {
                NetworkLogUI.Log($"[HOST] All {maxPlayers} players connected!");
                NetworkLogUI.Log("[HOST] Starting game...");
                yield return new WaitForSeconds(2f);

                LoadGameScene();
            }
            else
            {
                int connections = NetworkManager.Singleton.ConnectedClients.Count;
                Debug.LogError($"[SceneHandler] Timeout: {connections}/{maxPlayers} players");
                NetworkLogUI.Log($"[HOST] Timeout: {connections}/{maxPlayers} players");
            }
        }

        private void LoadGameScene()
        {
            if (NetworkManager.Singleton != null)
                Object.DontDestroyOnLoad(NetworkManager.Singleton.gameObject);

            NetworkManager.Singleton.SceneManager.LoadScene(_gameSceneName, LoadSceneMode.Additive);
            NetworkLogUI.Log("[HOST] Loading game...");
            Debug.Log("[SceneHandler] Loading scene with Additive mode");
        }

        private void OnSceneLoadComplete(string sceneName, LoadSceneMode loadSceneMode,
            List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
        {
            if (sceneName != _gameSceneName)
                return;

            Debug.Log($"[SceneHandler] Scene loaded: {sceneName}");
            UnloadPreviousScenes();
            _coroutineRunner.StartCoroutine(InitializeGameWithDelay());
            UnsubscribeFromSceneEvents();
        }

        private void UnloadPreviousScenes()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene.name != _gameSceneName && scene.isLoaded)
                {
                    Debug.Log($"[SceneHandler] Unloading: {scene.name}");
                    SceneManager.UnloadSceneAsync(scene);
                }
            }
        }

        private IEnumerator InitializeGameWithDelay()
        {
            yield return new WaitForSeconds(1f);

            Debug.Log($"[SceneHandler] Initializing game - isHost: {_isHost}");

            var boardGenerator = Object.FindFirstObjectByType<BoardGenerator>();
            if (boardGenerator != null)
            {
                Debug.Log("[SceneHandler] Initializing existing board...");
                boardGenerator.InitializeExistingBoard();
            }

            var networkGameManager = ServiceLocator.Get<NetworkGameManager>();
            if (networkGameManager != null)
            {
                networkGameManager.SetTeamFromNetwork(_isHost);
                Debug.Log($"[SceneHandler] Team set to: {networkGameManager.LocalPlayerTeam}");
            }
            else
            {
                Debug.LogError("[SceneHandler] NetworkGameManager is NULL!");
            }

            OnSceneReady?.Invoke();
        }
    }
}
