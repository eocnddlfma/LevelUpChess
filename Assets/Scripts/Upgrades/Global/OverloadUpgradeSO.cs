using UnityEngine;
using LevelUpChess.Pieces;
using LevelUpChess.Events;
using LevelUpChess.Managers;

namespace LevelUpChess.Upgrades
{
    /// <summary>
    /// 과부화: 상대가 특정 기물을 2회 이상 연속으로 사용했을때 해당 기물은 1턴동안 사용 불가능합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "OverloadUpgrade", menuName = "LevelUpChess/Upgrades/Global/Overload")]
    public class OverloadUpgradeSO : GlobalUpgradeSO
    {
        private const string DEFAULT_NAME = "과부화";
        private const string DEFAULT_DESC = "상대가 특정 기물을 2회 이상 연속으로 사용했을때 해당 기물은 1턴동안 사용 불가능합니다.";

        public override void ApplyGlobalEffect(Team team)
        {
            targetTeam = team;
            Bus<OnPieceMoved>.OnEvent += HandlePieceUsed;
            Bus<OnPieceAttacked>.OnEvent += HandlePieceUsed;
            Bus<OnTurnStart>.OnEvent += HandleTurnStart;
            Debug.Log($"[Overload] {team} 팀에 과부화 적용");
        }

        public override void RemoveGlobalEffect(Team team)
        {
            Bus<OnPieceMoved>.OnEvent -= HandlePieceUsed;
            Bus<OnPieceAttacked>.OnEvent -= HandlePieceUsed;
            Bus<OnTurnStart>.OnEvent -= HandleTurnStart;
            Debug.Log($"[Overload] {team} 팀에서 과부화 제거");
        }

        private void HandlePieceUsed(OnPieceMoved evt)
        {
            HandlePieceUsed(evt.Piece);
        }

        private void HandlePieceUsed(OnPieceAttacked evt)
        {
            HandlePieceUsed(evt.Attacker);
        }

        private void HandlePieceUsed(ChessPiece piece)
        {
            // 상대 팀 기물만
            if (piece.Team == targetTeam) return;

            var gameManager = LevelUpChess.Core.ServiceLocator.Get<NetworkGameManager>();
            if (gameManager == null) return;

            int currentTurn = gameManager.TurnCount;

            if (piece.LastUsedTurn == currentTurn)
            {
                piece.ConsecutiveUses++;
                if (piece.ConsecutiveUses >= 2)
                {
                    piece.IsOverloaded = true;
                    Debug.Log($"[Overload] {piece.name} 과부화! 1턴 사용 불가");
                }
            }
            else
            {
                piece.LastUsedTurn = currentTurn;
                piece.ConsecutiveUses = 1;
            }
        }

        private void HandleTurnStart(OnTurnStart evt)
        {
            // 턴 시작 시 과부화 리셋 (상대 팀 기물만)
            var gameManager = LevelUpChess.Core.ServiceLocator.Get<NetworkGameManager>();
            if (gameManager == null) return;

            var enemyTeam = targetTeam == Team.White ? Team.Black : Team.White;
            var enemyPieces = gameManager.GetPiecesOfTeam(enemyTeam);

            foreach (var piece in enemyPieces)
            {
                if (piece.IsOverloaded)
                {
                    piece.IsOverloaded = false;
                    piece.ConsecutiveUses = 0;
                    Debug.Log($"[Overload] {piece.name} 과부화 해제");
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