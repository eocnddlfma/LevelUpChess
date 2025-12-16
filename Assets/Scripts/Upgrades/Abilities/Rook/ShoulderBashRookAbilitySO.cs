using UnityEngine;
using LevelUpChess.Pieces;
using LevelUpChess.Board;

namespace LevelUpChess.Upgrades.Abilities
{
    /// <summary>
    /// 어깨빵: 십자 이동시 지나간 칸들의 옆칸의 적을 공격력/5만큼 밀침.
    /// </summary>
    [CreateAssetMenu(fileName = "ShoulderBashRookAbility", menuName = "LevelUpChess/Upgrades/Abilities/Rook/ShoulderBash")]
    public class ShoulderBashRookAbilitySO : AbilityBaseSO
    {
        private const string DEFAULT_NAME = "어깨빵";
        private const string DEFAULT_DESC = "십자 이동시 지나간 칸들의 옆칸의 적을 공격력/5만큼 밀침.";

        [Header("Shoulder Bash Settings")]
        [Tooltip("연쇄 충돌시 추가 피해")]
        [SerializeField] private int collisionDamage = 5;

        public override void OnApply(ChessPiece piece)
        {
            Debug.Log($"[ShoulderBashRook] {piece.name}에게 어깨빵 적용");
        }

        public override void OnRemove(ChessPiece piece)
        {
            Debug.Log($"[ShoulderBashRook] {piece.name}에서 어깨빵 제거");
        }

        public override void Execute(AbilityContext context)
        {
            if (context.Owner == null) return;
            if (context.Trigger != AbilityTrigger.OnAfterMove) return;
            if (context.FromTile == null || context.ToTile == null) return;

            var boardManager = LevelUpChess.Core.ServiceLocator.Get<BoardManager>();
            if (boardManager == null) return;

            // 이동 방향 계산
            Vector2Int direction = Vector2Int.zero;
            int dx = context.ToTile.coordinate.x - context.FromTile.coordinate.x;
            int dy = context.ToTile.coordinate.y - context.FromTile.coordinate.y;

            if (Mathf.Abs(dx) > Mathf.Abs(dy))
            {
                direction = new Vector2Int(dx > 0 ? 1 : -1, 0);
            }
            else
            {
                direction = new Vector2Int(0, dy > 0 ? 1 : -1);
            }

            // 밀침 거리 계산
            int pushDistance = Mathf.RoundToInt(context.Owner.Stats.AttackPower / 5f);
            if (pushDistance <= 0) return;

            // 이동 경로의 각 칸에 대해 옆칸 체크
            Vector2Int currentPos = context.FromTile.coordinate;
            int steps = Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy));

            for (int i = 0; i <= steps; i++)
            {
                // 옆칸 방향들
                Vector2Int[] sideDirections;
                if (direction.x != 0)
                {
                    // 수평 이동 - 위, 아래
                    sideDirections = new Vector2Int[] { Vector2Int.up, Vector2Int.down };
                }
                else
                {
                    // 수직 이동 - 왼쪽, 오른쪽
                    sideDirections = new Vector2Int[] { Vector2Int.left, Vector2Int.right };
                }

                foreach (var sideDir in sideDirections)
                {
                    Vector2Int sidePos = currentPos + sideDir;
                    var sideTile = boardManager.GetTileAt(sidePos);
                    if (sideTile != null && sideTile.OccupyingPiece != null && sideTile.OccupyingPiece.Team != context.Owner.Team)
                    {
                        // 적을 밀침
                        PushPiece(sideTile.OccupyingPiece, direction, pushDistance, boardManager);
                    }
                }

                // 다음 칸으로
                if (i < steps)
                {
                    currentPos += direction;
                }
            }
        }

        private void PushPiece(ChessPiece piece, Vector2Int direction, int distance, BoardManager boardManager)
        {
            Vector2Int newPos = piece.CurrentTile.coordinate + (direction * distance);
            var newTile = boardManager.GetTileAt(newPos);

            if (newTile == null)
            {
                // 보드 밖 - 충돌 피해
                piece.Stats.TakeDamage(collisionDamage);
                Debug.Log($"[ShoulderBashRook] {piece.name}이 벽에 충돌! {collisionDamage} 피해!");
                return;
            }

            if (newTile.OccupyingPiece != null)
            {
                // 다른 기물에 충돌
                var collidedPiece = newTile.OccupyingPiece;
                
                piece.Stats.TakeDamage(collisionDamage);
                collidedPiece.Stats.TakeDamage(collisionDamage);
                
                Debug.Log($"[ShoulderBashRook] {piece.name}이 {collidedPiece.name}에 충돌! 양쪽 {collisionDamage} 피해!");
            }
            else
            {
                // 밀치기 성공
                piece.MoveToTile(newTile);
                Debug.Log($"[ShoulderBashRook] {piece.name}을 {newPos}로 밀침!");
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
