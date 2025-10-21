using System;
using UnityEngine;
using UnityEngine.EventSystems;
using Code.CoreSystem;

public abstract class Interactable : Clickable, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private Vector3 dragOffset;
    private Transform dragTransform;

    protected virtual void Start()
    {
        dragTransform = transform;
    }

    public virtual void OnBeginDrag(PointerEventData eventData)
    {
        if (dragTransform == null) 
            dragTransform = transform;

        // 드래그 시작 시 마우스와 객체 간의 오프셋 계산
        dragOffset = dragTransform.position - GetWorldMousePosition(eventData);
        
        Bus<InteractableDragBegunEvent>.Raise(new InteractableDragBegunEvent { Interactable = this });
    }

    public virtual void OnDrag(PointerEventData eventData)
    {
        if (dragTransform == null)
            dragTransform = transform;

        // 마우스 위치에 따라 객체 이동
        dragTransform.position = GetWorldMousePosition(eventData) + dragOffset;
    }

    public virtual void OnEndDrag(PointerEventData eventData)
    {
        Bus<InteractableDragEndedEvent>.Raise(new InteractableDragEndedEvent { Interactable = this });
    }

    private Vector3 GetWorldMousePosition(PointerEventData eventData)
    {
        Vector3 mouseScreenPos = eventData.position;
        mouseScreenPos.z = 10f; // 카메라에서의 거리 설정
        return Camera.main.ScreenToWorldPoint(mouseScreenPos);
    }
}
