using UnityEngine;
using LevelUpChess.UI;

namespace LevelUpChess.Pieces
{
    /// <summary>
    /// 기물의 UI를 담당하는 컴포넌트
    /// - 상태 UI 업데이트
    /// - 레벨 표시
    /// - 상태 효과 표시 등
    /// </summary>
    public class PieceUI : MonoBehaviour
    {
        private ChessPiece _piece;
        private StatusUI _statusUI;
        
        /// <summary>
        /// 초기화
        /// </summary>
        public void Initialize(ChessPiece ownerPiece)
        {
            _piece = ownerPiece;
            _statusUI = GetComponentInChildren<StatusUI>();
        }
        
        /// <summary>
        /// 전체 UI 업데이트 (초기화 없이 값만 설정)
        /// </summary>
        public void UpdateAll(int currentHealth, int maxHealth, int attackPower, int level)
        {
            if (_statusUI != null)
            {
                _statusUI.SetHealth(currentHealth, maxHealth);
                _statusUI.SetAttackPower(attackPower);
                _statusUI.SetLevel(level);
            }
        }
        
        /// <summary>
        /// 상태 UI 초기화 (게임 시작 시 한번만 호출)
        /// </summary>
        public void InitializeStatusUI(int maxHealth, int attackPower, int level = 1, int exp = 0, int expToNextLevel = 100)
        {
            if (_statusUI != null)
            {
                _statusUI.Initialize(maxHealth, attackPower, level, exp, expToNextLevel);
            }
        }
        
        /// <summary>
        /// 체력만 업데이트
        /// </summary>
        public void UpdateHealth(int currentHealth, int maxHealth)
        {
            if (_statusUI != null)
            {
                _statusUI.SetHealth(currentHealth, maxHealth);
            }
        }
        
        /// <summary>
        /// 공격력만 업데이트
        /// </summary>
        public void UpdateAttackPower(int attackPower)
        {
            if (_statusUI != null)
            {
                _statusUI.SetAttackPower(attackPower);
            }
        }
        
        /// <summary>
        /// 레벨 업데이트
        /// </summary>
        public void UpdateLevel(int level)
        {
            if (_statusUI != null)
            {
                _statusUI.SetLevel(level);
            }
        }
        
        /// <summary>
        /// 경험치 업데이트
        /// </summary>
        public void UpdateExperience(int currentExp, int expToNextLevel)
        {
            if (_statusUI != null)
            {
                _statusUI.SetExperience(currentExp, expToNextLevel);
            }
        }
        
        /// <summary>
        /// 레벨업 시 호출 (경험치 바 리셋 애니메이션 포함)
        /// </summary>
        public void OnLevelUp(int newLevel, int remainingExp, int expToNextLevel)
        {
            if (_statusUI != null)
            {
                _statusUI.OnLevelUp(newLevel, remainingExp, expToNextLevel);
            }
            ShowLevelUpEffect();
        }
        
        /// <summary>
        /// 레벨업 효과 표시
        /// </summary>
        public void ShowLevelUpEffect()
        {
            // TODO: 레벨업 이펙트 추가 시 구현
            Debug.Log($"[PieceUI] {_piece?.name} Level Up!");
        }
        
        /// <summary>
        /// 대미지 효과 표시
        /// </summary>
        public void ShowDamageEffect(int damage)
        {
            // TODO: 대미지 숫자 팝업 등 추가 시 구현
        }
        
        /// <summary>
        /// 회복 효과 표시
        /// </summary>
        public void ShowHealEffect(int amount)
        {
            // TODO: 회복 이펙트 추가 시 구현
        }
    }
}
