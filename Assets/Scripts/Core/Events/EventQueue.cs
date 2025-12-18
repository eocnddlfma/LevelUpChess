using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LevelUpChess.Events;
using LevelUpChess.UI;
using LevelUpChess.Pieces;
using LevelUpChess.Managers;
using LevelUpChess.Upgrades;

namespace LevelUpChess.Core
{
    public class EventQueue : MonoBehaviour
    {
        private static EventQueue instance;
        public static EventQueue Instance => instance ??= FindObjectOfType<EventQueue>();

        private Queue<IEvent> eventQueue = new Queue<IEvent>();
        private bool isProcessing = false;

        private void Awake()
        {
            if (instance == null) instance = this;
        }

        private void OnEnable()
        {
        }

        private void OnDisable()
        {
        }

        public void Enqueue(IEvent evt)
        {
            eventQueue.Enqueue(evt);
            Debug.Log($"[EventQueue] Enqueued: {evt.GetType().Name}, Queue count: {eventQueue.Count}");
            if (!isProcessing) ProcessNext();
        }

        private void ProcessNext()
        {
            if (eventQueue.Count == 0) return;

            isProcessing = true;
            IEvent currentEvent = eventQueue.Dequeue();
            Debug.Log($"[EventQueue] Processing: {currentEvent.GetType().Name}, Remaining: {eventQueue.Count}");

            // 이벤트 타입에 따라 처리 (예: UI 표시)
            if (currentEvent is PieceLevelUpEvent levelUpEvent)
            {
                Bus<PieceLevelUpEvent>.Raise(levelUpEvent);
            }
            else if (currentEvent is PlayerLevelUpEvent playerlevelUpEvent)
            {
                // 플레이어 레벨업은 UpgradeManager를 통해 업그레이드 선택 UI를 띄운다
                var upgradeManager = UpgradeManager.Instance;
                if (upgradeManager != null)
                {
                    upgradeManager.OfferUpgradeSelection(playerlevelUpEvent);
                    // UI가 닫힐 때 UpgradeManager.OnPlayerUpgradeSelectionCompletedClientRpc()
                    // 안에서 EventQueue.OnUIClosed()를 호출하여 다음 이벤트를 처리한다.
                }
                else
                {
                    // UpgradeManager가 없으면 기존처럼 이벤트만 브로드캐스트하고 바로 다음으로
                    Bus<PlayerLevelUpEvent>.Raise(playerlevelUpEvent);
                    OnUIClosed();
                }
            }
            else
            {
                // 다른 이벤트는 즉시 Bus로 전달
                RaiseEvent(currentEvent);
                OnUIClosed(); // 바로 다음으로
            }
        }

        private void ShowLevelUpUI(PieceLevelUpEvent evt)
        {
            // 레벨업 팝업 생략하고 바로 업그레이드 선택으로
            RaiseEvent(evt);
            //OnUIClosed();
        }
        
        private void ShowMessageUI(ShowMessageEvent evt)
        {
            // 메시지 UI 표시
            var messageUI = ServiceLocator.Get<GameMessageUI>();
            if (messageUI != null)
            {
                messageUI.ShowMessage(evt.Message, 2f, () => {
                    RaiseEvent(evt);
                    OnUIClosed();
                });
            }
            else
            {
                Debug.LogWarning("[EventQueue] GameMessageUI not found!");
                RaiseEvent(evt);
                OnUIClosed();
            }
        }

        private void RaiseEvent(IEvent evt)
        {
            // 이벤트 타입에 따라 Bus.Raise 호출
            if (evt is PieceLevelUpEvent levelUpEvent)
            {
                Bus<PieceLevelUpEvent>.Raise(levelUpEvent);
            }
            else if (evt is ShowMessageEvent messageEvent)
            {
                Bus<ShowMessageEvent>.Raise(messageEvent);
            }
            // 다른 이벤트 추가 가능
        }

        public void OnUIClosed()
        {
            isProcessing = false;
            Debug.Log("[EventQueue] UI Closed, processing next event");
            ProcessNext(); // 다음 이벤트 처리
        }
    }
}