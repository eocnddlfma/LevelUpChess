using System;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using LevelUpChess.UI;

namespace LevelUpChess.Networking
{
    /// <summary>
    /// Relay Host 설정 담당
    /// </summary>
    public class RelayHostManager
    {
        private Allocation _allocation;
        
        public string JoinCode { get; private set; }

#if UNITY_WEBGL
        private const string CONNECTION_TYPE = "wss";
#else
        private const string CONNECTION_TYPE = "dtls";
#endif

        public async Task<bool> SetupHostAsync(int maxPlayers)
        {
            try
            {
                // 1. Relay Allocation 생성
                NetworkLogUI.Log("[HOST] Creating Relay allocation...");
                _allocation = await RelayService.Instance.CreateAllocationAsync(maxPlayers);
                Debug.Log($"[RelayHost] Allocation ID: {_allocation.AllocationId}");

                // 2. Join Code 생성
                NetworkLogUI.Log("[HOST] Generating join code...");
                JoinCode = await RelayService.Instance.GetJoinCodeAsync(_allocation.AllocationId);
                Debug.Log($"[RelayHost] Join Code: {JoinCode}");
                NetworkLogUI.Log($"[HOST] Join code: {JoinCode}");

                // 3. Transport 설정
                NetworkLogUI.Log("[HOST] Configuring transport...");
                var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
                if (transport == null)
                    throw new Exception("UnityTransport not found");

#if UNITY_WEBGL
                transport.UseWebSockets = true;
#endif
                transport.SetRelayServerData(_allocation.ToRelayServerData(CONNECTION_TYPE));

                // 4. Host 시작
                NetworkLogUI.Log("[HOST] Starting network host...");
                bool started = NetworkManager.Singleton.StartHost();
                
                if (!started)
                    throw new Exception("Failed to start host");

                NetworkLogUI.Log("[HOST] Network host started");
                Debug.Log("[RelayHost] Host setup completed");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RelayHost] Setup failed: {ex.Message}");
                NetworkLogUI.Log($"[HOST] Error: {ex.Message}");
                throw;
            }
        }
    }
}
