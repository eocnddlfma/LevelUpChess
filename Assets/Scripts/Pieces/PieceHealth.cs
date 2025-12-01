using UnityEngine;
using LevelUpChess.Core;
using LevelUpChess.Board;
using LevelUpChess.Events;
using LevelUpChess.Managers;
using LevelUpChess.UI;

namespace LevelUpChess.Pieces
{
    /// <summary>
    /// 기물의 체력 관리 컴포넌트
    /// ChessPiece에서 체력 관련 책임을 분리
    /// </summary>
    public class PieceHealth : MonoBehaviour
    {
        private ChessPiece piece;
        private int currentHealth;
        private int maxHealth;
        private int attackPower;
        private HealthBarUI healthBarUI;
        
        public int CurrentHealth => currentHealth;
        public int MaxHealth => maxHealth;
        public int AttackPower => attackPower;
        public bool IsAlive => currentHealth > 0;
        
        public void Initialize(ChessPiece ownerPiece, int maxHp)
        {
            piece = ownerPiece;
            maxHealth = maxHp;
            currentHealth = maxHp;
            attackPower = ownerPiece.AttackPower;
            
            // HealthBarUI 찾기 및 초기화
            healthBarUI = GetComponentInChildren<HealthBarUI>();
            if (healthBarUI != null)
            {
                healthBarUI.Initialize(maxHealth, attackPower);
            }
        }
        
        /// <summary>
        /// 공격력 설정
        /// </summary>
        public void SetAttackPower(int attack)
        {
            attackPower = attack;
            if (healthBarUI != null)
            {
                healthBarUI.SetAttackPower(attackPower);
            }
        }
        
        /// <summary>
        /// 대미지를 받음
        /// </summary>
        public void TakeDamage(int amount)
        {
            currentHealth -= amount;
            currentHealth = Mathf.Max(0, currentHealth);
            
            // 체력바 UI 업데이트
            UpdateHealthBarUI();
            
            if (currentHealth <= 0)
            {
                Die();
            }
        }
        
        /// <summary>
        /// 체력 회복
        /// </summary>
        public void Heal(int amount)
        {
            currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
            
            // 체력바 UI 업데이트
            UpdateHealthBarUI();
        }
        
        /// <summary>
        /// 체력바 UI 업데이트
        /// </summary>
        private void UpdateHealthBarUI()
        {
            if (healthBarUI != null)
            {
                healthBarUI.SetHealth(currentHealth, maxHealth);
            }
        }
        
        /// <summary>
        /// 기물 사망 처리
        /// </summary>
        public void Die()
        {
            // 현재 타일에서 제거
            if (piece.CurrentTile != null)
            {
                piece.CurrentTile.OccupyingPiece = null;
            }
            
            // BoardManager에서 등록 해제
            var boardManager = ServiceLocator.Get<BoardManager>();
            if (boardManager != null)
            {
                boardManager.UnregisterPiece(piece);
            }
            
            // King 사망 시 게임 오버
            if (piece.PieceType == PieceType.King)
            {
                Team winnerTeam = piece.Team == Team.White ? Team.Black : Team.White;
                
                var networkGameManager = ServiceLocator.Get<NetworkGameManager>();
                if (networkGameManager != null)
                {
                    networkGameManager.SetGameOverServerRpc(winnerTeam);
                }
                else
                {
                    Bus<GameOverEvent>.Raise(new GameOverEvent { WinnerTeam = winnerTeam });
                }
            }
            
            Destroy(gameObject);
        }
    }
}
