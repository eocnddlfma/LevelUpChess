using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using LevelUpChess.Upgrades;
using DG.Tweening;
using UnityEngine.EventSystems;

namespace LevelUpChess.Upgrades.UI
{
    /// <summary>
    /// 업그레이드 카드 UI - 개별 업그레이드 선택지 표시
    /// </summary>
    public class UpgradeCardUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [Header("UI References")]
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image iconImage;
        [SerializeField] private Image rarityBorder;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private TextMeshProUGUI typeText;       
        [Header("Rarity Colors")]
        [SerializeField] private Color commonColor = new Color(0.6f, 0.6f, 0.6f);
        [SerializeField] private Color uncommonColor = new Color(0.2f, 0.8f, 0.2f);
        [SerializeField] private Color rareColor = new Color(0.2f, 0.4f, 1f);
        [SerializeField] private Color epicColor = new Color(0.6f, 0.2f, 0.8f);
        [SerializeField] private Color legendaryColor = new Color(1f, 0.8f, 0.2f);

        [Header("Type Icons")]
        [SerializeField] private Sprite movementIcon;
        [SerializeField] private Sprite statIcon;
        [SerializeField] private Sprite abilityIcon;
        [SerializeField] private Sprite globalIcon;

        [Header("Animation")]
        [SerializeField] private float hoverScale = 1.1f;
        [SerializeField] private float animationSpeed = 10f;
        [SerializeField] private float selectionScale = 1.25f;
        [SerializeField] private float selectionDuration = 0.4f;
        [SerializeField] private Color selectionGlowColor = new Color(1f, 0.85f, 0.4f);

        private UpgradeBaseSO _upgrade;
        private int _cardIndex;
        private Vector3 _originalScale;
        private bool _isHovered;
        private Tween _selectionTween;
        private Color _originalBorderColor;
        private bool _isInteractable = true;
        private CanvasGroup _canvasGroup;

        public UpgradeBaseSO Upgrade => _upgrade;
        public event System.Action<int> OnCardSelected;

        private void Awake()
        {
            _originalScale = transform.localScale;
            if (rarityBorder != null)
                _originalBorderColor = rarityBorder.color;
            
            // 버튼을 사용하지 않음. 전체 카드 클릭으로 처리합니다.
            _canvasGroup = GetComponent<CanvasGroup>();
            _isInteractable = true;
        }

        private void Update()
        {
            // 호버 애니메이션
            Vector3 targetScale = _isHovered ? _originalScale * hoverScale : _originalScale;
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * animationSpeed);
        }

        /// <summary>
        /// 카드 초기화
        /// </summary>
        public void Setup(UpgradeBaseSO upgrade, int index)
        {
            _upgrade = upgrade;
            _cardIndex = index;

            if (upgrade == null)
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);

            // 이름 및 설명
            if (nameText != null)
            {
                nameText.text = upgrade.UpgradeName;
            }

            if (descriptionText != null)
            {
                descriptionText.text = upgrade.Description;
            }

            // 아이콘
            if (iconImage != null)
            {
                iconImage.sprite = upgrade.Icon;
                iconImage.gameObject.SetActive(upgrade.Icon != null);
            }

            // 희귀도 색상
            if (rarityBorder != null)
            {
                rarityBorder.color = GetRarityColor(upgrade.Rarity);
            }

            // 타입 표시
            if (typeText != null)
            {
                typeText.text = GetTypeText(upgrade.UpgradeType);
            }
        }

        private Color GetRarityColor(int rarity)
        {
            return rarity switch
            {
                0 => commonColor,
                1 => uncommonColor,
                2 => rareColor,
                3 => epicColor,
                4 => legendaryColor,
                _ => commonColor
            };
        }

        private string GetTypeText(UpgradeType type)
        {
            return type switch
            {
                UpgradeType.Movement => "행마법",
                UpgradeType.Stat => "스탯",
                UpgradeType.Ability => "능력",
                UpgradeType.Global => "전역",
                _ => "기타"
            };
        }

        private void OnSelectClicked()
        {
            if (!_isInteractable) return;
            OnCardSelected?.Invoke(_cardIndex);
        }

        /// <summary>
        /// 선택된 카드로 하이라이트하고 선택 애니메이션 재생
        /// </summary>
        public void HighlightAsChosen()
        {
            // 기존 트윈 정리
            transform.DOKill();
            rarityBorder?.DOColor(selectionGlowColor, selectionDuration / 2f).SetLoops(2, LoopType.Yoyo);

            // 선택 스케일 애니메이션: 빠르게 확대 후 원위치
            _selectionTween?.Kill();
            _selectionTween = transform
                .DOScale(_originalScale * selectionScale, selectionDuration)
                .SetEase(Ease.OutBack)
                .OnComplete(() => transform.DOScale(_originalScale, selectionDuration * 0.6f).SetEase(Ease.InBack));
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _isHovered = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isHovered = false;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            // 카드 전체 클릭을 선택 동작으로 연결
            OnSelectClicked();
        }

        public void SetInteractable(bool interactable)
        {
            // CanvasGroup가 있으면 상호작용/레이케스트 제어에 사용
            if (_canvasGroup != null)
            {
                _canvasGroup.interactable = interactable;
                _canvasGroup.blocksRaycasts = interactable;
            }
            _isInteractable = interactable;
        }
    }
}
