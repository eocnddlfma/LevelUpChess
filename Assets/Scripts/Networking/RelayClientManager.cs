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
    public class RelayClientManager : MonoBehaviour
    {
        public async Task<bool> SetupClientAsync(string joinCode)
        {
            try
            {
                if (string.IsNullOrEmpty(joinCode))
                    throw new ArgumentException("Join code is null or empty");

                NetworkLogUI.Log("[CLIENT] Joining...");
                var allocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
                ConfigureTransport(allocation);

                if (!NetworkManager.Singleton.StartClient())
                    throw new Exception("Failed to start client");

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RelayClient] {ex.Message}");
                throw;
            }
        }

        private void ConfigureTransport(JoinAllocation allocation)
        {
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            if (transport == null)
                throw new Exception("UnityTransport not found");

#if UNITY_WEBGL
            ConfigureForWebGL(transport, allocation);
#else
            ConfigureForDesktop(transport, allocation);
#endif
        }

#if UNITY_WEBGL
        private void ConfigureForWebGL(UnityTransport transport, JoinAllocation allocation)
        {
            transport.UseWebSockets = true;
            transport.SetRelayServerData(allocation.ToRelayServerData("wss"));
        }
#else
        private void ConfigureForDesktop(UnityTransport transport, JoinAllocation allocation)
        {
            transport.SetRelayServerData(allocation.ToRelayServerData("dtls"));
        }
#endif
    }
}
