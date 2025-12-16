using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using LevelUpChess.Upgrades;
using LevelUpChess.Pieces;
using LevelUpChess.Managers;
using LevelUpChess.Core;
using LevelUpChess.Events;
using Unity.Netcode;

namespace LevelUpChess.Upgrades.UI
{
    /// <summary>
    /// 업그레이드 선택 패널 UI
    /// 레벨업 시 3개의 업그레이드 선택지를 표시
    /// </summary>
    public class UpgradeSelectionPanelUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI pieceNameText;
        [SerializeField] private Transform cardContainer;

        [Header("Card Prefab")]
        [SerializeField] private UpgradeCardUI cardPrefab;
        [SerializeField] private int maxCards = 3;

        [Header("Animation")]
        [SerializeField] private float fadeInDuration = 0.3f;
        [SerializeField] private float cardStaggerDelay = 0.1f;

        private ChessPiece _targetPiece;
        private bool _isVisible;
        public static UpgradeSelectionPanelUI Instance { get; private set; }
        private int _currentTileX = -1;
        private int _currentTileY = -1;
        private bool _canSelect = false;
        private ulong _ownerClientId = 0UL;
        private Team _targetTeam;
        private bool _isGlobalSelection = false;
        private UpgradeManager _upgradeManager;
        private List<UpgradeCardUI> _cards = new List<UpgradeCardUI>();

        private void Awake()
        {
            Instance = this;
            // 카드 풀 생성
            for (int i = 0; i < maxCards; i++)
            {
                if (cardPrefab == null) continue;

                var card = Instantiate(cardPrefab, cardContainer);

                // 보정: prefab의 크기나 pivot이 잘못되어 있는 경우 런타임에서 정렬/스케일을 초기화
                var rt = card.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.localScale = Vector3.one;
                    rt.localRotation = Quaternion.identity;
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.anchorMin = new Vector2(0.5f, 0.5f);
                    rt.anchorMax = new Vector2(0.5f, 0.5f);
                    rt.anchoredPosition = Vector2.zero;
                }

                card.gameObject.SetActive(false);
                int index = i;
                card.OnCardSelected += (cardIndex) => OnCardSelected(cardIndex);
                _cards.Add(card);
            }

