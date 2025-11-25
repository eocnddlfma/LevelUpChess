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
    public bool IsRematch; // 리매치인 경우 UI를 숨기기 위한 플래그
}
