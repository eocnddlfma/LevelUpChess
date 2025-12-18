using UnityEngine;
using Unity.Netcode;
using System;
using System.Collections.Generic;
using System.Linq;
using LevelUpChess.Upgrades;
using LevelUpChess.Upgrades.UI;
using LevelUpChess.Pieces;
using LevelUpChess.Events;
using LevelUpChess.Board;
using LevelUpChess.Managers;
using LevelUpChess.Core;

namespace LevelUpChess.Upgrades
{
    /// <summary>
    /// 업그레이드 시스템 매니저
    /// 레벨업 시 업그레이드 선택, 적용 및 네트워크 동기화 담당
    /// NetworkBehaviour로 작동하며 프리팹으로 생성하여 NetworkManager에 등록 필요
    /// </summary>
    public class UpgradeManager : NetworkBehaviour
    {
        [Header("Settings")]
        [SerializeField] private UpgradePoolSO upgradePool;
        [SerializeField] private int upgradesPerSelection = 3;
        [SerializeField] private bool allowDuplicates = false;
        
        [Header("Rarity Settings")]
        [SerializeField] private int baseMaxRarity = 1;
        [SerializeField] private int rarityIncreasePerLevel = 1;
        [SerializeField] private int maxRarityLevel = 5;

        [Header("References")]
        [SerializeField] private BoardManager boardManager;

        // 각 플레이어의 적용된 업그레이드 추적
        private Dictionary<ulong, List<string>> _playerAppliedUpgrades = new Dictionary<ulong, List<string>>();
        
        // 각 기물의 적용된 업그레이드 추적
        private Dictionary<int, List<string>> _pieceAppliedUpgrades = new Dictionary<int, List<string>>();
        
        // 글로벌 업그레이드 중복 방지
        private HashSet<string> appliedGlobalUpgrades = new HashSet<string>();

        // 현재 선택 대기 중인 업그레이드
        private List<UpgradeBaseSO> _pendingSelections = new List<UpgradeBaseSO>();
        private ChessPiece _pendingTargetPiece;
        private ulong _pendingPlayerId;

        // 네트워크 권한의 대기 중인 선택 (서버에서 관리)
        private class PendingSelection
        {
            public int[] Options;
            public Team OwnerTeam;
            public float Deadline;
            public bool Chosen;
            public int ChosenIndex = -1;
            public ulong ChosenBy;

            // Server-side instance id to help recovery when tile occupant changed
            public int TargetPieceInstanceId = 0;
        }

        private readonly System.Collections.Generic.Dictionary<UnityEngine.Vector2Int, PendingSelection> _pendingNetworkSelections = new();

        // 이벤트
        public event Action<List<UpgradeBaseSO>, ChessPiece> OnUpgradeSelectionAvailable;
        public event Action<UpgradeBaseSO, ChessPiece> OnUpgradeApplied;
        public event Action OnUpgradeSelectionCancelled;

        public static UpgradeManager Instance { get; private set; }

        private void Awake()
        {
            Debug.Log("[UpgradeManager] Awake called");
            
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                Debug.Log("[UpgradeManager] Instance set successfully");
            }
            else
            {
                Debug.Log("[UpgradeManager] Duplicate instance, destroying");
                Destroy(gameObject);
                return;
            }

            // UpgradePool 자동 로드
            if (upgradePool == null)
            {
                AutoLoadUpgradePool();
            }
        }

        private void OnEnable()
        {
            Bus<PlayerLevelUpEvent>.OnEvent += OnPlayerLevelUp;
            Bus<AttackSuccessEvent>.OnEvent += OnAttackSuccess;
        }

        private void OnDisable()
        {
            Bus<PlayerLevelUpEvent>.OnEvent -= OnPlayerLevelUp;
            Bus<AttackSuccessEvent>.OnEvent -= OnAttackSuccess;
        }

        private void OnPlayerLevelUp(PlayerLevelUpEvent eventData)
        {
            // 플레이어가 레벨업하면 업그레이드 선택 창 표시
            StartCoroutine(ShowUpgradeSelectionForPlayer(eventData));
        }

        private void OnAttackSuccess(AttackSuccessEvent eventData)
        {
            if (!IsServer) return;  // 서버에서만 처리

            // 상대 클라이언트에 공격 결과 알림 (ClientRpc)
            NotifyAttackResultClientRpc(eventData.Attacker.CurrentTile.coordinate.x, eventData.Attacker.CurrentTile.coordinate.y,
                                        eventData.Target.CurrentTile.coordinate.x, eventData.Target.CurrentTile.coordinate.y,
                                        eventData.DamageDealt, eventData.TargetDied);
        }

        private System.Collections.IEnumerator ShowUpgradeSelectionForPlayer(PlayerLevelUpEvent eventData)
        {
            // 잠시 기다렸다가 업그레이드 선택 창 표시 (UI가 준비될 시간)
            yield return new UnityEngine.WaitForSeconds(0.5f);
            
            // 해당 팀의 플레이어에게 업그레이드 선택 제공
            OfferUpgradeSelection(eventData);
        }

