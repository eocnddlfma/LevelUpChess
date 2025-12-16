using UnityEngine;
using LevelUpChess.Upgrades;
using LevelUpChess.Pieces;
using System.Collections.Generic;

namespace LevelUpChess.Upgrades.Global
{
    /// <summary>
    /// 갬빗: 킹을 제외한 모든 피스가 각자 50% 확률로 사망합니다. 생존한 피스는 레벨이 3 증가합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "GambitUpgrade", menuName = "LevelUpChess/Upgrades/Global/Gambit")]
    public class GambitUpgradeSO : GlobalUpgradeSO
    {
        private const string DEFAULT_NAME = "갬빗";
        private const string DEFAULT_DESC = "킹을 제외한 모든 피스가 각자 50% 확률로 사망합니다. 생존한 피스는 레벨이 3 증가합니다.";

        [Header("Gambit Settings")]
        [Tooltip("피스 사망 확률 (0~1)")]
        [SerializeField] private float deathChance = 0.5f;
        
        [Tooltip("생존 피스 레벨 증가")]
        [SerializeField] private int levelBonus = 3;

        // 갬빗 적용된 팀의 피스 목록 (생존 여부 추적)
        private Dictionary<ulong, List<ChessPiece>> _gambitPieces = new Dictionary<ulong, List<ChessPiece>>();

        public override void ApplyGlobalEffect(Team team)
        {
            // Find all pieces in the team except King
            var pieces = new List<ChessPiece>();
            var allPieces = Object.FindObjectsByType<ChessPiece>(FindObjectsSortMode.None);
            foreach (var p in allPieces)
            {
                if (p != null && p.Team == team && p.PieceType != PieceType.King)
                    pieces.Add(p);
            }

            int teamId = (int)team;
            if (pieces == null || pieces.Count == 0)
            {
                Debug.LogWarning("[Gambit] 적용할 기물이 없습니다.");
                return;
            }

            Debug.Log($"[Gambit] 팀 {teamId}에 갬빗 적용 - 총 피스 수: {pieces.Count}");

            // 갬빗 발동 - 50% 확률로 피스 제거
            List<ChessPiece> survivors = new List<ChessPiece>();
            List<ChessPiece> casualties = new List<ChessPiece>();
            
            // 랜덤하게 분류
            foreach (var piece in pieces)
            {
                if (Random.value < deathChance)
                {
                    casualties.Add(piece);
                }
                else
                {
                    survivors.Add(piece);
                }
            }

            // 사망 처리
            foreach (var casualty in casualties)
            {
                Debug.Log($"[Gambit] {casualty.name}이(가) 갬빗으로 희생됩니다!");
                
                // 사망 처리
                casualty.gameObject.SetActive(false); // 임시 처리
            }

            // 생존자 레벨 증가
            foreach (var survivor in survivors)
            {
                survivor.Level += levelBonus;
                Debug.Log($"[Gambit] {survivor.name} 생존! 레벨 +{levelBonus}");
            }

            Debug.Log($"[Gambit] 갬빗 결과 - 희생: {casualties.Count}, 생존: {survivors.Count}");
        }

        public override void RemoveGlobalEffect(Team team)
        {
            ulong teamKey = (ulong)(int)team;
            _gambitPieces.Remove(teamKey);
            
            Debug.Log($"[Gambit] 팀 {team}에서 갬빗 제거");
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            upgradeName = DEFAULT_NAME;
            description = DEFAULT_DESC;
        }
#endif
    }
}
