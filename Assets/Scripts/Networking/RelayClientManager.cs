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
    /// Relay Client 설정 담당
    /// </summary>
    public class RelayClientManager
    {
        private JoinAllocation _allocation;

#if UNITY_WEBGL
        private const string CONNECTION_TYPE = "wss";
#else
        private const string CONNECTION_TYPE = "dtls";
#endif

        public async Task<bool> SetupClientAsync(string joinCode)
        {
            try
            {
                if (string.IsNullOrEmpty(joinCode))
                    throw new ArgumentException("Join code is null or empty");

                // 1. Relay 참가
                NetworkLogUI.Log("[CLIENT] Joining Relay server...");
                _allocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
                Debug.Log($"[RelayClient] Joined with code: {joinCode}");
                NetworkLogUI.Log("[CLIENT] Joined Relay server");

                // 2. Transport 설정
                NetworkLogUI.Log("[CLIENT] Configuring transport...");
                var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
                if (transport == null)
                    throw new Exception("UnityTransport not found");

#if UNITY_WEBGL
                transport.UseWebSockets = true;
#endif
                transport.SetRelayServerData(_allocation.ToRelayServerData(CONNECTION_TYPE));

                // 3. Client 시작
                NetworkLogUI.Log("[CLIENT] Starting network client...");
                bool started = NetworkManager.Singleton.StartClient();
                
                if (!started)
                    throw new Exception("Failed to start client");

                NetworkLogUI.Log("[CLIENT] Network client started");
                Debug.Log("[RelayClient] Client setup completed");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RelayClient] Setup failed: {ex.Message}");
                NetworkLogUI.Log($"[CLIENT] Error: {ex.Message}");
                throw;
            }
        }
    }
}
