using UnityEngine;
using LevelUpChess.Upgrades;
using LevelUpChess.Board;
using LevelUpChess.Pieces;

namespace LevelUpChess.Upgrades.Abilities
{
    /// <summary>
    /// 폰 자동이동 능력: 턴 종료 시 자동으로 전진 및 공격
    /// 폰 전용 능력
    /// </summary>
    [CreateAssetMenu(fileName = "AutoMoveAbility", menuName = "LevelUpChess/Upgrades/Abilities/AutoMove")]
    public class AutoMoveAbilitySO : AbilityBaseSO
    {
        [Header("Auto Move Settings")]
        [Tooltip("자동 이동 거리 (칸)")]
        [SerializeField] private int moveDistance = 1;
        
        [Tooltip("적이 있으면 자동 공격")]
        [SerializeField] private bool autoAttack = true;
        
        [Tooltip("자동 이동 방향 (true: 전진, false: 후진)")]
        [SerializeField] private bool moveForward = true;
        
        [Tooltip("장애물 있으면 이동 취소")]
        [SerializeField] private bool stopOnObstacle = true;

        public new string AbilityId => "ability_auto_move";
        public new string AbilityName => "자동 전진";
        public new string Description => autoAttack 
            ? $"턴 종료 시 {moveDistance}칸 자동 전진하고, 적이 있으면 공격합니다." 
            : $"턴 종료 시 {moveDistance}칸 자동 전진합니다.";

        public override void OnApply(ChessPiece piece)
        {
            Debug.Log($"[AutoMove] {piece.name}에게 자동 이동 능력 적용");
        }

        public override void OnRemove(ChessPiece piece)
        {
            Debug.Log($"[AutoMove] {piece.name}에서 자동 이동 능력 제거");
        }

        public override void Execute(AbilityContext context)
        {
            if (context.Owner == null)
            {
                Debug.LogWarning("[AutoMove] Owner가 없습니다.");
                return;
            }

            var chessPiece = context.Owner.GetComponent<ChessPiece>();
            if (chessPiece == null)
            {
                Debug.LogWarning("[AutoMove] ChessPiece 컴포넌트를 찾을 수 없습니다.");
                return;
            }

            // 현재 위치 기준으로 전진 방향 계산
            var currentTile = chessPiece.CurrentTile;
            if (currentTile == null)
            {
                Debug.LogWarning("[AutoMove] 현재 타일이 없습니다.");
                return;
            }

            // 팀에 따라 전진 방향 결정 (팀 0: +Y, 팀 1: -Y)
            int direction = (int)chessPiece.Team == 0 ? 1 : -1;
            if (!moveForward) direction *= -1;

            Vector2Int targetPosition = currentTile.coordinate + new Vector2Int(0, direction * moveDistance);
            
            Debug.Log($"[AutoMove] {chessPiece.name}이(가) {targetPosition}으로 자동 이동 시도");

            // 타겟 타일 확인 (BoardManager 또는 Grid 시스템을 통해)
            // 실제 이동은 턴 시스템에서 처리해야 함
            // 여기서는 자동 이동 요청만 설정
            
            // 자동 이동 정보를 context에 저장
            // TODO: CustomData는 Dictionary가 아니므로 별도 처리 필요
            // context.CustomData = new { AutoMoveTarget = targetPosition, AutoAttack = autoAttack, StopOnObstacle = stopOnObstacle };
            
            // 실제 이동 처리는 TurnManager 또는 별도 시스템에서 이 데이터를 읽어 처리
        }
        
        /// <summary>
        /// 자동 이동 실행 (TurnManager에서 호출)
        /// </summary>
        public bool TryAutoMove(ChessPiece piece, BoardManager boardManager)
        {
            if (piece == null || boardManager == null) return false;
            
            var currentTile = piece.CurrentTile;
            if (currentTile == null) return false;

            int direction = (int)piece.Team == 0 ? 1 : -1;
            if (!moveForward) direction *= -1;

            Vector2Int targetPosition = currentTile.coordinate + new Vector2Int(0, direction * moveDistance);
            
            // 보드 범위 확인
            var targetTile = boardManager.GetTileAt(targetPosition);
            if (targetTile == null)
            {
                Debug.Log($"[AutoMove] 타겟 위치가 보드 범위 밖입니다: {targetPosition}");
                return false;
            }

            // 적이 있는 경우
            if (targetTile.OccupyingPiece != null)
            {
                if ((int)targetTile.OccupyingPiece.Team != (int)piece.Team && autoAttack)
                {
                    // 자동 공격
                    Debug.Log($"[AutoMove] {piece.name}이(가) {targetTile.OccupyingPiece.name}을(를) 자동 공격");
                    // piece.Attack(targetTile.OccupiedPiece);
                    return true;
                }
                else if (stopOnObstacle)
                {
                    Debug.Log($"[AutoMove] 장애물로 인해 이동 취소");
                    return false;
                }
            }
            
            // 빈 칸으로 이동
            Debug.Log($"[AutoMove] {piece.name}이(가) {targetPosition}으로 자동 이동");
            // piece.MoveTo(targetTile);
            return true;
        }
    }
}
