using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// 게임 내 메시지를 화면에 표시하는 UI 관리자
/// </summary>
public class GameMessageUI : MonoBehaviour
{
    public static GameMessageUI Instance { get; private set; }
    
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private float defaultDisplayDuration = 2f;
    
    private Coroutine currentMessageCoroutine;
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        // 초기 상태: 텍스트 숨김
        if (messageText != null)
        {
            messageText.text = "";
            messageText.gameObject.SetActive(false);
        }
        
        Debug.Log($"[GameMessageUI] Initialized. MessageText assigned: {messageText != null}");
    }
    
    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
    
    /// <summary>
    /// 메시지를 지정된 시간동안 표시
    /// duration = 0이면 지속 표시 (명시적으로 HideMessage 호출 필요)
    /// </summary>
    public void ShowMessage(string message, float duration = -1f)
    {
        if (messageText == null)
        {
            Debug.LogWarning("[GameMessageUI] Message text is not assigned!");
            return;
        }
        
        // GameObject가 비활성화되어 있으면 활성화
        if (!gameObject.activeInHierarchy)
        {
            Debug.LogWarning("[GameMessageUI] GameObject is inactive! Activating...");
            gameObject.SetActive(true);
        }
        
        // 이전 메시지 코루틴 중지
        if (currentMessageCoroutine != null)
        {
            StopCoroutine(currentMessageCoroutine);
        }
        
        // duration이 0이면 지속 표시
        if (duration == 0f)
        {
            messageText.text = message;
            messageText.gameObject.SetActive(true);
            Debug.Log($"[GameMessageUI] Showing persistent message: {message}, Active: {messageText.gameObject.activeSelf}");
            return;
        }
        
        // 새 메시지 표시
        float displayDuration = duration > 0 ? duration : defaultDisplayDuration;
        currentMessageCoroutine = StartCoroutine(ShowMessageCoroutine(message, displayDuration));
    }
    
    /// <summary>
    /// 메시지를 즉시 숨김
    /// </summary>
    public void HideMessage()
    {
        if (currentMessageCoroutine != null)
        {
            StopCoroutine(currentMessageCoroutine);
            currentMessageCoroutine = null;
        }
        
        if (messageText != null)
        {
            messageText.text = "";
            messageText.gameObject.SetActive(false);
        }
    }
    
    private IEnumerator ShowMessageCoroutine(string message, float duration)
    {
        // 메시지 표시
        messageText.text = message;
        messageText.gameObject.SetActive(true);
        
        Debug.Log($"[GameMessageUI] Showing message: {message}, Active: {messageText.gameObject.activeSelf}");
        
        // 지정된 시간 대기
        yield return new WaitForSeconds(duration);
        
        // 메시지 숨김
        messageText.text = "";
        messageText.gameObject.SetActive(false);
        
        currentMessageCoroutine = null;
    }
}
