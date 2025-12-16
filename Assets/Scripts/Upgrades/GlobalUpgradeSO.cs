using UnityEngine;
using LevelUpChess.Pieces;

namespace LevelUpChess.Upgrades
{
    /// <summary>
    /// 글로벌 업그레이드 베이스 클래스
    /// </summary>
    public abstract class GlobalUpgradeSO : UpgradeBaseSO
    {
        [Header("Global Upgrade Settings")]
        [Tooltip("적용 대상 팀")]
        [SerializeField] protected Team targetTeam = Team.White;

        /// <summary>
        /// 글로벌 효과 적용 (모든 해당 팀 기물에게)
        /// </summary>
        public abstract void ApplyGlobalEffect(Team team);

        /// <summary>
        /// 글로벌 효과 제거
        /// </summary>
        public abstract void RemoveGlobalEffect(Team team);

        /// <summary>
        /// 새로운 기물이 추가될 때 호출
        /// </summary>
        public virtual void OnPieceAdded(ChessPiece piece) { }

        /// <summary>
        /// 기물이 제거될 때 호출
        /// </summary>
        public virtual void OnPieceRemoved(ChessPiece piece) { }

        /// <summary>
        /// 팀 전체에 업그레이드 적용 (피스 리스트 버전) - 서브클래스에서 오버라이드 가능
        /// </summary>
        public virtual void ApplyToTeam(int teamId, System.Collections.Generic.List<ChessPiece> pieces)
        {
        }

        /// <summary>
        /// 팀에서 업그레이드 제거 (피스 리스트 버전) - 서브클래스에서 오버라이드 가능
        /// </summary>
        public virtual void RemoveFromTeam(int teamId, System.Collections.Generic.List<ChessPiece> pieces)
        {
        }

        /// <summary>
        /// UpgradeBaseSO.Apply(ChessPiece) 구현 - 글로벌 업그레이드인 경우 팀 단위로 적용
        /// </summary>
        public override void Apply(ChessPiece piece)
        {
            if (piece == null) return;
            ApplyGlobalEffect(piece.Team);
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
        }

        protected override void SetDefaultNameAndDescription()
        {
            upgradeType = UpgradeType.Global;
        }
#endif
    }
}
