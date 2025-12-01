using UnityEngine;
using UnityEngine.UI;
using TMPro;
using LevelUpChess.Core;

namespace LevelUpChess.UI
{
    /// <summary>
    /// 네트워크 로그 UI - 연결 상태 및 디버그 메시지 표시
    /// </summary>
    public class NetworkLogUI : MonoBehaviour
    {
        [SerializeField] private Transform logContainer;
        [SerializeField] private GameObject logPrefab;
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private int maxLogs = 50;

        private void Awake()
        {
            if (ServiceLocator.Has<NetworkLogUI>())
            {
                Destroy(gameObject);
                return;
            }
            ServiceLocator.Register(this);
        }

        private void Start()
        {
            Debug.Log("[NetworkLogUI] Initialized");
        }

        /// <summary>
        /// 로그 메시지를 UI에 표시
        /// </summary>
        public void DisplayLog(string message)
        {
            if (logPrefab == null || logContainer == null) 
            {
                Debug.LogWarning("[NetworkLogUI] messagePrefab or messageContainer is not assigned");
                return;
            }

            // 최대 메시지 수 초과 시 오래된 메시지 제거
            while (logContainer.childCount >= maxLogs)
            {
                Destroy(logContainer.GetChild(0).gameObject);
            }

            GameObject ui = Instantiate(logPrefab, logContainer);
            TextMeshProUGUI text = ui.GetComponent<TextMeshProUGUI>();
            if (text != null) 
            {
                text.text = $"[{System.DateTime.Now:HH:mm:ss}] {message}";
            }

            if (scrollRect != null)
            {
                Canvas.ForceUpdateCanvases();
                scrollRect.verticalNormalizedPosition = 0f;
            }
        }

        /// <summary>
        /// 정적 메서드로 외부에서 쉽게 로그 추가
        /// </summary>
        public static void Log(string message)
        {
            var instance = ServiceLocator.Get<NetworkLogUI>();
            if (instance != null)
            {
                instance.DisplayLog(message);
            }
            
            // 콘솔에도 출력
            Debug.Log($"[NetworkLog] {message}");
        }

        /// <summary>
        /// 모든 로그 메시지 제거
        /// </summary>
        public void Clear()
        {
            if (logContainer == null) return;
            
            foreach (Transform child in logContainer)
            {
                Destroy(child.gameObject);
            }
        }

        private void OnDestroy()
        {
            if (ServiceLocator.Get<NetworkLogUI>() == this)
                ServiceLocator.Unregister<NetworkLogUI>();
        }
    }
}
