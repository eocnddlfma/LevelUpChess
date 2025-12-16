using UnityEngine;
using LevelUpChess.Pieces;
using LevelUpChess.Board;
using System.Collections.Generic;

namespace LevelUpChess.Upgrades.Abilities
{
    /// <summary>
    /// 폰 구조적 폭력: 공격할때 연결(주변 8칸에 다른 폰이 있다면 +1, 해당 폰을 기준으로 또 연결함)되어 있을때 연결된 폰 갯수만큼 가하는 데미지 배로 증가.
    /// </summary>
    [CreateAssetMenu(fileName = "StructuralViolenceAbility", menuName = "LevelUpChess/Upgrades/Abilities/Pawn/StructuralViolence")]
    public class StructuralViolenceAbilitySO : AbilityBaseSO
    {
        private const string DEFAULT_NAME = "폰 구조적 폭력";
        private const string DEFAULT_DESC = "공격할때 연결(주변 8칸에 다른 폰이 있다면 +1, 해당 폰을 기준으로 또 연결함)되어 있을때 연결된 폰 갯수만큼 가하는 데미지 배로 증가.";

        [Header("Structural Violence Settings")]
        [Tooltip("필요한 연결된 폰 수")]
        [SerializeField] private int requiredPawnCount = 2;

        [Tooltip("연결된 폰 1개당 추가 공격력")]
        [SerializeField] private int bonusAttack = 1;

        public override void OnApply(ChessPiece piece)
        {
            Debug.Log($"[StructuralViolence] {piece.name}에게 구조적 폭력 적용");
        }

        public override void OnRemove(ChessPiece piece)
        {
            Debug.Log($"[StructuralViolence] {piece.name}에서 구조적 폭력 해제");
        }

        public override void Execute(AbilityContext context)
        {
            if (context.Owner == null) return;
            if (context.Trigger != AbilityTrigger.OnAttackHit) return;

            var boardManager = LevelUpChess.Core.ServiceLocator.Get<BoardManager>();
            if (boardManager == null) return;

            var startTile = context.Owner.CurrentTile;
            if (startTile == null) return;

            // Flood fill to count connected pawns in 8 directions.
            var visited = new HashSet<ChessPiece>();
            var stack = new Stack<ChessPiece>();
            stack.Push(context.Owner);
            visited.Add(context.Owner);

            Vector2Int[] offsets =
            {
                new Vector2Int(1, 0), new Vector2Int(-1, 0),
                new Vector2Int(0, 1), new Vector2Int(0, -1),
                new Vector2Int(1, 1), new Vector2Int(1, -1),
                new Vector2Int(-1, 1), new Vector2Int(-1, -1)
            };

            while (stack.Count > 0)
            {
                var current = stack.Pop();
                var tile = current.CurrentTile;
                if (tile == null) continue;

                foreach (var offset in offsets)
                {
                    var neighborTile = boardManager.GetTileAt(tile.coordinate + offset);
                    var neighbor = neighborTile?.OccupyingPiece;
                    if (neighbor != null &&
                        neighbor.Team == context.Owner.Team &&
                        neighbor.PieceType == PieceType.Pawn &&
                        !visited.Contains(neighbor))
                    {
                        visited.Add(neighbor);
                        stack.Push(neighbor);
                    }
                }
            }

            int pawnCount = visited.Count; // includes self
            if (pawnCount >= requiredPawnCount)
            {
                int totalBonus = bonusAttack * pawnCount;
                context.BonusDamage += totalBonus;
                Debug.Log($"[StructuralViolence] 연결된 폰 {pawnCount}개! 추가 피해 {totalBonus}");
            }
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            if (string.IsNullOrEmpty(upgradeName)) upgradeName = DEFAULT_NAME;
            if (string.IsNullOrEmpty(description)) description = DEFAULT_DESC;
            trigger = AbilityTrigger.OnAttackHit;
            pieceFilter = PieceTypeFilter.Pawn;
        }
#endif
    }
}
