using UnityEngine;
using LevelUpChess.Pieces;
using LevelUpChess.Board;

namespace LevelUpChess.Upgrades.Abilities
{
    /// <summary>
    /// 자동이동: 자기 턴 행동이후 자동 한칸 전진, 전진시 앞에 적 있을 경우 공격
    /// </summary>
    [CreateAssetMenu(fileName = "PawnAutoMoveAbility", menuName = "LevelUpChess/Upgrades/Abilities/Pawn/PawnAutoMove")]
    public class PawnAutoMoveAbilitySO : AbilityBaseSO
    {
        private const string DEFAULT_NAME = "자동이동";
        private const string DEFAULT_DESC = "자기 턴 행동이후 자동 한칸 전진, 전진시 앞에 적 있을 경우 공격";

        public override void OnApply(ChessPiece piece)
        {
            Debug.Log($"[PawnAutoMove] {piece.name}에게 자동이동 적용");
        }

        public override void OnRemove(ChessPiece piece)
        {
            Debug.Log($"[PawnAutoMove] {piece.name}에서 자동이동 제거");
        }

        public override void Execute(AbilityContext context)
        {
            if (context.Owner == null || context.Trigger != AbilityTrigger.OnTurnEnd) return;

            // 턴 종료 시 자동 이동/공격 실행
            PerformAutoMove(context.Owner, context);
        }

        private void PerformAutoMove(ChessPiece pawn, AbilityContext context)
        {
            if (pawn == null || pawn.CurrentTile == null) return;

            // BoardManager 가져오기
            BoardManager boardManager = context.CustomData as BoardManager;
            if (boardManager == null) return;

            // 폰의 전진 방향 결정 (팀에 따라)
            int direction = pawn.Team == Team.White ? 1 : -1;
            Vector2Int currentPos = pawn.CurrentTile.coordinate;
            Vector2Int forwardPos = new Vector2Int(currentPos.x, currentPos.y + direction);

            // 전진 위치에 적이 있는지 확인
            Tile forwardTile = boardManager.GetTileAt(forwardPos);
            if (forwardTile != null && forwardTile.OccupyingPiece != null && forwardTile.OccupyingPiece.Team != pawn.Team)
            {
                // 적이 있으면 공격
                ChessPiece target = forwardTile.OccupyingPiece;
                Debug.Log($"[PawnAutoMove] {pawn.name} 자동 공격: {target.name}");
                
                // 자동 공격 실행
                pawn.Combat.PerformAttack(forwardTile, target, () => {
                    Debug.Log($"[PawnAutoMove] {pawn.name} 자동 공격 완료");
                });
            }
            else if (forwardTile != null && forwardTile.OccupyingPiece == null)
            {
                // 적이 없으면 이동
                Debug.Log($"[PawnAutoMove] {pawn.name} 자동 이동: {currentPos} -> {forwardPos}");
                
                // 자동 이동 실행
                pawn.MoveToTile(forwardTile, () => {
                    Debug.Log($"[PawnAutoMove] {pawn.name} 자동 이동 완료");
                });
            }
            else
            {
                // 이동 불가 (벽이나 다른 기물)
                Debug.Log($"[PawnAutoMove] {pawn.name} 자동 이동 불가: {forwardPos}");
            }
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            if (string.IsNullOrEmpty(upgradeName)) upgradeName = DEFAULT_NAME;
            if (string.IsNullOrEmpty(description)) description = DEFAULT_DESC;
        }

        protected override void SetDefaultNameAndDescription()
        {
            upgradeName = DEFAULT_NAME;
            description = DEFAULT_DESC;
        }
#endif
    }
}