        private void OfferUpgradeSelection(PlayerLevelUpEvent eventData)
        {
            Team team = eventData.Team;
            int playerLevel = eventData.NewLevel;

            if (upgradePool == null)
            {
                Debug.LogError("[UpgradeManager] UpgradePool이 설정되지 않았습니다.");
                return;
            }

            // 플레이어 레벨업 시 글로벌 업그레이드만 제공
            // 적용된 글로벌 업그레이드 ID 목록 (중복 방지)
            var excludedIds = allowDuplicates ? null : appliedGlobalUpgrades.ToList();

            // 플레이어 레벨에 따른 최대 희귀도 계산
            int maxRarity = Mathf.Min(baseMaxRarity + (playerLevel - 1) * rarityIncreasePerLevel, maxRarityLevel);

            // 글로벌 업그레이드 중 사용 가능한 것 필터링
            var availableUpgrades = upgradePool.GlobalUpgrades.ToList();

            if (availableUpgrades.Count == 0)
            {
                Debug.LogWarning($"[UpgradeManager] {team} 플레이어에 적용 가능한 글로벌 업그레이드가 없습니다.");
                return;
            }

            // 랜덤으로 업그레이드 선택
            var selectedUpgrades = new List<UpgradeBaseSO>();
            var tempList = new List<UpgradeBaseSO>(availableUpgrades);
            for (int i = 0; i < Mathf.Min(upgradesPerSelection, tempList.Count); i++)
            {
                int randomIndex = UnityEngine.Random.Range(0, tempList.Count);
                selectedUpgrades.Add(tempList[randomIndex]);
                tempList.RemoveAt(randomIndex);
            }

            var indices = new System.Collections.Generic.List<int>();
            foreach (var upgrade in selectedUpgrades)
            {
                indices.Add(upgradePool.GetUpgradeIndex(upgrade));
            }

            // 플레이어 업그레이드이므로 타일 좌표는 (0,0)으로 설정
            var coord = UnityEngine.Vector2Int.zero;

            var pending = new PendingSelection
            {
                Options = indices.ToArray(),
                OwnerTeam = team,
                Deadline = UnityEngine.Time.time + 15f,
                Chosen = false,
                TargetPieceInstanceId = 0  // 글로벌 업그레이드이므로 피스 없음
            };

            _pendingNetworkSelections[coord] = pending;

            // 모든 클라이언트에 동일한 옵션 전송
            ShowSelectionClientRpc(pending.Options, coord.x, coord.y, (int)pending.OwnerTeam);

            // 선택 완료까지 대기 (코루틴이나 이벤트로 처리)
        }

