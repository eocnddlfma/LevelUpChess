using Unity.Netcode;
using UnityEngine;
using LevelUpChess.Upgrades;

namespace LevelUpChess.Networking
{
    /// <summary>
    /// 네트워크 게임 오브젝트 스폰 관리
    /// NetworkManager에 연결하여 호스트 시작 시 필요한 오브젝트들을 스폰
    /// </summary>
    public class NetworkSpawner : MonoBehaviour
    {
        [Header("Prefabs to Spawn")]
        [SerializeField] private GameObject upgradeManagerPrefab;

        private void Start()
        {
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnServerStarted += OnServerStarted;
            }
        }

        private void OnDestroy()
        {
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnServerStarted -= OnServerStarted;
            }
        }

        private void OnServerStarted()
        {
            Debug.Log("[NetworkSpawner] Server started, spawning network objects...");
            
            SpawnUpgradeManager();
        }

        private void SpawnUpgradeManager()
        {
            // 이미 존재하는지 확인
            if (UpgradeManager.Instance != null)
            {
                Debug.Log("[NetworkSpawner] UpgradeManager already exists");
                return;
            }

            if (upgradeManagerPrefab == null)
            {
                Debug.LogError("[NetworkSpawner] UpgradeManager prefab is not assigned!");
                return;
            }

            // 프리팹 인스턴스 생성
            GameObject instance = Instantiate(upgradeManagerPrefab);
            instance.name = "UpgradeManager";

            // NetworkObject 스폰
            NetworkObject netObj = instance.GetComponent<NetworkObject>();
            if (netObj != null)
            {
                netObj.Spawn();
                Debug.Log("[NetworkSpawner] UpgradeManager spawned successfully");
            }
            else
            {
                Debug.LogError("[NetworkSpawner] UpgradeManager prefab has no NetworkObject component!");
                Destroy(instance);
            }
        }
    }
}
