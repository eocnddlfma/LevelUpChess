using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using LevelUpChess.Pieces;
using LevelUpChess.Core;

namespace LevelUpChess.UI
{
    /// <summary>
    /// 레벨업 팝업 UI
    /// </summary>
    public class LevelUpPopupUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI messageText;
        [SerializeField] private Button closeButton;
        [SerializeField] private float autoCloseDelay = 3f;

        private System.Action onClosed;

        private void Awake()
        {
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(OnCloseClicked);
            }
            gameObject.SetActive(false);
        }

        public void Show(ChessPiece piece, int newLevel, System.Action onComplete = null)
        {
            onClosed = onComplete;

            if (titleText != null)
            {
                titleText.text = "레벨 업!";
            }

            if (messageText != null)
            {
                messageText.text = $"{piece.name}이(가) 레벨 {newLevel}로 상승했습니다!";
            }

            gameObject.SetActive(true);

            // 자동 닫기
            StartCoroutine(AutoClose());
        }

        private void OnCloseClicked()
        {
            Close();
        }

        private void Close()
        {
            gameObject.SetActive(false);
            onClosed?.Invoke();
        }

        private IEnumerator AutoClose()
        {
            yield return new WaitForSeconds(autoCloseDelay);
            if (gameObject.activeSelf)
            {
                Close();
            }
        }
    }
}