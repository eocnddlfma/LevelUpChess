using UnityEngine;
using LevelUpChess.Pieces;
using LevelUpChess.Board;
using System.Collections.Generic;
using LevelUpChess.Pieces.Movements.UpgradableMovements.Queen;

namespace LevelUpChess.Upgrades.Abilities
{
    /// <summary>
    /// 퀸 전용: 시간역행 - 이전에 이동/공격 할 수 있던 칸의 경우 이후에도 이동/공격할 수 있습니다.
    /// 현재 이동가능한 행마법이 없을때 이 능력을 이용해서 이동시 이동할 수 있는 위치가 초기화됩니다.
    /// </summary>
    [CreateAssetMenu(fileName = "TimeRewindAbility", menuName = "LevelUpChess/Upgrades/Abilities/Queen/Time Rewind")]
    public class TimeRewindAbilitySO : AbilityBaseSO
    {
        [SerializeField] private TimeRewindMovementSO timeRewindMovement;

        private const string DEFAULT_NAME = "시간역행";
        private const string DEFAULT_DESC = "사용 시 이동 가능한 위치가 초기화됩니다.";

        public override void OnApply(ChessPiece piece)
        {
            if (piece.PieceType != PieceType.Queen) return;

            piece.HasTimeRewind = true;
            piece.PreviousAvailableTiles.Clear();

            // 초기 이전 칸들 설정 (현재 이동 가능한 칸들)
            var availableMoves = piece.GetAvailableMoves();
            foreach (var move in availableMoves)
            {
                if (!piece.PreviousAvailableTiles.Contains(move.to))
                {
                    piece.PreviousAvailableTiles.Add(move.to);
                }
            }

            // 시간역행 이동 전략 추가
            if (timeRewindMovement != null)
            {
                piece.AddMovementStrategy(timeRewindMovement);
            }

            Debug.Log($"[TimeRewind] {piece.name}에게 시간역행 적용 - 초기 칸 수: {piece.PreviousAvailableTiles.Count}");
        }

        public override void OnRemove(ChessPiece piece)
        {
            piece.HasTimeRewind = false;
            piece.PreviousAvailableTiles.Clear();

            // 이동 전략 제거
            if (timeRewindMovement != null)
            {
                piece.RemoveMovementStrategy(timeRewindMovement);
            }

            Debug.Log($"[TimeRewind] {piece.name}에서 시간역행 제거");
        }

        public override void Execute(AbilityContext context)
        {
            if (context.Owner == null || context.Owner.PieceType != PieceType.Queen) return;
            if (context.Trigger != AbilityTrigger.OnTurnStart) return;

            // 턴 시작시 이전 칸들 업데이트
            var availableMoves = context.Owner.GetAvailableMoves();
            foreach (var move in availableMoves)
            {
                if (!context.Owner.PreviousAvailableTiles.Contains(move.to))
                {
                    context.Owner.PreviousAvailableTiles.Add(move.to);
                }
            }

            Debug.Log($"[TimeRewind] {context.Owner.name} 이전 칸 업데이트 - 총 칸 수: {context.Owner.PreviousAvailableTiles.Count}");
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            if (string.IsNullOrEmpty(upgradeName)) upgradeName = DEFAULT_NAME;
            if (string.IsNullOrEmpty(description)) description = DEFAULT_DESC;
            trigger = AbilityTrigger.OnTurnStart;
            pieceFilter = PieceTypeFilter.Queen;
        }
#endif
    }
}

// ChessPiece 확장
namespace LevelUpChess.Pieces
{
    public partial class ChessPiece
    {
        // 시간역행 능력 관련
        public bool HasTimeRewind { get; set; } = false;
        public List<Vector2Int> PreviousAvailableTiles { get; } = new List<Vector2Int>();

        // 글로벌 업그레이드 관련
        public bool IsFrozen { get; set; } = false;
        public int LastUsedTurn { get; set; } = -1;
        public int ConsecutiveUses { get; set; } = 0;
        public bool IsOverloaded { get; set; } = false;
    }
}