        private void AutoLoadUpgradePool()
        {
            // 1. Resources 폴더에서 시도
            upgradePool = Resources.Load<UpgradePoolSO>("MainUpgradePool");
            
            if (upgradePool == null)
            {
                // 2. AssetDatabase에서 검색 (에디터에서만)
#if UNITY_EDITOR
                string[] guids = UnityEditor.AssetDatabase.FindAssets("t:UpgradePoolSO MainUpgradePool");
                if (guids.Length > 0)
                {
                    string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                    upgradePool = UnityEditor.AssetDatabase.LoadAssetAtPath<UpgradePoolSO>(path);
                    Debug.Log($"[UpgradeManager] UpgradePool 자동 로드 성공: {path}");
                }
                else
                {
                    Debug.LogError("[UpgradeManager] MainUpgradePool을 찾을 수 없습니다! Tools > LevelUpChess > Auto Generate Upgrades를 실행하세요.");
                }
#else
                Debug.LogError("[UpgradeManager] MainUpgradePool을 찾을 수 없습니다! Resources 폴더에 배치되어야 합니다.");
#endif
            }
            else
            {
                Debug.Log("[UpgradeManager] UpgradePool 자동 로드 성공 (Resources)");
            }
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            // 레벨업 이벤트 구독
            Bus<PieceLevelUpEvent>.OnEvent += OnPieceLevelUp;

            // Custom message handler 등록
            NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler("ShowUpgradeSelection", OnShowUpgradeSelectionMessage);
            
            Debug.Log($"[UpgradeManager] 네트워크 스폰 완료. IsServer: {IsServer}, IsClient: {IsClient}, upgradePool: {(upgradePool != null ? "OK" : "NULL")}");
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            
            // 이벤트 구독 해제
            Bus<PieceLevelUpEvent>.OnEvent -= OnPieceLevelUp;

            // Custom message handler 해제
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.CustomMessagingManager.UnregisterNamedMessageHandler("ShowUpgradeSelection");
            }
        }

        // 서버에서 호출: 기물이 레벨업하면 서버가 업그레이드 옵션을 생성하고 클라이언트에 전송
        private void CreateSelectionsForPieceServer(ChessPiece piece)
        {
            if (!IsServer)
                return;

            if (piece == null || !piece.IsAlive || piece.CurrentTile == null)
            {
                Debug.LogWarning($"[UpgradeManager] Cannot create selections for piece {piece?.name}: not alive or no current tile.");
                return;
            }

            if (upgradePool == null)
            {
                Debug.LogError("[UpgradeManager] Cannot create selections: upgradePool is null.");
                return;
            }

            // 적용된 업그레이드 ID 목록 (서버에서도 동일하게 사용)
            var excludedIds = allowDuplicates ? null : GetAppliedUpgrades(piece);

            int currentLevel = piece.GetComponent<PieceCombat>()?.Level ?? 1;
            int maxRarity = Mathf.Min(baseMaxRarity + (currentLevel - 1) * rarityIncreasePerLevel, maxRarityLevel);

            var drawResults = upgradePool.DrawUpgrades(piece, upgradesPerSelection, excludedIds, maxRarity);
            if (drawResults.Count == 0)
            {
                Debug.LogWarning($"[UpgradeManager] {piece.name}에 적용 가능한 업그레이드가 없습니다.");
                return;
            }

            var indices = new System.Collections.Generic.List<int>();
            foreach (var dr in drawResults)
            {
                indices.Add(upgradePool.GetUpgradeIndex(dr.Upgrade));
            }

            var coord = piece.CurrentTile?.coordinate ?? UnityEngine.Vector2Int.zero;

            var pending = new PendingSelection
            {
                Options = indices.ToArray(),
                OwnerTeam = piece.Team,
                Deadline = UnityEngine.Time.time + 15f,
                Chosen = false,
                TargetPieceInstanceId = piece.GetInstanceID()
            };

            _pendingNetworkSelections[coord] = pending;

            // 모든 클라이언트에 동일한 옵션 전송 (ownerTeam으로 전송)
            ShowSelectionClientRpc(pending.Options, coord.x, coord.y, (int)pending.OwnerTeam);
        }

        /// <summary>
        /// 클라이언트에서 로컬로 옵션을 생성하고 바로 UI를 보여준 뒤 서버에 브로드캐스트 요청을 보냅니다.
        /// 보안 검증은 생략(사용자 요청)
        /// </summary>
        public void ClientCreateAndBroadcastSelections(ChessPiece piece)
        {
            if (IsServer) return; // 서버에서는 사용하지 않음

            if (upgradePool == null)
            {
                Debug.LogError("[UpgradeManager] Cannot create selections: upgradePool is null.");
                return;
            }

            var excludedIds = allowDuplicates ? null : GetAppliedUpgrades(piece);
            int currentLevel = piece.GetComponent<PieceCombat>()?.Level ?? 1;
            int maxRarity = Mathf.Min(baseMaxRarity + (currentLevel - 1) * rarityIncreasePerLevel, maxRarityLevel);

            var drawResults = upgradePool.DrawUpgrades(piece, upgradesPerSelection, excludedIds, maxRarity);
            if (drawResults.Count == 0)
            {
                Debug.LogWarning($"[UpgradeManager] {piece.name}에 적용 가능한 업그레이드가 없습니다.");
                return;
            }

            var indices = new System.Collections.Generic.List<int>();
            foreach (var dr in drawResults)
            {
                indices.Add(upgradePool.GetUpgradeIndex(dr.Upgrade));
            }

            var coord = piece.CurrentTile?.coordinate ?? UnityEngine.Vector2Int.zero;

            // 로컬에서 즉시 UI 표시
            var ui = UnityEngine.Object.FindFirstObjectByType<LevelUpChess.Upgrades.UI.UpgradeSelectionPanelUI>();
            if (ui != null)
            {
                ui.ShowWithOptions(indices.ToArray(), coord.x, coord.y, (int)piece.Team);
            }

            // 서버에 브로드캐스트 요청 (서버는 이를 받아 다른 클라이언트에 전파)
            BroadcastSelectionsServerRpc(indices.ToArray(), coord.x, coord.y, (int)piece.Team);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void BroadcastSelectionsServerRpc(int[] upgradeIndices, int tileX, int tileY, int ownerTeam, RpcParams rpcParams = default)
        {
            // 서버는 pending을 저장하고 모든 클라이언트에 전파
            var coord = new UnityEngine.Vector2Int(tileX, tileY);

            var pending = new PendingSelection
            {
                Options = upgradeIndices,
                OwnerTeam = (Team)ownerTeam,
                Deadline = UnityEngine.Time.time + 15f,
                Chosen = false
            };

            _pendingNetworkSelections[coord] = pending;

            ShowSelectionClientRpc(pending.Options, coord.x, coord.y, (int)pending.OwnerTeam);
        }

        [ClientRpc]
        private void ShowSelectionClientRpc(int[] upgradeIndices, int tileX, int tileY, int ownerTeam)
        {
            // 클라이언트 측: UI 표시
            var ui = FindFirstObjectByType<UpgradeSelectionPanelUI>();
            if (ui != null)
            {
                ui.ShowWithOptions(upgradeIndices, tileX, tileY, ownerTeam);
            }
            else
            {
                Debug.LogWarning("[UpgradeManager] UpgradeSelectionPanelUI not found on client.");
            }
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        public void SelectUpgradeServerRpc(int optionIndex, int tileX, int tileY, RpcParams rpcParams = default)
        {
            var coord = new UnityEngine.Vector2Int(tileX, tileY);
            if (!_pendingNetworkSelections.TryGetValue(coord, out var pending))
                return;

            Debug.Log($"[UpgradeManager] SelectUpgradeServerRpc called by {rpcParams.Receive.SenderClientId} for coord {coord} (OwnerTeam: {pending?.OwnerTeam})");

            // Validate sender is owner for this pending selection (basic mapping: host=White(0), client=Black(1))
            ulong senderId = rpcParams.Receive.SenderClientId;
            ulong expectedOwnerClientId = pending.OwnerTeam == Team.White ? 0UL : 1UL;
            if (senderId != expectedOwnerClientId)
            {
                Debug.LogWarning($"[UpgradeManager] Unauthorized select RPC from client {senderId} for team {pending.OwnerTeam}");
                return;
            }

            if (pending.Chosen) return;

            // 실제 업그레이드 적용 (간단한 흐름: 첫 선택을 수락)
            int upgradeIndex = pending.Options.Length > optionIndex && optionIndex >= 0 ? pending.Options[optionIndex] : -1;
            if (upgradeIndex < 0)
                return;

            var upgrade = upgradePool.GetUpgradeByIndex(upgradeIndex);
            if (upgrade == null) return;

            var boardMgr = boardManager ?? FindFirstObjectByType<Board.BoardManager>();
            var piece = boardMgr?.GetPieceAt(coord);

            // Try to recover piece by instance ID if the coordinate occupant is null
            if (piece == null && pending.TargetPieceInstanceId != 0)
            {
                var allPieces = FindObjectsByType<ChessPiece>(FindObjectsSortMode.None);
                foreach (var p in allPieces)
                {
                    if (p != null && p.GetInstanceID() == pending.TargetPieceInstanceId)
                    {
                        piece = p;
                        Debug.LogWarning($"[UpgradeManager] Recovered piece by instance id: {p.name}");
                        break;
                    }
                }
            }

            if (piece == null)
            {
                // Additional debug logging: pending data and current tile occupant
                var tile = boardMgr?.GetTileAt(coord);
                Debug.LogWarning($"[UpgradeManager] Piece not found at coordinate {coord} for applying upgrade. Pending owner: {pending?.OwnerTeam}, Options: {string.Join(",", pending?.Options ?? new int[0])}");
                Debug.LogWarning($"[UpgradeManager] Tile occupant: {(tile?.OccupyingPiece != null ? tile.OccupyingPiece.name : "NULL")}");

                pending.Chosen = true; // 선택 완료로 설정하여 더 이상 선택하지 못함
                return;
            }

            ApplyUpgrade(piece, upgrade);

            // 이벤트 발생
            OnUpgradeApplied?.Invoke(upgrade, piece);
            Bus<UpgradeAppliedEvent>.Raise(new UpgradeAppliedEvent { Piece = piece, Upgrade = upgrade });

            // 클라이언트에 업그레이드 적용 알림
            OnUpgradeAppliedClientRpc(tileX, tileY, upgradePool.GetUpgradeIndex(upgrade));

            pending.Chosen = true;
            pending.ChosenIndex = optionIndex;
            pending.ChosenBy = rpcParams.Receive.SenderClientId;

            NotifySelectionResultClientRpc(optionIndex, tileX, tileY, pending.ChosenBy);

            // 대기 상태 초기화
            ClearPendingSelection();
        }

        [ClientRpc]
        private void NotifySelectionResultClientRpc(int optionIndex, int tileX, int tileY, ulong chosenClientId)
        {
            var ui = FindFirstObjectByType<UpgradeSelectionPanelUI>();
            if (ui != null)
            {
                ui.OnSelectionMade(optionIndex, tileX, tileY, chosenClientId);
            }
        }

        [ClientRpc]
        private void OnUpgradeAppliedClientRpc(int tileX, int tileY, int upgradeIndex)
        {
            if (IsServer) return; // 호스트에서는 서버에서 이미 적용됨

            var boardMgr = FindFirstObjectByType<LevelUpChess.Board.BoardManager>();
            var piece = boardMgr?.GetPieceAt(new UnityEngine.Vector2Int(tileX, tileY));
            var upgrade = GetUpgradeByIndex(upgradeIndex);
            if (upgrade != null && piece != null)
            {
                ApplyUpgrade(piece, upgrade);
            }
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>
        /// 기물 레벨업 이벤트 처리
        /// </summary>
        private void OnPieceLevelUp(PieceLevelUpEvent evt)
        {
            Debug.Log($"[UpgradeManager] OnPieceLevelUp called for {evt.Piece?.name} at level {evt.NewLevel}");
            
            if (evt.Piece == null)
            {
                Debug.LogWarning("[UpgradeManager] evt.Piece is null");
                return;
            }

            // 서버 권한일 때만 업그레이드 선택지 생성 및 클라이언트에 전송
            if (IsServer)
            {
                CreateSelectionsForPieceServer(evt.Piece);
            }
        }

        /// <summary>
        /// 로컬에서 업그레이드 선택지 생성 (네트워크 없이)
        /// </summary>
        private void GenerateUpgradeSelectionsLocal(ChessPiece piece)
        {
            Debug.Log($"[UpgradeManager] GenerateUpgradeSelectionsLocal called for {piece.name}");
            
            if (upgradePool == null)
            {
                Debug.LogError("[UpgradeManager] UpgradePool이 설정되지 않았습니다!");
                return;
            }

            // 적용된 업그레이드 ID 목록
            var excludedIds = allowDuplicates ? null : GetAppliedUpgrades(piece);

            // 희귀도 계산
            int currentLevel = piece.GetComponent<PieceCombat>()?.Level ?? 1;
            int maxRarity = Mathf.Min(baseMaxRarity + (currentLevel - 1) * rarityIncreasePerLevel, maxRarityLevel);
            
            Debug.Log($"[UpgradeManager] Level: {currentLevel}, MaxRarity: {maxRarity}");

            // 가중치 기반 뽑기 시스템 사용
            var drawResults = upgradePool.DrawUpgrades(piece, upgradesPerSelection, excludedIds, maxRarity);

            if (drawResults.Count == 0)
            {
                Debug.LogWarning($"[UpgradeManager] {piece.name}에 적용 가능한 업그레이드가 없습니다.");
                return;
            }

            // 선택지 추출
            var selections = new List<UpgradeBaseSO>();
            for (int i = 0; i < drawResults.Count; i++)
            {
                selections.Add(drawResults[i].Upgrade);
                Debug.Log($"[UpgradeManager] 뽑기 결과 [{i}]: {drawResults[i].Upgrade.UpgradeName} " +
                    $"(공통풀: {drawResults[i].IsFromCommonPool}, 가중치: {drawResults[i].DrawWeight:F2})");
            }

            // 대기 상태 저장
            _pendingSelections = selections;
            _pendingTargetPiece = piece;

            // 네트워크 대기 상태 저장
            var coord = piece.CurrentTile.coordinate;
            var indices = new int[selections.Count];
            for (int i = 0; i < selections.Count; i++)
            {
                indices[i] = upgradePool.GetUpgradeIndex(selections[i]);
            }
            var pending = new PendingSelection
            {
                Options = indices,
                OwnerTeam = piece.Team,
                TargetPieceInstanceId = piece.GetInstanceID()
            };
            _pendingNetworkSelections[coord] = pending;

            // 모든 클라이언트에 UI 표시

            // 선택자 클라이언트에 먼저 표시
            var ownerClientId = piece.Team == Team.White ? 0UL : 1UL;
            var writer = new FastBufferWriter(1024, Unity.Collections.Allocator.Temp);
            writer.WriteValueSafe(indices);
            writer.WriteValueSafe(piece.CurrentTile.coordinate.x);
            writer.WriteValueSafe(piece.CurrentTile.coordinate.y);
            writer.WriteValueSafe((int)piece.Team);
            writer.WriteValueSafe(piece.GetInstanceID());
            NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage("ShowUpgradeSelection", ownerClientId, writer);
            writer.Dispose();

            // 다른 클라이언트에 표시
            ShowUpgradeSelectionClientRpc(indices, piece.CurrentTile.coordinate.x, piece.CurrentTile.coordinate.y, (int)piece.Team, piece.GetInstanceID());

            // 이벤트 발생 (로컬 UI용)
            int subscriberCount = OnUpgradeSelectionAvailable?.GetInvocationList().Length ?? 0;
            Debug.Log($"[UpgradeManager] Invoking OnUpgradeSelectionAvailable. Subscribers: {subscriberCount}");
            
            OnUpgradeSelectionAvailable?.Invoke(selections, piece);
            
            Debug.Log($"[UpgradeManager] 업그레이드 선택지 표시 완료: {selections.Count}개");
        }

        // ShowUpgradeSelectionClientRpc 제거됨 - 로컬에서 직접 처리 (GenerateUpgradeSelectionsLocal에서)

        [ClientRpc]
        private void ShowUpgradeSelectionClientRpc(int[] upgradeIndices, int tileX, int tileY, int ownerTeam, int targetPieceInstanceId)
        {
            var ui = FindFirstObjectByType<UpgradeSelectionPanelUI>();
            if (ui != null)
            {
                ui.ShowWithOptions(upgradeIndices, tileX, tileY, ownerTeam);
            }
        }

        private void OnShowUpgradeSelectionMessage(ulong senderClientId, FastBufferReader reader)
        {
            reader.ReadValueSafe(out int[] upgradeIndices);
            reader.ReadValueSafe(out int tileX);
            reader.ReadValueSafe(out int tileY);
            reader.ReadValueSafe(out int ownerTeam);
            reader.ReadValueSafe(out int targetPieceInstanceId);

            var ui = FindFirstObjectByType<UpgradeSelectionPanelUI>();
            if (ui != null)
            {
                ui.ShowWithOptions(upgradeIndices, tileX, tileY, ownerTeam);
            }
        }

        /// <summary>
        /// 플레이어가 업그레이드 선택 (UI에서 호출) - 로컬 처리
        /// </summary>
        public void SelectUpgrade(int selectionIndex)
        {
            if (selectionIndex < 0 || selectionIndex >= _pendingSelections.Count)
            {
                Debug.LogError($"[UpgradeManager] 잘못된 선택 인덱스: {selectionIndex}");
                return;
            }

            if (_pendingTargetPiece == null)
            {
                Debug.LogError("[UpgradeManager] 대상 기물이 없습니다.");
                return;
            }

            var selectedUpgrade = _pendingSelections[selectionIndex];
            Debug.Log($"[UpgradeManager] 업그레이드 선택: {selectedUpgrade.UpgradeName}");

            // 로컬에서 직접 적용
            ApplyUpgrade(_pendingTargetPiece, selectedUpgrade);

            // 이벤트 발생
            OnUpgradeApplied?.Invoke(selectedUpgrade, _pendingTargetPiece);

            // 대기 상태 초기화
            ClearPendingSelection();
        }

        // SelectUpgradeServerRpc 및 NotifyUpgradeAppliedClientRpc 제거됨 - 로컬에서 직접 처리

        /// <summary>
        /// 업그레이드 적용
        /// </summary>
        private void ApplyUpgrade(ChessPiece piece, UpgradeBaseSO upgrade)
        {
            var combat = piece.GetComponent<PieceCombat>();
            if (combat == null)
            {
                Debug.LogError($"[UpgradeManager] {piece.name}에 PieceCombat이 없습니다.");
                return;
            }

            // 업그레이드 적용
            upgrade.Apply(piece);

            // ChessPiece에 업그레이드 ID 저장
            piece.AddAppliedUpgrade(upgrade.UpgradeHash);

            // 적용 기록
            if (!string.IsNullOrEmpty(upgrade.UpgradeHash))
            {
                TrackAppliedUpgrade(piece, upgrade.UpgradeHash);
            }

            Debug.Log($"[UpgradeManager] {piece.name}에 {upgrade.UpgradeName} 적용 완료");
        }

        /// <summary>
        /// 랜덤 업그레이드 선택
        /// </summary>
        private List<UpgradeBaseSO> SelectRandomUpgrades(List<UpgradeBaseSO> pool, int count)
        {
            var result = new List<UpgradeBaseSO>();
            var tempPool = new List<UpgradeBaseSO>(pool);

            count = Mathf.Min(count, tempPool.Count);

            for (int i = 0; i < count; i++)
            {
                int randomIndex = UnityEngine.Random.Range(0, tempPool.Count);
                result.Add(tempPool[randomIndex]);
                tempPool.RemoveAt(randomIndex);
            }

            return result;
        }

        /// <summary>
        /// 적용된 업그레이드 기록
        /// </summary>
        private void TrackAppliedUpgrade(ChessPiece piece, string upgradeHash)
        {
            if (string.IsNullOrEmpty(upgradeHash)) return;
            
            int pieceId = piece.GetInstanceID();
            
            if (!_pieceAppliedUpgrades.ContainsKey(pieceId))
            {
                _pieceAppliedUpgrades[pieceId] = new List<string>();
            }

            if (!_pieceAppliedUpgrades[pieceId].Contains(upgradeHash))
            {
                _pieceAppliedUpgrades[pieceId].Add(upgradeHash);
            }
        }

        /// <summary>
        /// 기물에 적용된 업그레이드 목록 반환
        /// </summary>
        public List<string> GetAppliedUpgrades(ChessPiece piece)
        {
            int pieceId = piece.GetInstanceID();
            
            if (_pieceAppliedUpgrades.TryGetValue(pieceId, out var upgrades))
            {
                return new List<string>(upgrades);
            }

            return new List<string>();
        }

        /// <summary>
        /// 대기 중인 선택 초기화
        /// </summary>
        public void ClearPendingSelection()
        {
            _pendingSelections.Clear();
            _pendingTargetPiece = null;
        }

        /// <summary>
        /// 선택 취소 (UI에서 호출)
        /// </summary>
        public void CancelSelection()
        {
            ClearPendingSelection();
            OnUpgradeSelectionCancelled?.Invoke();
        }

        /// <summary>
        /// 전역 업그레이드 적용 (게임 시작 시 또는 특수 이벤트)
        /// </summary>
        public void ApplyGlobalUpgrade(int teamId, UpgradeBaseSO upgrade, List<ChessPiece> teamPieces)
        {
            if (upgrade == null || upgrade.UpgradeType != UpgradeType.Global)
            {
                Debug.LogError("[UpgradeManager] 전역 업그레이드가 아닙니다.");
                return;
            }

            // 중복 방지
            if (appliedGlobalUpgrades.Contains(upgrade.UpgradeHash))
            {
                Debug.LogWarning($"[UpgradeManager] 글로벌 업그레이드 {upgrade.UpgradeHash} 이미 적용됨");
                return;
            }

            if (IsServer)
            {
                appliedGlobalUpgrades.Add(upgrade.UpgradeHash);
                
                if (upgrade is GlobalUpgradeSO globalUpgrade)
                {
                    globalUpgrade.ApplyToTeam(teamId, teamPieces);
                }
                
                // 모든 클라이언트에 알림
                int upgradeIndex = upgradePool.GetUpgradeIndex(upgrade);
                NotifyGlobalUpgradeClientRpc(teamId, upgradeIndex);
            }
        }

        [ClientRpc]
        private void NotifyGlobalUpgradeClientRpc(int teamId, int upgradeIndex)
        {
            var upgrade = upgradePool.GetUpgradeByIndex(upgradeIndex);
            if (upgrade != null)
            {
                Debug.Log($"[UpgradeManager] 팀 {teamId}에 전역 업그레이드 적용: {upgrade.UpgradeName}");
            }
        }

        /// <summary>
        /// AbilityContext 생성 헬퍼
        /// </summary>
        public AbilityContext CreateAbilityContext(ChessPiece owner, ChessPiece target = null)
        {
            return new AbilityContext(owner)
            {
                Owner = owner,
                Target = target,
                FromTile = owner?.CurrentTile,
                ToTile = target?.CurrentTile,
                CustomData = boardManager
            };
        }

        /// <summary>
        /// 팀에 글로벌 업그레이드 선택지를 제공 (3개 중 선택)
        /// </summary>
        /// <param name="team">Target team.</param>
        public void GrantGlobalUpgradeWithChoice(Team team)
        {
            if (upgradePool == null)
            {
                Debug.LogWarning("[UpgradeManager] Cannot grant global upgrade with choice: upgradePool is null.");
                return;
            }

            var globals = upgradePool.GlobalUpgrades;
            if (globals == null || globals.Count == 0)
            {
                Debug.LogWarning("[UpgradeManager] Cannot grant global upgrade with choice: no global upgrades configured.");
                return;
            }

            // 3개 랜덤 선택 (중복 없이, 이미 적용된 글로벌 제외)
            var selected = new List<UpgradeBaseSO>();
            var availableIndices = new List<int>();
            for (int i = 0; i < globals.Count; i++)
            {
                if (!appliedGlobalUpgrades.Contains(globals[i].UpgradeHash))
                {
                    availableIndices.Add(i);
                }
            }

            if (availableIndices.Count == 0)
            {
                Debug.LogWarning("[UpgradeManager] 모든 글로벌 업그레이드가 이미 적용됨");
                return;
            }

            int count = Mathf.Min(3, availableIndices.Count);
            for (int i = 0; i < count; i++)
            {
                int randomIndex = UnityEngine.Random.Range(0, availableIndices.Count);
                int globalIndex = availableIndices[randomIndex];
                selected.Add(globals[globalIndex]);
                availableIndices.RemoveAt(randomIndex);
            }

            // UI 표시
            var ui = UnityEngine.Object.FindFirstObjectByType<LevelUpChess.Upgrades.UI.UpgradeSelectionPanelUI>();
            if (ui != null)
            {
                ui.ShowGlobalUpgradeSelections(selected, team);
            }
            else
            {
                Debug.LogWarning("[UpgradeManager] UpgradeSelectionPanelUI not found, falling back to random.");
                GrantRandomGlobalUpgrade(team);
            }
        }

        /// <summary>
        /// Grant a random global upgrade to the specified team (used by PawnSell).
        /// </summary>
        /// <param name="team">Target team.</param>
        public void GrantRandomGlobalUpgrade(Team team)
        {
            if (upgradePool == null)
            {
                Debug.LogWarning("[UpgradeManager] Cannot grant global upgrade: upgradePool is null.");
                return;
            }

            if (boardManager == null)
            {
                Debug.LogWarning("[UpgradeManager] Cannot grant global upgrade: boardManager reference is missing.");
                return;
            }

            var globals = upgradePool.GlobalUpgrades;
            if (globals == null || globals.Count == 0)
            {
                Debug.LogWarning("[UpgradeManager] Cannot grant global upgrade: no global upgrades configured.");
                return;
            }

            int idx = UnityEngine.Random.Range(0, globals.Count);
            var selected = globals[idx];

            if (selected == null)
            {
                Debug.LogWarning("[UpgradeManager] Selected global upgrade is null.");
                return;
            }

            var teamPieces = boardManager.GetPiecesByTeam(team) ?? new List<ChessPiece>();
            ApplyGlobalUpgrade((int)team, selected, teamPieces);
            Debug.Log($"[UpgradeManager] Granted random global upgrade '{selected.UpgradeName}' to team {team}.");
        }

        /// <summary>
        /// 선택된 글로벌 업그레이드를 팀에 적용
        /// </summary>
        public void ApplyGlobalUpgrade(UpgradeBaseSO upgrade, Team team)
        {
            if (upgrade == null)
            {
                Debug.LogWarning("[UpgradeManager] Cannot apply global upgrade: upgrade is null.");
                return;
            }

            if (boardManager == null)
            {
                Debug.LogWarning("[UpgradeManager] Cannot apply global upgrade: boardManager reference is missing.");
                return;
            }

            var teamPieces = boardManager.GetPiecesByTeam(team) ?? new List<ChessPiece>();
            ApplyGlobalUpgrade((int)team, upgrade, teamPieces);
            Debug.Log($"[UpgradeManager] Applied selected global upgrade '{upgrade.UpgradeName}' to team {team}.");

            // UI 숨김
            var ui = UnityEngine.Object.FindFirstObjectByType<LevelUpChess.Upgrades.UI.UpgradeSelectionPanelUI>();
            if (ui != null)
            {
                ui.Hide();
            }
        }

        // 네트워크/외부에서 업그레이드 인덱스로 업그레이드 정보를 얻을 수 있도록 공개 헬퍼
        public UpgradeBaseSO GetUpgradeByIndex(int index)
        {
            if (upgradePool == null) return null;
            return upgradePool.GetUpgradeByIndex(index);
        }

        /// <summary>
        /// 특정 기물에 적용된 업그레이드 ID 목록 반환
        /// </summary>
        public List<string> GetAppliedUpgradeIdsForPiece(ChessPiece piece)
        {
            int pieceId = piece.GetInstanceID();
            if (_pieceAppliedUpgrades.TryGetValue(pieceId, out var upgrades))
            {
                return new List<string>(upgrades);
            }
            return new List<string>();
        }

        /// <summary>
        /// 특정 기물에 적용된 업그레이드 SO 목록 반환
        /// </summary>
        public List<UpgradeBaseSO> GetAppliedUpgradesForPiece(ChessPiece piece)
        {
            var ids = GetAppliedUpgradeIdsForPiece(piece);
            var result = new List<UpgradeBaseSO>();
            foreach (var id in ids)
            {
                if (string.IsNullOrEmpty(id)) continue;
                // upgradePool에서 id로 찾기
                var upgrade = upgradePool?.GetUpgradeById(id);
                if (upgrade != null)
                {
                    result.Add(upgrade);
                }
            }
            return result;
        }

        /// <summary>
        /// 특정 트리거에 해당하는 능력들을 실행
        /// </summary>
        public void ExecuteAbilities(ChessPiece piece, AbilityTrigger trigger, AbilityContext context)
        {
            if (piece == null || upgradePool == null)
                return;

            var appliedUpgrades = GetAppliedUpgradesForPiece(piece);
            foreach (var upgrade in appliedUpgrades)
            {
                if (upgrade is AbilityBaseSO ability && (ability.Trigger == trigger || ability.Trigger == AbilityTrigger.Passive))
                {
                    try
                    {
                        ability.Execute(context);
                        Debug.Log($"[UpgradeManager] Executed ability '{ability.UpgradeName}' for {piece.name} on trigger {trigger}");
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"[UpgradeManager] Error executing ability '{ability.UpgradeName}': {e.Message}");
                    }
                }
            }
        }

        [ClientRpc]
        private void NotifyAttackResultClientRpc(int attackerX, int attackerY, int targetX, int targetY, int damage, bool targetDied)
        {
            // 클라이언트에서 동일 로직 실행 (서버에서 이미 실행되었으므로 중복 방지 필요)
            var boardMgr = ServiceLocator.Get<BoardManager>();
            if (boardMgr == null) return;

            var attackerTile = boardMgr.GetTileAt(attackerX, attackerY);
            var targetTile = boardMgr.GetTileAt(targetX, targetY);

            if (attackerTile?.OccupyingPiece == null || targetTile?.OccupyingPiece == null) return;

            var attacker = attackerTile.OccupyingPiece;
            var target = targetTile.OccupyingPiece;

            // TakeDamage만 동기화 (레벨업은 로컬에서 처리)
            target.Combat.TakeDamage(damage, attacker, handleDeath: false);
        }

        #region Global Upgrades

        /// <summary>
        /// 사용 가능한 글로벌 업그레이드 목록 반환
        /// </summary>
        public List<GlobalUpgradeSO> GetAvailableGlobalUpgrades(Team team)
        {
            if (upgradePool == null) return new List<GlobalUpgradeSO>();
            
            var available = new List<GlobalUpgradeSO>();
            foreach (var upgrade in upgradePool.GlobalUpgrades)
            {
                if (upgrade is GlobalUpgradeSO globalUpgrade)
                {
                    // 이미 적용된 글로벌 업그레이드는 제외
                    bool alreadyApplied = false;
                    foreach (var appliedList in _playerAppliedUpgrades.Values)
                    {
                        if (appliedList.Contains(upgrade.UpgradeHash))
                        {
                            alreadyApplied = true;
                            break;
                        }
                    }
                    
                    if (!alreadyApplied)
                    {
                        available.Add(globalUpgrade);
                    }
                }
            }
            
            return available;
        }

        /// <summary>
        /// 글로벌 업그레이드 적용
        /// </summary>
        public void ApplyGlobalUpgrade(GlobalUpgradeSO upgrade, Team team)
        {
            if (upgrade == null) return;
            
            // 서버에서만 적용
            if (!IsServer) return;
            
            ulong teamKey = (ulong)(int)team;
            
            // 적용 기록
            if (!_playerAppliedUpgrades.ContainsKey(teamKey))
            {
                _playerAppliedUpgrades[teamKey] = new List<string>();
            }
            _playerAppliedUpgrades[teamKey].Add(upgrade.UpgradeHash);
            
            // 업그레이드 적용
            upgrade.ApplyGlobalEffect(team);
            
            Debug.Log($"[UpgradeManager] Applied global upgrade '{upgrade.UpgradeName}' to team {team}");
            
            // 클라이언트에 알림
            NotifyGlobalUpgradeAppliedClientRpc(upgrade.UpgradeHash, team);
        }

        [ClientRpc]
        private void NotifyGlobalUpgradeAppliedClientRpc(string upgradeId, Team team)
        {
            // 클라이언트에서도 업그레이드 적용 (로컬 효과)
            var upgrade = upgradePool?.GlobalUpgrades.Find(u => u.UpgradeHash == upgradeId);
            if (upgrade is GlobalUpgradeSO globalUpgrade)
            {
                globalUpgrade.ApplyGlobalEffect(team);
                Debug.Log($"[UpgradeManager] Client applied global upgrade '{globalUpgrade.UpgradeName}' to team {team}");
            }
        }

        #endregion
    }
}
