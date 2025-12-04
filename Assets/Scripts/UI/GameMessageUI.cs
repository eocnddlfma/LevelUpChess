using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using LevelUpChess.Core;

namespace LevelUpChess.UI
{
    /// <summary>
    /// 게임 내 메시지를 화면에 표시하는 UI 관리자
    /// </summary>
    public class GameMessageUI : MonoBehaviour
    {
    
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private float defaultDisplayDuration = 2f;
    
    private Coroutine currentMessageCoroutine;
    
    private void Awake()
    {
        // 기존에 등록된 서비스가 있으면 제거 (파괴된 객체일 수 있음)
        var existing = ServiceLocator.Get<GameMessageUI>();
        if (existing != null && existing != this)
        {
            // 기존 서비스가 유효한 객체인지 확인 (ReferenceEquals로 null 체크)
            if (!ReferenceEquals(existing, null) && existing.gameObject != null && existing.gameObject.scene.isLoaded)
            {
                Debug.Log("[GameMessageUI] Another valid instance exists, destroying this one.");
                Destroy(gameObject);
                return;
            }
            else
            {
                // 기존 서비스가 파괴된 객체면 제거하고 새로 등록
                Debug.Log("[GameMessageUI] Existing instance was invalid, replacing...");
                ServiceLocator.Unregister<GameMessageUI>();
            }
        }
        
        ServiceLocator.Register(this);
        
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
        if (ServiceLocator.Get<GameMessageUI>() == this)
            ServiceLocator.Unregister<GameMessageUI>();
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
}
