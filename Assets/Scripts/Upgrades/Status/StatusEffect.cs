using System;
using UnityEngine;
using LevelUpChess.Pieces;

namespace LevelUpChess.Upgrades.Status
{
    /// <summary>
    /// 기본 상태이상 타입
    /// </summary>
    public abstract class StatusEffect
    {
        protected ChessPiece Owner { get; private set; }
        protected ChessPiece Source { get; private set; }
        protected int RemainingTurns { get; set; }
        protected string SourceName { get; private set; }

        public StatusEffect(int turns, ChessPiece source = null, string sourceName = null)
        {
            RemainingTurns = turns;
            Source = source;
            SourceName = sourceName;
        }

        public void SetOwner(ChessPiece owner)
        {
            Owner = owner;
        }

        public virtual void OnApply()
        {
            // override
        }

        public virtual void OnTick()
        {
            // override - called each tick (e.g., turn start)
            RemainingTurns = Math.Max(0, RemainingTurns - 1);
        }

        public virtual void OnRemove()
        {
            // override
        }

        public bool IsExpired => RemainingTurns <= 0;
    }
}