            // 초기 상태 숨김
            Hide(immediate: true);
        }

        private void OnEnable()
        {
            Bus<GameOverEvent>.OnEvent += OnGameOver;
        }

        private void OnDisable()
        {
            Bus<GameOverEvent>.OnEvent -= OnGameOver;
        }

        private void OnGameOver(GameOverEvent eventData)
        {
            // 게임 종료 시 업그레이드 창 닫기 (리플레이 포함)
            Hide(immediate: true);
        }

        private void Start()
        {
            // UpgradeManager 이벤트 구독
            _upgradeManager = UpgradeManager.Instance;
            if (_upgradeManager != null)
            {
                _upgradeManager.OnUpgradeSelectionAvailable += OnSelectionAvailable;
                _upgradeManager.OnUpgradeApplied += OnUpgradeApplied;
                _upgradeManager.OnUpgradeSelectionCancelled += OnSelectionCancelled;
                Debug.Log("[UpgradeSelectionPanelUI] UpgradeManager 이벤트 구독 완료");
            }
            else
            {
                Debug.LogError("[UpgradeSelectionPanelUI] UpgradeManager를 찾을 수 없습니다! Instance가 null입니다.");
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (_upgradeManager != null)
            {
                _upgradeManager.OnUpgradeSelectionAvailable -= OnSelectionAvailable;
                _upgradeManager.OnUpgradeApplied -= OnUpgradeApplied;
                _upgradeManager.OnUpgradeSelectionCancelled -= OnSelectionCancelled;
            }
        }

        /// <summary>
        /// 업그레이드 선택지 표시
        /// </summary>
        private void OnSelectionAvailable(List<UpgradeBaseSO> upgrades, ChessPiece piece)
        {
            Debug.Log($"[UpgradeSelectionPanelUI] OnSelectionAvailable called! Piece: {piece?.name}, Upgrades: {upgrades?.Count}");
            
            _targetPiece = piece;
            _isGlobalSelection = false;
            
            // 타이틀 및 기물 이름 설정
            if (titleText != null)
            {
                titleText.text = "업그레이드 선택";
            }

            if (pieceNameText != null)
            {
                pieceNameText.text = piece != null ? $"{piece.name} 레벨업!" : "레벨업!";
            }

            // 카드 설정
            for (int i = 0; i < _cards.Count; i++)
            {
                if (i < upgrades.Count)
                {
                    _cards[i].Setup(upgrades[i], i);
                    _cards[i].SetInteractable(true);
                }
                else
                {
                    _cards[i].gameObject.SetActive(false);
                }
            }

            Show();
        }

        /// <summary>
        /// 글로벌 업그레이드 선택지 표시
        /// </summary>
        public void ShowGlobalUpgradeSelections(List<UpgradeBaseSO> upgrades, Team team)
        {
            Debug.Log($"[UpgradeSelectionPanelUI] ShowGlobalUpgradeSelections called! Team: {team}, Upgrades: {upgrades?.Count}");
            
            _targetTeam = team;
            _isGlobalSelection = true;
            
            // 타이틀 및 팀 이름 설정
            if (titleText != null)
            {
                titleText.text = "단체 강화 선택";
            }

            if (pieceNameText != null)
            {
                pieceNameText.text = $"{team}팀 단체 강화!";
            }

            // 카드 설정
            for (int i = 0; i < _cards.Count; i++)
            {
                if (i < upgrades.Count)
                {
                    _cards[i].Setup(upgrades[i], i);
                    _cards[i].SetInteractable(true);
                }
                else
                {
                    _cards[i].gameObject.SetActive(false);
                }
            }

            Show();
        }

        /// <summary>
        /// 글로벌 업그레이드 선택지 표시 (플레이어 레벨업용)
        /// </summary>
        public void ShowGlobalSelection(List<GlobalUpgradeSO> globalUpgrades, Team targetTeam)
        {
            if (globalUpgrades == null || globalUpgrades.Count == 0) return;

            _targetPiece = null;
            _targetTeam = targetTeam;
            _isGlobalSelection = true;

            // 타이틀 및 기물 이름 설정
            if (titleText != null)
            {
                titleText.text = "플레이어 레벨업!";
            }

            if (pieceNameText != null)
            {
                pieceNameText.text = $"{targetTeam} 팀 글로벌 업그레이드 선택";
            }

            // 카드 설정
            for (int i = 0; i < _cards.Count; i++)
            {
                if (i < globalUpgrades.Count)
                {
                    _cards[i].Setup(globalUpgrades[i], i);
                    _cards[i].SetInteractable(true);
                }
                else
                {
                    _cards[i].gameObject.SetActive(false);
                }
            }

            Show();
        }

        /// <summary>
        /// 카드 선택 처리
        /// </summary>
        private void OnCardSelected(int cardIndex)
        {
            if (!_canSelect) return; // 선택 권한이 없으면 무시

            // 모든 카드 비활성화 (중복 클릭 방지)
            foreach (var card in _cards)
            {
                card.SetInteractable(false);
            }

            _canSelect = false; // disable further selection locally to prevent double clicks

            // 요청: 서버에 선택 전송
            if (UpgradeManager.Instance != null)
            {
                if (_isGlobalSelection)
                {
                    // 글로벌 업그레이드 선택
                    UpgradeManager.Instance.ApplyGlobalUpgrade(_cards[cardIndex].Upgrade, _targetTeam);
                }
                else
                {
                    // 일반 업그레이드 선택
                    UpgradeManager.Instance.SelectUpgradeServerRpc(cardIndex, _currentTileX, _currentTileY);
                }
            }
        }

        /// <summary>
        /// 업그레이드 적용 완료
        /// </summary>
        private void OnUpgradeApplied(UpgradeBaseSO upgrade, ChessPiece piece)
        {
            // 선택 완료 후 패널 숨김
            Hide();
        }

        /// <summary>
        /// 선택 취소
        /// </summary>
        private void OnSelectionCancelled()
        {
            Hide();
        }

        /// <summary>
        /// 스킵 버튼 클릭
        /// </summary>
        private void OnSkipClicked()
        {
            _upgradeManager?.CancelSelection();
            Hide();
        }

        /// <summary>
        /// 닫기 버튼 클릭
        /// </summary>
        private void OnCloseClicked()
        {
            _upgradeManager?.CancelSelection();
            Hide();
        }

        /// <summary>
        /// 패널 표시
        /// </summary>
        public void Show()
        {
            if (_isVisible) return;
            _isVisible = true;

            panelRoot.SetActive(true);
            
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
                StartCoroutine(FadeIn());
            }

            // 카드 등장 애니메이션
            StartCoroutine(AnimateCardsIn());

            // 게임 일시정지 (선택적)
            // Time.timeScale = 0f;
        }

        /// <summary>
        /// 서버에서 전달된 옵션으로 UI를 표시합니다.
        /// </summary>
        public void ShowWithOptions(int[] upgradeIndices, int tileX, int tileY, int ownerTeam)
        {
            _currentTileX = tileX;
            _currentTileY = tileY;

            var netGameMgr = ServiceLocator.Get<NetworkGameManager>();
            _canSelect = netGameMgr != null && netGameMgr.LocalPlayerTeam == (LevelUpChess.Pieces.Team)ownerTeam;
            _ownerClientId = netGameMgr != null && netGameMgr.LocalPlayerTeam == (LevelUpChess.Pieces.Team)ownerTeam ? NetworkManager.Singleton.LocalClientId : ulong.MaxValue;

            var upgradeMgr = UpgradeManager.Instance;
            List<UpgradeBaseSO> upgrades = new List<UpgradeBaseSO>();
            if (upgradeMgr != null)
            {
                foreach (var idx in upgradeIndices)
                {
                    var up = upgradeMgr.GetUpgradeByIndex(idx);
                    upgrades.Add(up);
                }
            }

            // 좌표 기준으로 기물을 찾아서 UI에 전달합니다. 기물이 아직 없으면 잠깐 대기합니다.
            var coord = new UnityEngine.Vector2Int(tileX, tileY);
            var boardMgr = FindFirstObjectByType<LevelUpChess.Board.BoardManager>();
            ChessPiece piece = null;

            if (boardMgr != null)
            {
                piece = boardMgr.GetPieceAt(coord);
            }

            if (piece != null)
            {
                OnSelectionAvailable(upgrades, piece);
            }
            else
            {
                // 기물이 아직 생성되지 않았을 수 있으므로 최대 2초 동안 폴링
                StartCoroutine(WaitForPieceAndShow(upgrades, coord, 2f, ownerTeam));
            }

            // 선택 권한 결정: NetworkGameManager의 LocalPlayerTeam과 비교
            var netGameMgrRef = ServiceLocator.Get<NetworkGameManager>();
            if (netGameMgrRef != null)
            {
                _canSelect = netGameMgrRef.LocalPlayerTeam == (Team)ownerTeam;
            }
            else
            {
                _canSelect = false;
            }

            if (!_canSelect)
            {
                foreach (var card in _cards)
                    card.SetInteractable(false);
            }
        }

        /// <summary>
        /// 서버가 모든 클라이언트에 선택 결과를 알렸을 때 호출
        /// </summary>
        public void OnSelectionMade(int optionIndex, int tileX, int tileY, ulong chosenClientId)
        {
            // 해당 타일의 선택과 일치하면 처리
            if (tileX != _currentTileX || tileY != _currentTileY) return;

            // 비선택자는 어떤 항목이 선택되었는지 하이라이트
            for (int i = 0; i < _cards.Count; i++)
            {
                if (i == optionIndex)
                {
                    _cards[i].HighlightAsChosen();
                }
                else
                {
                    _cards[i].SetInteractable(false);
                }
            }

            // 선택자라면 즉시 패널 닫기
            if (NetworkManager.Singleton.LocalClientId == chosenClientId)
            {
                Hide();
                return;
            }

            // 비선택자는 하이라이트 애니메이션이 끝난 뒤 패널을 닫도록 함
            StartCoroutine(HideAfterDelay(0.8f));
        }

        private System.Collections.IEnumerator HideAfterDelay(float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            Hide();
        }

        /// <summary>
        /// 패널 숨김
        /// </summary>
        public void Hide(bool immediate = false)
        {
            if (!_isVisible && !immediate) return;
            _isVisible = false;

            if (immediate)
            {
                panelRoot.SetActive(false);
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = 0f;
                    canvasGroup.interactable = false;
                    canvasGroup.blocksRaycasts = false;
                }
            }
            else
            {
                if (canvasGroup != null)
                {
                    StartCoroutine(FadeOut());
                }
                else
                {
                    panelRoot.SetActive(false);
                }
            }

            // 게임 재개 (선택적)
            // Time.timeScale = 1f;

            _targetPiece = null;
        }

        private System.Collections.IEnumerator FadeIn()
        {
            float elapsed = 0f;
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeInDuration);
                yield return null;
            }
            canvasGroup.alpha = 1f;
        }

        private System.Collections.IEnumerator FadeOut()
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            float elapsed = 0f;
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeInDuration);
                yield return null;
            }
            canvasGroup.alpha = 0f;
            panelRoot.SetActive(false);
        }


        private System.Collections.IEnumerator AnimateCardsIn()
        {
            foreach (var card in _cards)
            {
                if (card.gameObject.activeSelf)
                {
                    // 초기 상태
                    card.transform.localScale = Vector3.zero;
                }
            }

            foreach (var card in _cards)
            {
                if (card.gameObject.activeSelf)
                {
                    // 스케일 애니메이션
                    StartCoroutine(ScaleCard(card.transform));
                    yield return new WaitForSecondsRealtime(cardStaggerDelay);
                }
            }
        }

        private System.Collections.IEnumerator ScaleCard(Transform cardTransform)
        {
            float elapsed = 0f;
            float duration = 0.2f;
            
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;
                // 바운스 효과
                float scale = Mathf.Sin(t * Mathf.PI * 0.5f) * 1.1f;
                if (t > 0.5f)
                {
                    scale = 1f + (1.1f - 1f) * (1f - (t - 0.5f) * 2f);
                }
                cardTransform.localScale = Vector3.one * Mathf.Min(scale, 1f);
                yield return null;
            }
            cardTransform.localScale = Vector3.one;
        }

        /// <summary>
        /// 외부에서 패널 토글
        /// </summary>
        public void Toggle()
        {
            if (_isVisible)
            {
                Hide();
            }
            else
            {
                Show();
            }
        }

        private System.Collections.IEnumerator WaitForPieceAndShow(List<UpgradeBaseSO> upgrades, UnityEngine.Vector2Int coord, float timeout, int ownerTeam)
        {
            float elapsed = 0f;
            LevelUpChess.Pieces.ChessPiece piece = null;
            var boardMgr = FindFirstObjectByType<LevelUpChess.Board.BoardManager>();

            while (elapsed < timeout)
            {
                if (boardMgr != null)
                {
                    piece = boardMgr.GetPieceAt(coord);
                    if (piece != null) break;
                }

                yield return new WaitForSecondsRealtime(0.1f);
                elapsed += 0.1f;
            }

            if (piece != null)
            {
                OnSelectionAvailable(upgrades, piece);
            }
            else
            {
                Debug.LogWarning($"[UpgradeSelectionPanelUI] Piece not found at {coord} after waiting; showing generic UI.");
                OnSelectionAvailable(upgrades, null);
            }

            // 선택 권한 판정
            var netGameMgr = ServiceLocator.Get<NetworkGameManager>();
            if (netGameMgr != null)
            {
                _canSelect = netGameMgr.LocalPlayerTeam == (Team)ownerTeam;
            }
            else
            {
                _canSelect = false;
            }

            if (!_canSelect)
            {
                foreach (var card in _cards)
                    card.SetInteractable(false);
            }
        }

        /// <summary>
        /// 현재 표시 상태
        /// </summary>
        public bool IsVisible => _isVisible;
    }
}
