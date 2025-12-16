using UnityEngine;
using LevelUpChess.Pieces;
using LevelUpChess.Board;

namespace LevelUpChess.Upgrades.Abilities
{
    /// <summary>
    /// 폰 자동이동: 자기 턴 행동이후 자동 한칸 전진, 전진시 앞에 적 있을 경우 공격
    /// </summary>
    [CreateAssetMenu(fileName = "PawnAutoMoveAbility", menuName = "LevelUpChess/Upgrades/Abilities/Pawn/AutoMove")]
    public class PawnAutoMoveAbilitySO : AbilityBaseSO
    {
        private const string DEFAULT_NAME = "자동이동";
        private const string DEFAULT_DESC = "자기 턴 행동이후 자동 한칸 전진, 전진시 앞에 적 있을 경우 공격";

        public override void OnApply(ChessPiece piece)
        {
            Debug.Log($"[PawnAutoMove] {piece.name}에게 자동이동 능력 적용");
        }

        public override void OnRemove(ChessPiece piece)
        {
            Debug.Log($"[PawnAutoMove] {piece.name}에서 자동이동 능력 제거");
        }

        public override void Execute(AbilityContext context)
        {
            if (context.Owner == null || context.Owner.PieceType != PieceType.Pawn) return;
            
            if (context.Trigger == AbilityTrigger.OnTurnEnd)
            {
                var currentTile = context.Owner.CurrentTile;
                if (currentTile == null) return;
                
                var boardManager = LevelUpChess.Core.ServiceLocator.Get<BoardManager>();
                if (boardManager == null) return;

                // 전진 방향 계산 (White는 위로, Black은 아래로)
                int direction = context.Owner.Team == Team.White ? 1 : -1;
                Vector2Int forwardPos = currentTile.coordinate + new Vector2Int(0, direction);
                
                var forwardTile = boardManager.GetTileAt(forwardPos);
                if (forwardTile == null) return;

                if (forwardTile.OccupyingPiece == null)
                {
                    // 빈 칸이면 이동
                    context.Owner.MoveToTile(forwardTile);
                    Debug.Log($"[PawnAutoMove] {context.Owner.name} 자동 전진!");
                }
                else if (forwardTile.OccupyingPiece.Team != context.Owner.Team)
                {
                    // 적이 있으면 공격
                    var target = forwardTile.OccupyingPiece;
                    context.Owner.Combat.PerformAttack(forwardTile, target, () =>
                    {
                        Debug.Log($"[PawnAutoMove] {context.Owner.name} 자동 공격!");
                    });
                }
            }
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            if (string.IsNullOrEmpty(upgradeName)) upgradeName = DEFAULT_NAME;
            if (string.IsNullOrEmpty(description)) description = DEFAULT_DESC;
            trigger = AbilityTrigger.OnTurnEnd;
            pieceFilter = PieceTypeFilter.Pawn;
        }
#endif
    }
}
