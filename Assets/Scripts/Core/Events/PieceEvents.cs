using System.Collections.Generic;
using LevelUpChess.Pieces;

namespace LevelUpChess.Events
{
    /// <summary>
    /// 체스 기물 선택 시 발생 (이동 가능한 위치 포함)
    /// </summary>
    public struct PieceSelectedEvent : IEvent
    {
        public ChessPiece Piece;
        public List<Move> AvailableMoves;
    }

    /// <summary>
    /// 선택 해제 시 발생
    /// </summary>
    public struct SelectionClearedEvent : IEvent { }

    /// <summary>
    /// 업그레이드 적용 시 발생
    /// </summary>
    public struct UpgradeAppliedEvent : IEvent
    {
        public ChessPiece Piece;
        public LevelUpChess.Upgrades.UpgradeBaseSO Upgrade;
    }

    /// <summary>
    /// 공격 성공 시 발생
    /// </summary>
    public struct AttackSuccessEvent : IEvent
    {
        public ChessPiece Attacker;
        public ChessPiece Target;
        public int DamageDealt;
        public bool TargetDied;
    }

    /// <summary>
    /// 기물 이동 시 발생
    /// </summary>
    public struct OnPieceMoved : IEvent
    {
        public ChessPiece Piece;
    }

    /// <summary>
    /// 턴 시작 시 발생
    /// </summary>
    public struct OnTurnStart : IEvent
    {
        public int TurnNumber;
    }

    /// <summary>
    /// 기물 공격 시 발생
    /// </summary>
    public struct OnPieceAttacked : IEvent
    {
        public ChessPiece Attacker;
        public ChessPiece Target;
    }
}
