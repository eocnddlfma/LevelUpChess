using UnityEngine;
using System.Collections.Generic;
using LevelUpChess.Pieces;
using LevelUpChess.Board;

namespace LevelUpChess.Upgrades.Abilities
{
    /// <summary>
    /// 떼껄룩: 십자 방향중 막히지 않은 위치에 있는 적 피스는 행동할 수 없습니다.
    /// </summary>
    [CreateAssetMenu(fileName = "DisableCrossAbility", menuName = "LevelUpChess/Upgrades/Abilities/Rook/DisableCross")]
    public class DisableCrossAbilitySO : AbilityBaseSO
    {
        private const string DEFAULT_NAME = "떼껄룩";
        private const string DEFAULT_DESC = "십자 방향중 막히지 않은 위치에 있는 적 피스는 행동할 수 없습니다.";

        [Header("Disable Cross Settings")]
        [Tooltip("효과 범위 (칸 수, 0 = 무제한)")]
        [SerializeField] private int range = 0;

        // 현재 디버프가 적용된 적들 추적
        private Dictionary<ChessPiece, List<ChessPiece>> _disabledEnemies = new();

        public override void OnApply(ChessPiece piece)
        {
            Debug.Log($"[DisableCross] {piece.name}에게 떼껄룩 적용");
            _disabledEnemies[piece] = new List<ChessPiece>();
            UpdateDisables(piece);
        }

        public override void OnRemove(ChessPiece piece)
        {
            Debug.Log($"[DisableCross] {piece.name}에서 떼껄룩 제거");
            ClearDisables(piece);
            _disabledEnemies.Remove(piece);
        }

        private void UpdateDisables(ChessPiece owner)
        {
            if (owner?.CurrentTile == null) return;

            var boardManager = LevelUpChess.Core.ServiceLocator.Get<BoardManager>();
            if (boardManager == null) return;

            // 기존 디버프 제거
            ClearDisables(owner);

            // 십자 방향 체크
            Vector2Int[] directions = new Vector2Int[]
            {
                Vector2Int.up,
                Vector2Int.down,
                Vector2Int.left,
                Vector2Int.right
            };

            foreach (var dir in directions)
            {
                int maxDist = range == 0 ? 8 : range;
                for (int i = 1; i <= maxDist; i++)
                {
                    Vector2Int checkPos = owner.CurrentTile.coordinate + (dir * i);
                    var tile = boardManager.GetTileAt(checkPos);
                    
                    if (tile == null) break;

                    if (tile.OccupyingPiece != null)
                    {
                        if (tile.OccupyingPiece.Team != owner.Team)
                        {
                            // 적에게 행동 불가 적용
                            tile.OccupyingPiece.IsDisabled = true;
                            _disabledEnemies[owner].Add(tile.OccupyingPiece);
                            Debug.Log($"[DisableCross] {tile.OccupyingPiece.name} 행동 불가!");
                        }
                        // 아군이든 적이든 막혀있으면 그 방향 중단
                        break;
                    }
                }
            }
        }

        private void ClearDisables(ChessPiece owner)
        {
            if (!_disabledEnemies.ContainsKey(owner)) return;

            foreach (var enemy in _disabledEnemies[owner])
            {
                if (enemy != null)
                {
                    enemy.IsDisabled = false;
                }
            }
            _disabledEnemies[owner].Clear();
        }

        public override void Execute(AbilityContext context)
        {
            if (context.Owner == null) return;
            
            // 이동 후 행동 불가 재계산
            if (context.Trigger == AbilityTrigger.OnAfterMove)
            {
                UpdateDisables(context.Owner);
            }
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            if (string.IsNullOrEmpty(upgradeName)) upgradeName = DEFAULT_NAME;
            if (string.IsNullOrEmpty(description)) description = DEFAULT_DESC;
            trigger = AbilityTrigger.OnAfterMove;
            pieceFilter = PieceTypeFilter.Rook;
        }
#endif
    }
}
