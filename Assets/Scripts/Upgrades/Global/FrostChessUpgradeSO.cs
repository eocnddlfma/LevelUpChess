using UnityEngine;
using LevelUpChess.Pieces;
using LevelUpChess.Events;
using LevelUpChess.Managers;

namespace LevelUpChess.Upgrades
{
    /// <summary>
    /// 프로스트체스: 상대 기물들이 이동한 경우 그 다음턴 동안 상대 기물은 얼어서 이동할 수 없습니다.
    /// </summary>
    [CreateAssetMenu(fileName = "FrostChessUpgrade", menuName = "LevelUpChess/Upgrades/Global/Frost Chess")]
    public class FrostChessUpgradeSO : GlobalUpgradeSO
    {
        private const string DEFAULT_NAME = "프로스트체스";
        private const string DEFAULT_DESC = "상대 기물들이 이동한 경우 그 다음턴 동안 상대 기물은 얼어서 이동할 수 없습니다.";

        public override void ApplyGlobalEffect(Team team)
        {
            targetTeam = team;
            Bus<OnPieceMoved>.OnEvent += HandlePieceMoved;
            Bus<OnTurnStart>.OnEvent += HandleTurnStart;
            Debug.Log($"[FrostChess] {team} 팀에 프로스트체스 적용");
        }

        public override void RemoveGlobalEffect(Team team)
        {
            Bus<OnPieceMoved>.OnEvent -= HandlePieceMoved;
            Bus<OnTurnStart>.OnEvent -= HandleTurnStart;
            Debug.Log($"[FrostChess] {team} 팀에서 프로스트체스 제거");
        }

        private void HandlePieceMoved(OnPieceMoved evt)
        {
            // 상대 팀 기물이 이동하면 얼음 효과
            if (evt.Piece.Team != targetTeam)
            {
                evt.Piece.IsFrozen = true;
                Debug.Log($"[FrostChess] {evt.Piece.name} 얼음 효과 적용");
            }
        }

        private void HandleTurnStart(OnTurnStart evt)
        {
            // 턴 시작 시 얼음 효과 리셋 (상대 팀 기물만)
            var gameManager = LevelUpChess.Core.ServiceLocator.Get<NetworkGameManager>();
            if (gameManager == null) return;

            var enemyTeam = targetTeam == Team.White ? Team.Black : Team.White;
            var enemyPieces = gameManager.GetPiecesOfTeam(enemyTeam);

            foreach (var piece in enemyPieces)
            {
                if (piece.IsFrozen)
                {
                    piece.IsFrozen = false;
                    Debug.Log($"[FrostChess] {piece.name} 얼음 효과 해제");
                }
            }
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            if (string.IsNullOrEmpty(upgradeName)) upgradeName = DEFAULT_NAME;
            if (string.IsNullOrEmpty(description)) description = DEFAULT_DESC;
        }
#endif
    }
}