namespace LevelUpChess.Upgrades
{
    /// <summary>
    /// Upgrade categories.
    /// </summary>
    public enum UpgradeType
    {
        Movement,
        Stat,
        Ability,
        Global
    }
    
    /// <summary>
    /// Upgrade application target.
    /// </summary>
    public enum UpgradeTarget
    {
        Piece,
        Player,
        AllPieces
    }
    
    /// <summary>
    /// Piece type filter.
    /// </summary>
    public enum PieceTypeFilter
    {
        Any,
        Pawn,
        Rook,
        Knight,
        Bishop,
        Queen,
        King
    }
    
    /// <summary>
    /// Ability execution trigger.
    /// </summary>
    public enum AbilityTrigger
    {
        OnAttackStart,
        OnAttackHit,
        OnHit,
        OnKill,
        OnDamaged,
        OnAllyHit,
        OnAllyDeath,
        OnBeforeMove,
        OnAfterMove,
        OnTurnStart,
        OnTurnEnd,
        OnDeath,
        Passive
    }
}
