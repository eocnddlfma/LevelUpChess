using UnityEngine;
using UnityEngine.SceneManagement;
using LevelUpChess.Events;
using LevelUpChess.Pieces;

namespace LevelUpChess.Managers
{
    public class GameManager : MonoBehaviour
    {
        private void Start()
        {
            Bus<GameOverEvent>.OnEvent += OnGameOver;
        }

        private void OnDisable()
        {
            Bus<GameOverEvent>.OnEvent -= OnGameOver;
        }

        private void OnGameOver(GameOverEvent eventData)
        {
            Debug.Log($"[GameManager] Game Over! Winner: {eventData.WinnerTeam}");
        }

        public void Replay()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}
