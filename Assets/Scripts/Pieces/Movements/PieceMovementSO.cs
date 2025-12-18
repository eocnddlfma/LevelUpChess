using System.Collections.Generic;
using UnityEngine;
using LevelUpChess.Core;
using LevelUpChess.Board;
using LevelUpChess.Upgrades;

namespace LevelUpChess.Pieces
{
    /// <summary>
    /// 이동 타입
    /// </summary>
    public enum MoveType : byte
    {
        Normal = 0,      // 이동 + 공격 가능
        MoveOnly = 1,    // 이동만 가능
        AttackOnly = 2   // 공격만 가능
    }

    public abstract class PieceMovementSO : ScriptableObject
    {
        [Header("Movement Type")]
        [SerializeField] protected MoveType moveType = MoveType.Normal;
        
        [Header("Piece Filter")]
        [SerializeField] protected PieceTypeFilter pieceFilter = PieceTypeFilter.Any;
        
        public MoveType MoveType => moveType;
        public PieceTypeFilter PieceFilter => pieceFilter;
        
        public abstract List<Move> GetAvailableMoves(ChessPiece piece);

        /// <summary>
        /// MoveType에 따라 이동/공격 필터링
        /// </summary>
        protected List<Move> FilterByMoveType(List<Move> allMoves)
        {
            switch (moveType)
            {
                case Pieces.MoveType.MoveOnly:
                    // 이동만 가능 (공격 제거)
                    allMoves.RemoveAll(m => m.isCapture);
                    break;
                    
                case Pieces.MoveType.AttackOnly:
                    // 공격만 가능 (이동 제거)
                    allMoves.RemoveAll(m => !m.isCapture);
                    break;
                    
                case Pieces.MoveType.Normal:
                default:
                    // 이동 + 공격 모두 가능
                    break;
            }
            
            return allMoves;
        }

        protected List<Move> GetSlidingMoves(ChessPiece piece, Vector2Int[] directions)
        {
            var moves = new List<Move>();
            if (piece.CurrentTile == null) return moves;

            Vector2Int pos = piece.CurrentTile.coordinate;
            var boardManager = ServiceLocator.Get<BoardManager>();

            foreach (var d in directions)
            {
                Vector2Int cur = pos + d;
                while (true)
                {
                    var t = boardManager.GetTileAt(cur);
                    if (t == null) break;
                    
                    if (t.OccupyingPiece == null)
                    {
                        moves.Add(new Move(pos, cur));
                    }
                    else
                    {
                        // 폰 뛰어넘기 체크 (Bishop 능력)
                        if (piece.CanJumpOverPawns && t.OccupyingPiece.PieceType == PieceType.Pawn)
                        {
                            // 폰을 뛰어넘고 계속 진행
                            cur += d;
                            continue;
                        }
                        
                        if (t.OccupyingPiece.Team != piece.Team || piece.CanAttackAllies)
                            moves.Add(new Move(pos, cur) { isCapture = true });
                        break;
                    }
                    cur += d;
                }
            }

            return FilterByMoveType(moves);
        }

        protected List<Move> GetJumpingMoves(ChessPiece piece, Vector2Int[] offsets)
        {
            var moves = new List<Move>();
            if (piece.CurrentTile == null) return moves;

            Vector2Int pos = piece.CurrentTile.coordinate;
            var boardManager = ServiceLocator.Get<BoardManager>();

            foreach (var offset in offsets)
            {
                Vector2Int target = pos + offset;
                var t = boardManager.GetTileAt(target);
                if (t != null)
                {
                    if (t.OccupyingPiece == null)
                    {
                        moves.Add(new Move(pos, target));
                    }
                    else if (t.OccupyingPiece.Team != piece.Team || piece.CanAttackAllies)
                    {
                        moves.Add(new Move(pos, target) { isCapture = true });
                    }
                }
            }

            return FilterByMoveType(moves);
        }

#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            // 서브클래스에서 pieceFilter 설정
        }
#endif
    }
}
