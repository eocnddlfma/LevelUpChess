using System;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;
using LevelUpChess.UI;

namespace LevelUpChess.Networking
{
    /// <summary>
    /// Unity Services 초기화 및 인증 관리
    /// </summary>
    public static class AuthManager
    {
        public static bool IsAuthenticated => AuthenticationService.Instance.IsSignedIn;
        public static string PlayerId => AuthenticationService.Instance.PlayerId;

        public static async Task<bool> InitializeAndAuthenticateAsync()
        {
            try
            {
                await InitializeServicesAsync();
                await SignInAsync();
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Auth] Failed: {ex.Message}");
                NetworkLogUI.Log($"Auth Error: {ex.Message}");
                return false;
            }
        }

        private static async Task InitializeServicesAsync()
        {
            if (UnityServices.State == ServicesInitializationState.Initialized)
                return;

            var options = new InitializationOptions();

#if UNITY_WEBGL
            string uniqueProfile = $"Player_{Guid.NewGuid().ToString().Substring(0, 8)}";
            options.SetProfile(uniqueProfile);
            Debug.Log($"[Auth] WebGL profile: {uniqueProfile}");
#endif

            await UnityServices.InitializeAsync(options);
            Debug.Log("[Auth] Unity Services initialized");
        }

        private static async Task SignInAsync()
        {
            if (AuthenticationService.Instance.IsSignedIn)
            {
                AuthenticationService.Instance.SignOut();
            }

#if !UNITY_WEBGL
            AuthenticationService.Instance.ClearSessionToken();
#endif

            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            Debug.Log($"[Auth] Signed in: {PlayerId}");
            NetworkLogUI.Log($"Player: {PlayerId.Substring(0, 8)}");
        }
    }
}
