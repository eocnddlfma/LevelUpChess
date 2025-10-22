using System.Collections.Generic;
using Events;
using UnityEngine;

public struct ClickableSelectedEvent : IEvent
{
    public MonoBehaviour Clickable;
}

public struct InteractableSelectedEvent : IEvent
{
    public MonoBehaviour Interactable;
}

public struct InteractableDragBegunEvent : IEvent
{
    public MonoBehaviour Interactable;
}

public struct InteractableDragEndedEvent : IEvent
{
    public MonoBehaviour Interactable;
}

public struct PieceSelectedEvent : IEvent
{
    public ChessPiece Piece;
    public List<Move> AvailableMoves;
}

public struct SelectionClearedEvent : IEvent
{
}

public struct TurnChangedEvent : IEvent
{
    public Team NewTeam;
}

public struct GameOverEvent : IEvent
{
    public Team WinnerTeam;
}
