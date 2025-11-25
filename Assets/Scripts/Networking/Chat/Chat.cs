using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 채팅 UI 시스템 - 메시지 표시만 담당
/// 네트워크 동기화는 NetworkChat에서 처리
/// </summary>
public class Chat : MonoBehaviour
{
    [SerializeField] private Transform messageContainer;
    [SerializeField] private GameObject messagePrefab;
    [SerializeField] private ScrollRect scrollRect;

    public static Chat Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else if (Instance != this)
            Destroy(gameObject);
    }

    private void Start()
    {
        Debug.Log("[Chat] Chat UI initialized");
    }

    /// <summary>
    /// 메시지를 UI에 표시 (로컬에서만)
    /// </summary>
    public void DisplayMessage(string message)
    {
        if (messagePrefab == null || messageContainer == null) 
        {
            Debug.LogError("[Chat] messagePrefab or messageContainer is NULL!");
            return;
        }

        GameObject ui = Instantiate(messagePrefab, messageContainer);
        TextMeshProUGUI text = ui.GetComponent<TextMeshProUGUI>();
        if (text != null) 
        {
            text.text = $"[{System.DateTime.Now:HH:mm:ss}] {message}";
            Debug.Log($"[Chat] Message displayed: {message}");
        }
        else
        {
            Debug.LogError("[Chat] TextMeshProUGUI component not found on message prefab!");
        }

        if (scrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 0f;
        }
    }

    /// <summary>
    /// 정적 메서드로 외부에서 쉽게 메시지 표시
    /// </summary>
    public static void AddMessage(string message)
    {
        Debug.Log($"[Chat] AddMessage: {message}");
        
        if (Instance != null)
        {
            Instance.DisplayMessage(message);
        }
        else
        {
            Debug.LogWarning($"[Chat] Chat instance not found: {message}");
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}

