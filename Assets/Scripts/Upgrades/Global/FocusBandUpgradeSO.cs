using UnityEngine;
using System.Collections.Generic;
using LevelUpChess.Pieces;

namespace LevelUpChess.Upgrades
{
    /// <summary>
    /// 기합의 띠: 체력이 0이 되는 공격을 받아도 한번 무마하고 체력을 1로 바꿉니다.
    /// </summary>
    [CreateAssetMenu(fileName = "FocusBandUpgrade", menuName = "LevelUpChess/Upgrades/Global/FocusBand")]
    public class FocusBandUpgradeSO : GlobalUpgradeSO
    {
        private const string DEFAULT_NAME = "기합의 띠";
        private const string DEFAULT_DESC = "체력이 0이 되는 공격을 받아도 한번 무마하고 체력을 1로 바꿉니다.";

        private Dictionary<ChessPiece, bool> hasUsedFocusBand = new Dictionary<ChessPiece, bool>();
        private HashSet<int> affectedTeams = new HashSet<int>();
        private List<ChessPiece> affectedPieces = new List<ChessPiece>();

        public override void ApplyGlobalEffect(Team team)
        {
            int teamId = (int)team;
            if (affectedTeams.Contains(teamId))
                return;

            affectedTeams.Add(teamId);
            
            var allPieces = Object.FindObjectsByType<ChessPiece>(FindObjectsSortMode.None);
            foreach (var piece in allPieces)
            {
                if (piece != null && (int)piece.Team == teamId)
                {
                    hasUsedFocusBand[piece] = false;
                    piece.OnBeforeDeath += OnBeforeDeath;
                    affectedPieces.Add(piece);
                }
            }
            
            Debug.Log($"[FocusBand] Team {team}에 기합의 띠 적용");
        }

        public override void ApplyToTeam(int teamId, List<ChessPiece> pieces)
        {
            if (affectedTeams.Contains(teamId))
                return;

            affectedTeams.Add(teamId);

            foreach (var piece in pieces)
            {
                if (piece != null)
                {
                    hasUsedFocusBand[piece] = false;
                    piece.OnBeforeDeath += OnBeforeDeath;
                    affectedPieces.Add(piece);
                }
            }
            
            Debug.Log($"[FocusBand] Team {teamId}에 기합의 띠 적용");
        }

        public override void RemoveGlobalEffect(Team team)
        {
            int teamId = (int)team;
            affectedTeams.Remove(teamId);

            foreach (var piece in affectedPieces)
            {
                if (piece != null)
                {
                    hasUsedFocusBand.Remove(piece);
                    piece.OnBeforeDeath -= OnBeforeDeath;
                }
            }
            affectedPieces.RemoveAll(p => (int)p.Team == teamId);
        }

        private bool OnBeforeDeath(ChessPiece piece)
        {
            if (piece == null)
                return false;

            // 이미 사용했으면 패스
            if (hasUsedFocusBand.TryGetValue(piece, out bool used) && used)
                return false;

            // 기합의 띠 발동
            hasUsedFocusBand[piece] = true;
            piece.SetHealth(1);
            
            Debug.Log($"[FocusBand] {piece.name}이 기합의 띠로 생존! 체력 1");
            
            return true; // 죽음 방지
        }

        public bool HasUsedFocusBand(ChessPiece piece)
        {
            return hasUsedFocusBand.TryGetValue(piece, out bool used) && used;
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            upgradeName = DEFAULT_NAME;
            description = DEFAULT_DESC;
        }
    }
}
