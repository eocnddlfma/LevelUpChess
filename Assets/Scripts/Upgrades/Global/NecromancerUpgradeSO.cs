using UnityEngine;
using System.Collections.Generic;
using LevelUpChess.Pieces;

namespace LevelUpChess.Upgrades
{
    /// <summary>
    /// 네크로멘서: 킹의 체력이 1이 되고, 모든 폰이 불사 상태가 됨
    /// </summary>
    [CreateAssetMenu(fileName = "NecromancerUpgrade", menuName = "LevelUpChess/Upgrades/Global/Necromancer")]
    public class NecromancerUpgradeSO : GlobalUpgradeSO
    {
        private const string DEFAULT_NAME = "네크로맨서";
        private const string DEFAULT_DESC = "킹의 체력이 1이 됩니다. 폰이 죽지 않습니다.";

        private HashSet<int> affectedTeams = new HashSet<int>();
        private Dictionary<ChessPiece, int> originalKingHealth = new Dictionary<ChessPiece, int>();
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
                    ApplyNecromancy(piece);
                    affectedPieces.Add(piece);
                }
            }
            
            Debug.Log($"[Necromancer] Team {team}에 네크로멘서 적용");
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
                    ApplyNecromancy(piece);
                    affectedPieces.Add(piece);
                }
            }
            
            Debug.Log($"[Necromancer] Team {teamId}에 네크로멘서 적용");
        }

        public override void RemoveGlobalEffect(Team team)
        {
            int teamId = (int)team;
            affectedTeams.Remove(teamId);

            foreach (var piece in affectedPieces)
            {
                if (piece != null)
                {
                    RemoveNecromancy(piece);
                }
            }
            affectedPieces.RemoveAll(p => (int)p.Team == teamId);
        }

        private void ApplyNecromancy(ChessPiece piece)
        {
            if (piece.PieceType == PieceType.King)
            {
                // 킹의 체력을 1로 설정
                originalKingHealth[piece] = piece.MaxHealth;
                piece.SetMaxHealth(1);
                piece.SetHealth(1);
                Debug.Log($"[Necromancer] {piece.name} 킹의 체력이 1로 감소");
            }
            else if (piece.PieceType == PieceType.Pawn)
            {
                // 폰에게 불사 적용
                piece.IsImmortal = true;
                piece.OnBeforeDeath += PreventPawnDeath;
                Debug.Log($"[Necromancer] {piece.name} 폰이 불사 상태");
            }
        }

        private void RemoveNecromancy(ChessPiece piece)
        {
            if (piece.PieceType == PieceType.King)
            {
                if (originalKingHealth.TryGetValue(piece, out int originalHealth))
                {
                    piece.SetMaxHealth(originalHealth);
                    originalKingHealth.Remove(piece);
                }
            }
            else if (piece.PieceType == PieceType.Pawn)
            {
                piece.IsImmortal = false;
                piece.OnBeforeDeath -= PreventPawnDeath;
            }
        }

        private bool PreventPawnDeath(ChessPiece piece)
        {
            if (piece == null || piece.PieceType != PieceType.Pawn)
                return false;

            // 폰은 죽지 않고 체력 1로 유지
            piece.SetHealth(1);
            Debug.Log($"[Necromancer] {piece.name} 폰이 불사로 생존!");
            
            return true; // 죽음 방지
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            upgradeName = DEFAULT_NAME;
            description = DEFAULT_DESC;
        }
    }
}
