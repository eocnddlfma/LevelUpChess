using UnityEngine;
using UnityEngine.UI;
using TMPro;
using LevelUpChess.Events;
using LevelUpChess.Pieces;

namespace LevelUpChess.UI
{
    /// <summary>
    /// 마우스를 따라다니는 툴팁 UI
    /// EventBus를 통해 MouseHoverBeganEvent/EndedEvent를 구독하여 표시
    /// </summary>
    public class TooltipUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private RectTransform tooltipPanel;
        [SerializeField] private TextMeshProUGUI tooltipText;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Image panelBackground;

        [Header("Settings")]
        [SerializeField] private Vector2 offset = new Vector2(20f, 20f);
        [SerializeField] private float fadeSpeed = 10f;
        [SerializeField] private Vector2 screenPadding = new Vector2(10f, 10f);
        
        [Header("Color Settings")]
        [SerializeField] private Color whitePieceBackgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.95f);
        [SerializeField] private Color whitePieceTextColor = Color.white;
        [SerializeField] private Color blackPieceBackgroundColor = new Color(0.9f, 0.9f, 0.9f, 0.95f);
        [SerializeField] private Color blackPieceTextColor = Color.black;

        private Canvas _canvas;
        private ITooltipProvider _currentProvider;
        private bool _isVisible;

        private void Awake()
        {
            _canvas = GetComponentInParent<Canvas>();
            
            if (canvasGroup == null)
                canvasGroup = tooltipPanel.GetComponent<CanvasGroup>();
            
            if (panelBackground == null)
                panelBackground = tooltipPanel.GetComponent<Image>();

            HideImmediate();
        }

        private void OnEnable()
        {
            Bus<MouseHoverBeganEvent>.OnEvent += OnMouseHoverBegan;
            Bus<MouseHoverEndedEvent>.OnEvent += OnMouseHoverEnded;
        }

        private void OnDisable()
        {
            Bus<MouseHoverBeganEvent>.OnEvent -= OnMouseHoverBegan;
            Bus<MouseHoverEndedEvent>.OnEvent -= OnMouseHoverEnded;
        }

        private void Update()
        {
            if (_isVisible)
            {
                UpdatePosition();
                FadeIn();
            }
            else
            {
                FadeOut();
            }
        }

        private void OnMouseHoverBegan(MouseHoverBeganEvent eventData)
        {
            // Tile이 ITooltipProvider를 구현 (타일 위의 기물 정보 반환)
            var provider = eventData.Target.GetComponent<ITooltipProvider>();
            if (provider != null)
            {
                _currentProvider = provider;
                ShowTooltip();
            }
        }

        private void OnMouseHoverEnded(MouseHoverEndedEvent eventData)
        {
            HideTooltip();
        }

        private void ShowTooltip()
        {
            _isVisible = true;
            tooltipPanel.gameObject.SetActive(true);
            UpdateContent();
            UpdatePosition();
        }

        private void HideTooltip()
        {
            _isVisible = false;
            _currentProvider = null;
        }
        
        private void HideImmediate()
        {
            _isVisible = false;
            _currentProvider = null;
            
            if (canvasGroup != null)
                canvasGroup.alpha = 0f;
            
            if (tooltipPanel != null)
                tooltipPanel.gameObject.SetActive(false);
        }
        
        private void FadeIn()
        {
            if (canvasGroup != null && canvasGroup.alpha < 1f)
            {
                canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, 1f, fadeSpeed * Time.deltaTime);
            }
        }
        
        private void FadeOut()
        {
            if (canvasGroup != null && canvasGroup.alpha > 0f)
            {
                canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, 0f, fadeSpeed * Time.deltaTime);
                
                if (canvasGroup.alpha <= 0f && tooltipPanel != null)
                {
                    tooltipPanel.gameObject.SetActive(false);
                }
            }
        }

        private void UpdateContent()
        {
            if (_currentProvider == null || tooltipText == null)
                return;

            string content = _currentProvider.GetTooltipContent();
            
            // 콘텐츠가 null이면 툴팅 숨기기 (빈 타일)
            if (string.IsNullOrEmpty(content))
            {
                HideTooltip();
                return;
            }
            
            tooltipText.text = content;
            
            // 팀에 따라 배경색과 텍스트 색상 변경
            Team? team = _currentProvider.GetTooltipTeam();
            if (team.HasValue)
            {
                if (team.Value == Team.White)
                {
                    // 백팀: 검은 배경, 흰색 텍스트
                    if (panelBackground != null)
                        panelBackground.color = whitePieceBackgroundColor;
                    if (tooltipText != null)
                        tooltipText.color = whitePieceTextColor;
                }
                else // Black
                {
                    // 흔팀: 흰 배경, 검은 텍스트
                    if (panelBackground != null)
                        panelBackground.color = blackPieceBackgroundColor;
                    if (tooltipText != null)
                        tooltipText.color = blackPieceTextColor;
                }
            }
            else
            {
                // 기본 색상 (백팀 스타일)
                if (panelBackground != null)
                    panelBackground.color = whitePieceBackgroundColor;
                if (tooltipText != null)
                    tooltipText.color = whitePieceTextColor;
            }

            // 텍스트 크기에 맞춰 패널 크기 조정
            LayoutRebuilder.ForceRebuildLayoutImmediate(tooltipPanel);
        }

        private void UpdatePosition()
        {
            if (_canvas == null || tooltipPanel == null)
                return;

            Vector2 mousePos = Input.mousePosition;
            SetTooltipPosition(mousePos);
            ClampToScreen();
        }
        
        private void SetTooltipPosition(Vector2 mousePos)
        {
            if (_canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                tooltipPanel.position = mousePos + offset;
            }
            else if (_canvas.renderMode == RenderMode.ScreenSpaceCamera && _canvas.worldCamera != null)
            {
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _canvas.transform as RectTransform,
                    mousePos,
                    _canvas.worldCamera,
                    out Vector2 localPoint))
                {
                    tooltipPanel.localPosition = localPoint + offset;
                }
            }
        }

        private void ClampToScreen()
        {
            Vector3[] corners = new Vector3[4];
            tooltipPanel.GetWorldCorners(corners);

            Vector2 min = RectTransformUtility.WorldToScreenPoint(_canvas.worldCamera, corners[0]);
            Vector2 max = RectTransformUtility.WorldToScreenPoint(_canvas.worldCamera, corners[2]);

            Vector2 adjustedPosition = tooltipPanel.position;

            // 화면 경계 체크
            if (max.x > Screen.width)
                adjustedPosition.x -= (max.x - Screen.width + screenPadding.x);
            
            if (min.x < 0)
                adjustedPosition.x += (Mathf.Abs(min.x) + screenPadding.x);
            
            if (max.y > Screen.height)
                adjustedPosition.y -= (max.y - Screen.height + screenPadding.y);
            
            if (min.y < 0)
                adjustedPosition.y += (Mathf.Abs(min.y) + screenPadding.y);

            tooltipPanel.position = adjustedPosition;
        }
    }
}
