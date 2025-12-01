using UnityEngine;
using UnityEngine.EventSystems;
using LevelUpChess.Events;

namespace LevelUpChess.Interactables
{
    /// <summary>
    /// 상호작용 가능한 오브젝트의 기본 클래스
    /// 클릭 및 드래그 기능 제공
    /// </summary>
    public abstract class Interactable : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("Drag Settings")]
        [SerializeField] protected bool isDraggable = false;
        
        private Vector3 dragOffset;
        private Vector3 originalPosition;

        public virtual void OnPointerClick(PointerEventData eventData)
        {
            if (this is IClickable clickable)
            {
                Bus<ClickableSelectedEvent>.Raise(new ClickableSelectedEvent { Clickable = clickable });
            }
        }

        public virtual void OnBeginDrag(PointerEventData eventData)
        {
            if (!isDraggable) return;
            
            originalPosition = transform.position;
            dragOffset = transform.position - GetWorldMousePosition(eventData);
            
            Bus<InteractableDragBegunEvent>.Raise(new InteractableDragBegunEvent { Draggable = this });
        }

        public virtual void OnDrag(PointerEventData eventData)
        {
            if (!isDraggable) return;
            
            transform.position = GetWorldMousePosition(eventData) + dragOffset;
        }

        public virtual void OnEndDrag(PointerEventData eventData)
        {
            if (!isDraggable) return;
            
            Bus<InteractableDragEndedEvent>.Raise(new InteractableDragEndedEvent { Draggable = this });
        }

        /// <summary>
        /// 드래그 취소 시 원래 위치로 복귀
        /// </summary>
        protected void ResetPosition()
        {
            transform.position = originalPosition;
        }

        private Vector3 GetWorldMousePosition(PointerEventData eventData)
        {
            Vector3 mouseScreenPos = eventData.position;
            mouseScreenPos.z = 10f;
            return Camera.main.ScreenToWorldPoint(mouseScreenPos);
        }
    }
}
