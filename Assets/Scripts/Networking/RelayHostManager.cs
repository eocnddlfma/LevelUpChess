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
    public class RelayHostManager : MonoBehaviour
    {
        public string JoinCode { get; private set; }

        public async Task<bool> SetupHostAsync(int maxPlayers)
        {
            try
            {
                var allocation = await RelayService.Instance.CreateAllocationAsync(maxPlayers);
                JoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
                NetworkLogUI.Log($"[HOST] Code: {JoinCode}");

                ConfigureTransport(allocation);

                if (!NetworkManager.Singleton.StartHost())
                    throw new Exception("Failed to start host");

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RelayHost] {ex.Message}");
                throw;
            }
        }

        private void ConfigureTransport(Allocation allocation)
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
        private void ConfigureForWebGL(UnityTransport transport, Allocation allocation)
        {
            transport.UseWebSockets = true;
            transport.SetRelayServerData(allocation.ToRelayServerData("wss"));
        }
#else
        private void ConfigureForDesktop(UnityTransport transport, Allocation allocation)
        {
            transport.SetRelayServerData(allocation.ToRelayServerData("dtls"));
        }
#endif
    }
}
