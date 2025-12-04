using System;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;
using LevelUpChess.UI;

namespace LevelUpChess.Networking
{
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

#if UNITY_WEBGL
            await InitializeForWebGL();
#else
            await InitializeForDesktop();
#endif
        }

#if UNITY_WEBGL
        private static async Task InitializeForWebGL()
        {
            var options = new InitializationOptions();
            options.SetProfile($"Player_{Guid.NewGuid().ToString().Substring(0, 8)}");
            await UnityServices.InitializeAsync(options);
        }
#else
        private static async Task InitializeForDesktop()
        {
            await UnityServices.InitializeAsync();
        }
#endif

        private static async Task SignInAsync()
        {
            if (AuthenticationService.Instance.IsSignedIn)
            {
                AuthenticationService.Instance.SignOut();
            }

#if UNITY_WEBGL
            await SignInForWebGL();
#else
            await SignInForDesktop();
#endif
        }

#if UNITY_WEBGL
        private static async Task SignInForWebGL()
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            NetworkLogUI.Log($"Player: {PlayerId.Substring(0, 8)}");
        }
#else
        private static async Task SignInForDesktop()
        {
            AuthenticationService.Instance.ClearSessionToken();
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            NetworkLogUI.Log($"Player: {PlayerId.Substring(0, 8)}");
        }
#endif
    }
}
