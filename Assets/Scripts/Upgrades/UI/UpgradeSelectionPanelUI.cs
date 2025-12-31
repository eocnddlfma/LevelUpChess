using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using LevelUpChess.Core;
using LevelUpChess.Upgrades;
using LevelUpChess.Pieces;
using LevelUpChess.Managers;
using LevelUpChess.Events;
using LevelUpChess.UI;
using Unity.Netcode;

namespace LevelUpChess.Upgrades.UI
{
    public class UpgradeSelectionPanelUI : MonoBehaviour
    {
        private bool _isShowing = false;
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
        [SerializeField] private float pieceWaitTimeout = 2f;

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
        private Queue<IEvent> _eventQueue = new Queue<IEvent>();
        private bool _isProcessing = false;
        private IEvent _currentProcessingEvent = null;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this.gameObject);
                return;
            }
            Instance = this;
            InitializeCardPool();
        }

        private void InitializeCardPool()
        {
            for (int i = 0; i < maxCards; i++)
            {
                if (cardPrefab == null) continue;
                var card = Instantiate(cardPrefab, cardContainer);
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
                var cardUI = card.GetComponent<UpgradeCardUI>();
                if (cardUI == null)
                {
                    Destroy(card);
                    continue;
                }
                card.gameObject.SetActive(false);
                cardUI.OnCardSelected += (cardIndex) => OnCardSelected(cardIndex);
                _cards.Add(cardUI);
            }
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
            Hide(immediate: true);
        }

        private void Start()
        {
            _upgradeManager = UpgradeManager.Instance;
            if (_upgradeManager != null)
            {
                _upgradeManager.OnUpgradeSelectionAvailable += OnSelectionAvailable;
                _upgradeManager.OnUpgradeApplied += OnUpgradeApplied;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (_upgradeManager != null)
            {
                _upgradeManager.OnUpgradeSelectionAvailable -= OnSelectionAvailable;
                _upgradeManager.OnUpgradeApplied -= OnUpgradeApplied;
            }
        
        }

        private void SetupCards(List<UpgradeBaseSO> upgrades)
        {
            for (int i = 0; i < _cards.Count; i++)
            {
                if (i < upgrades.Count)
                {
                    _cards[i].Setup(upgrades[i], i);
                    _cards[i].SetInteractable(_canSelect);
                    if (!_cards[i].gameObject.activeSelf) _cards[i].gameObject.SetActive(true);
                }
                else
                {
                    _cards[i].gameObject.SetActive(false);
                }
            }
        }
        public void ShowGlobalUpgradeSelections(List<UpgradeBaseSO> upgrades, Team team)
        {
            _targetTeam = team;
            _isGlobalSelection = true;
            
            if (titleText != null) titleText.text = "단체 강화 선택";
            if (pieceNameText != null) pieceNameText.text = $"{(team == Team.White ? "백팀" : "흑팀")} 단체 강화!";

            SetupCards(upgrades);
            Show();
        }
        private void OnCardSelected(int cardIndex)
        {
            foreach (var card in _cards) card.SetInteractable(false);
            _canSelect = false;
            StartCardSelectionAnimation(cardIndex);
            SendUpgradeSelectionToServer(cardIndex);
        }

        private void StartCardSelectionAnimation(int cardIndex)
        {
            for (int i = 0; i < _cards.Count; i++)
            {
                bool isSelected = (i == cardIndex);
                _cards[i].StartSelectionAnimation(isSelected, isSelected ? (System.Action)(() => Hide()) : () => {}, () => {});
            }
        }

        /// <summary>
        /// 서버에 업그레이드 선택 전송
        /// </summary>
        private void SendUpgradeSelectionToServer(int cardIndex)
        {
            if (UpgradeManager.Instance == null) return;

            if (_isGlobalSelection)
            {
                // 글로벌 업그레이드 선택
                var upgradeIndex = UpgradeManager.Instance.GetUpgradeIndex(_cards[cardIndex].Upgrade);
                UpgradeManager.Instance.ApplyGlobalUpgradeServerRpc(cardIndex, upgradeIndex, _targetTeam);
            }
            else
            {
                // 일반 업그레이드 선택
                UpgradeManager.Instance.SelectUpgradeServerRpc(cardIndex, _currentTileX, _currentTileY);
            }
        }

        private void OnUpgradeApplied(UpgradeBaseSO upgrade, ChessPiece piece)
        {
        }

        private void OnSelectionAvailable(List<UpgradeBaseSO> upgrades, ChessPiece piece)
        {
            if (upgrades == null || upgrades.Count == 0) return;

            var upgradeManager = UpgradeManager.Instance;
            if (upgradeManager == null) return;

            var indices = new int[upgrades.Count];
            for (int i = 0; i < upgrades.Count; i++)
            {
                indices[i] = upgradeManager.GetUpgradeIndex(upgrades[i]);
            }

            if (piece == null)
            {
                // 글로벌 업그레이드
                ShowGlobalUpgradeSelections(upgrades, Team.White); // 팀은 임시로 White, 필요시 조정
            }
            else
            {
                // 피스 업그레이드
                var coord = piece.CurrentTile?.coordinate ?? Vector2Int.zero;
                ShowWithOptions(indices, coord.x, coord.y, (int)piece.Team);
            }
        }

        public void Show()
        {
            if (_isShowing || _isVisible) return;
            
            if (panelRoot == null)
            {
                Debug.LogError("[UpgradeSelectionPanelUI] panelRoot is null! Cannot show panel.");
                return;
            }
            
            _isVisible = true;
            _isShowing = true;

            panelRoot.SetActive(true);
            
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }
        }

        public void ShowWithOptions(int[] upgradeIndices, int tileX, int tileY, int ownerTeam, bool isGlobal = false)
        {
            _currentTileX = tileX;
            _currentTileY = tileY;
            DetermineSelectionPermission(ownerTeam);
            var upgrades = LoadUpgradesFromIndices(upgradeIndices);
            ShowUpgradeSelections(upgrades, tileX, tileY, ownerTeam, isGlobal);
        }

        private void DetermineSelectionPermission(int ownerTeam)
        {
            var netGameMgr = ServiceLocator.Get<NetworkGameManager>();
            if (netGameMgr != null)
            {
                _canSelect = netGameMgr.LocalPlayerTeam == (Team)ownerTeam;
                _ownerClientId = _canSelect ? NetworkManager.Singleton.LocalClientId : ulong.MaxValue;
            }
            else
            {
                _canSelect = false;
                _ownerClientId = ulong.MaxValue;
            }
        }

        private List<UpgradeBaseSO> LoadUpgradesFromIndices(int[] upgradeIndices)
        {
            var upgradeMgr = UpgradeManager.Instance;
            var upgrades = new List<UpgradeBaseSO>();
            if (upgradeMgr != null)
            {
                foreach (var idx in upgradeIndices) upgrades.Add(upgradeMgr.GetUpgradeByIndex(idx));
            }
            return upgrades;
        }

        private void ShowUpgradeSelections(List<UpgradeBaseSO> upgrades, int tileX, int tileY, int ownerTeam, bool isGlobal)
        {
            var coord = new Vector2Int(tileX, tileY);
            bool isGlobalUpgrade = isGlobal || (coord == Vector2Int.zero);
            if (isGlobalUpgrade)
            {
                ShowGlobalUpgradeSelections(upgrades, (Team)ownerTeam);
            }
            else
            {
                var boardMgr = FindFirstObjectByType<Board.BoardManager>();
                ChessPiece piece = null;
                if (boardMgr != null) piece = boardMgr.GetPieceAt(coord);
                if (piece != null)
                {
                    OnSelectionAvailable(upgrades, piece);
                }
                else
                {
                    StartCoroutine(WaitForPieceAndShow(upgrades, coord, pieceWaitTimeout, ownerTeam));
                }
            }
        }

        public void OnSelectionMade(int optionIndex, int tileX, int tileY, ulong chosenClientId)
        {
            if (tileX != _currentTileX || tileY != _currentTileY) return;

            for (int i = 0; i < _cards.Count; i++)
            {
                if (i == optionIndex) _cards[i].HighlightAsChosen();
                else _cards[i].SetInteractable(false);
            }

            if (NetworkManager.Singleton.LocalClientId == chosenClientId && !_isGlobalSelection) Hide();
            else StartCoroutine(HideAfterDelay(0.8f));
        }

        private IEnumerator HideAfterDelay(float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            Hide();
        }

        public void Hide(bool immediate = false)
        {
            if (!_isVisible && !immediate) return;
            
            if (panelRoot == null)
            {
                _isVisible = false;
                _isShowing = false;
                ProcessNextEvent();
                return;
            }
            
            _isVisible = false;
            _isShowing = false;

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
                panelRoot.SetActive(false);
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = 0f;
                    canvasGroup.interactable = false;
                    canvasGroup.blocksRaycasts = false;
                }
            }

            _targetPiece = null;
            _currentProcessingEvent = null;
            ProcessNextEvent();
        }

        public static void Enqueue(IEvent evt)
        {
            if (Instance == null) return;
            Instance._eventQueue.Enqueue(evt);
            if (!Instance._isProcessing) Instance.ProcessNextEvent();
        }

        private void ProcessNextEvent()
        {
            if (_eventQueue.Count == 0)
            {
                _isProcessing = false;
                _currentProcessingEvent = null;
                return;
            }

            if (!_isProcessing)
            {
                _isProcessing = true;
                StartCoroutine(ProcessEventCoroutine());
            }
        }

        private IEnumerator ProcessEventCoroutine()
        {
            while (_eventQueue.Count > 0)
            {
                IEvent currentEvent = _eventQueue.Dequeue();
                _currentProcessingEvent = currentEvent;
                yield return StartCoroutine(ProcessEvent(currentEvent));
            }

            _isProcessing = false;
            _currentProcessingEvent = null;
        }

        /// <summary>
        /// 개별 이벤트 처리
        /// </summary>
        private IEnumerator ProcessEvent(IEvent currentEvent)
        {
            if (currentEvent is PieceLevelUpEvent pieceLevelUpEvent)
            {
                yield return StartCoroutine(ProcessPieceLevelUpEvent(pieceLevelUpEvent));
            }
            else if (currentEvent is PlayerLevelUpEvent playerLevelUpEvent)
            {
                yield return StartCoroutine(ProcessPlayerLevelUpEvent(playerLevelUpEvent));
            }
            else if (currentEvent is ShowMessageEvent messageEvent)
            {
                yield return StartCoroutine(ProcessShowMessageEvent(messageEvent));
            }
            else
            {
                // 알 수 없는 이벤트는 무시하고 다음으로
                Debug.LogWarning($"[UpgradeSelectionPanelUI] Unknown event type: {currentEvent.GetType().Name}. Skipping.");
            }
        }
        private IEnumerator WaitForUIDisplay(float timeout, string eventName)
        {
            float elapsed = 0f;
            while (elapsed < timeout && !_isVisible)
            {
                yield return new WaitForSecondsRealtime(0.1f);
                elapsed += 0.1f;
            }
            if (!_isVisible)
            {
                Debug.LogWarning($"[UpgradeSelectionPanelUI] UI not displayed after {timeout}s for {eventName}. Proceeding to next event.");
            }
        }
        private IEnumerator ProcessPlayerLevelUpEvent(PlayerLevelUpEvent playerLevelUpEvent)
        {
            var upgradeManager = UpgradeManager.Instance;
            if (upgradeManager != null)
            {
                upgradeManager.OfferUpgradeSelection(playerLevelUpEvent);
                yield return StartCoroutine(WaitForUIDisplay(3f, "PlayerLevelUpEvent"));
            }
            else
            {
                Bus<PlayerLevelUpEvent>.Raise(playerLevelUpEvent);
            }
        }
        private IEnumerator ProcessPieceLevelUpEvent(PieceLevelUpEvent pieceLevelUpEvent)
        {
            var upgradeManager = UpgradeManager.Instance;
            if (upgradeManager != null && pieceLevelUpEvent.Piece != null)
            {
                upgradeManager.ClientCreateAndBroadcastSelections(pieceLevelUpEvent.Piece);
                yield return StartCoroutine(WaitForUIDisplay(3f, "PieceLevelUpEvent"));
            }
            else
            {
                Bus<PieceLevelUpEvent>.Raise(pieceLevelUpEvent);
            }
        }
        private IEnumerator ProcessShowMessageEvent(ShowMessageEvent messageEvent)
        {
            var messageUI = ServiceLocator.Get<GameMessageUI>();
            if (messageUI != null)
            {
                bool messageCompleted = false;
                messageUI.ShowMessage(messageEvent.Message, 2f, () =>
                {
                    Bus<ShowMessageEvent>.Raise(messageEvent);
                    messageCompleted = true;
                });
                
                while (!messageCompleted) yield return null;
            }
            else
            {
                Bus<ShowMessageEvent>.Raise(messageEvent);
            }
        }
        /// <summary>
        /// 이벤트 세부 정보를 문자열로 반환 (로그용)
        /// </summary>










        public void Toggle()
        {
            if (_isVisible) Hide();
            else Show();
        }

        private IEnumerator WaitForPieceAndShow(List<UpgradeBaseSO> upgrades, Vector2Int coord, float timeout, int ownerTeam)
        {
            float elapsed = 0f;
            ChessPiece piece = null;
            var boardMgr = FindFirstObjectByType<Board.BoardManager>();
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
            if (piece != null) OnSelectionAvailable(upgrades, piece);
            else OnSelectionAvailable(upgrades, null);
        }

        public bool IsVisible => _isVisible;


    }
}
