using UnityEngine;
using DG.Tweening;
using System;

namespace LevelUpChess.Pieces
{
    /// <summary>
    /// 기물의 이동/공격 애니메이션을 담당하는 컴포넌트
    /// 단일 책임: 순수하게 애니메이션(트윈)만 처리
    /// 게임 로직(대미지, 타일 업데이트 등)은 ChessPiece가 처리
    /// </summary>
    public class PieceAnimator : MonoBehaviour
    {
        [Header("이동 애니메이션 설정")]
        [SerializeField] private float moveDuration = 0.1f;
        [SerializeField] private Ease moveEase = Ease.InOutQuad;
        
        [Header("공격 애니메이션 설정")]
        [SerializeField] private float attackLungeDuration = 0.08f;   // 찌르기 전진 시간
        [SerializeField] private float attackReturnDuration = 0.06f;  // 찌르기 복귀 시간
        [SerializeField] private float lungeDistance = 0.3f;          // 찌르기 거리 (0~1, 대상까지의 비율)
        [SerializeField] private Ease attackMoveEase = Ease.OutQuad;
        [SerializeField] private Ease attackReturnEase = Ease.OutQuad;
        
        private Tween _currentTween;
        private Sequence _currentSequence;
        
        public float MoveDuration => moveDuration;
        public bool IsAnimating => (_currentTween != null && _currentTween.IsActive()) || 
                                   (_currentSequence != null && _currentSequence.IsActive());
        
        /// <summary>
        /// 이동 시간 설정
        /// </summary>
        public void SetMoveDuration(float duration)
        {
            moveDuration = duration;
        }
        
        /// <summary>
        /// 진행 중인 애니메이션 중지
        /// </summary>
        public void StopAnimation()
        {
            if (_currentTween != null && _currentTween.IsActive())
            {
                _currentTween.Kill();
                _currentTween = null;
            }
            
            if (_currentSequence != null && _currentSequence.IsActive())
            {
                _currentSequence.Kill();
                _currentSequence = null;
            }
        }
        
        /// <summary>
        /// 특정 위치로 이동 애니메이션
        /// </summary>
        public Tween AnimateMoveTo(Vector3 targetPos, Action onComplete = null)
        {
            StopAnimation();
            
            targetPos.z = transform.position.z;
            
            _currentTween = transform.DOMove(targetPos, moveDuration)
                .SetEase(moveEase)
                .OnComplete(() => onComplete?.Invoke());
            
            return _currentTween;
        }
        
        /// <summary>
        /// 특정 위치로 이동 애니메이션 (지정된 시간 사용)
        /// </summary>
        public Tween AnimateMoveTo(Vector3 targetPos, float duration, Action onComplete = null)
        {
            StopAnimation();
            
            targetPos.z = transform.position.z;
            
            _currentTween = transform.DOMove(targetPos, duration)
                .SetEase(moveEase)
                .OnComplete(() => onComplete?.Invoke());
            
            return _currentTween;
        }
        
        /// <summary>
        /// 공격 애니메이션: 대상 방향으로 찌르기 후 복귀, 그 후 대미지 콜백
        /// </summary>
        /// <param name="targetPos">공격 대상 위치</param>
        /// <param name="onAttackHit">찌르기가 대상에 닿았을 때 콜백 (대미지 처리)</param>
        /// <param name="onComplete">전체 공격 애니메이션 완료 콜백</param>
        public void AnimateAttack(Vector3 targetPos, Action onAttackHit, Action onComplete = null)
        {
            StopAnimation();
            
            Vector3 originalPos = transform.position;
            targetPos.z = originalPos.z;
            
            // 찌르기 위치 계산 (대상 방향으로 lungeDistance만큼)
            Vector3 direction = (targetPos - originalPos).normalized;
            Vector3 lungePos = originalPos + direction * (Vector3.Distance(originalPos, targetPos) * lungeDistance);
            
            _currentSequence = DOTween.Sequence();
            
            // 1. 찌르기 (대상 방향으로 전진)
            _currentSequence.Append(transform.DOMove(lungePos, attackLungeDuration).SetEase(attackMoveEase));
            
            // 2. 대미지 처리 콜백
            _currentSequence.AppendCallback(() => onAttackHit?.Invoke());
            
            // 3. 원래 위치로 복귀
            _currentSequence.Append(transform.DOMove(originalPos, attackReturnDuration).SetEase(attackReturnEase));
            
            // 4. 완료 콜백
            _currentSequence.AppendCallback(() => onComplete?.Invoke());
        }
        
        /// <summary>
        /// 대상 위치로 이동 (적을 잡은 후 이동용)
        /// </summary>
        public Tween AnimateMoveToTarget(Vector3 targetPos, Action onComplete = null)
        {
            targetPos.z = transform.position.z;
            
            _currentTween = transform.DOMove(targetPos, moveDuration)
                .SetEase(moveEase)
                .OnComplete(() => onComplete?.Invoke());
            
            return _currentTween;
        }
        
        private void OnDestroy()
        {
            StopAnimation();
        }
    }
}
