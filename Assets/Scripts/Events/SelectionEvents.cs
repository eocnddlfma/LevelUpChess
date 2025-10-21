using System.Collections.Generic;
using UnityEngine;
using Code.CoreSystem;

public class ClickableSelectedEvent : IEvent
{
    public MonoBehaviour Clickable;
}

public class InteractableSelectedEvent : IEvent
{
    public MonoBehaviour Interactable;
}

public class InteractableDragBegunEvent : IEvent
{
    public MonoBehaviour Interactable;
}

public class InteractableDragEndedEvent : IEvent
{
    public MonoBehaviour Interactable;
}

public class PieceSelectedEvent : IEvent
{
    public ChessPiece Piece;
    public List<Move> AvailableMoves;
}

public class SelectionClearedEvent : IEvent
{
}

public class TurnChangedEvent : IEvent
{
    public Team NewTeam;
}
