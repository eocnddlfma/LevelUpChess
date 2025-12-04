using UnityEngine;

namespace LevelUpChess.UI
{
    public class PanelToggle : MonoBehaviour
    {
        [SerializeField] private GameObject targetPanel;

        public void TurnOn()
        {
            if (targetPanel != null)
                targetPanel.SetActive(true);
        }

        public void TurnOff()
        {
            if (targetPanel != null)
                targetPanel.SetActive(false);
        }
    }
}
