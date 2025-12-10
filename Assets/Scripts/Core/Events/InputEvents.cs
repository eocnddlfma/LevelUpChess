using UnityEngine;
using LevelUpChess.Interactables;

namespace LevelUpChess.Events
{
    /// <summary>
    /// 클릭 가능한 객체가 선택되었을 때 발생
    /// </summary>
    public struct ClickableSelectedEvent : IEvent
    {
        public IClickable Clickable;
    }

    /// <summary>
    /// 드래그 시작 시 발생
    /// </summary>
    public struct InteractableDragBegunEvent : IEvent
    {
        public Interactable Draggable;
    }

    /// <summary>
    /// 드래그 종료 시 발생
    /// </summary>
    public struct InteractableDragEndedEvent : IEvent
    {
        public Interactable Draggable;
    }

    /// <summary>
    /// 마우스 호버 시작 시 발생
    /// </summary>
    public struct MouseHoverBeganEvent : IEvent
    {
        public GameObject Target;
    }

    /// <summary>
    /// 마우스 호버 종료 시 발생
    /// </summary>
    public struct MouseHoverEndedEvent : IEvent
    {
        public GameObject Target;
    }
}
