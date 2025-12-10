using UnityEngine;
using LevelUpChess.Pieces;
using LevelUpChess.Interactables;
using LevelUpChess.UI;

namespace LevelUpChess.Board
{
    [RequireComponent(typeof(Collider2D))]
    public class Tile : MonoBehaviour, IClickable, ITooltipProvider
    {
        public Vector2Int coordinate;
        [SerializeField] private GameObject highlightObject;
        [SerializeField] private GameObject baseColorObject;
        [SerializeField] private GameObject moveableIndicator;
        [SerializeField] private GameObject attackableIndicator;
        public ChessPiece occupyingPiece;

        public ChessPiece OccupyingPiece
        {
            get => occupyingPiece;
            set => occupyingPiece = value;
        }

        void Awake()
        {
            if (highlightObject != null)
            {
                highlightObject.SetActive(false);
            }

            if (moveableIndicator != null)
            {
                moveableIndicator.SetActive(false);
            }

            if (attackableIndicator != null)
            {
                attackableIndicator.SetActive(false);
            }
        }
        public void SetColor(Color color)
        {
            if (baseColorObject != null)
            {
                baseColorObject.GetComponent<SpriteRenderer>().color = color;
            }
        }

        public void SetHighlight(bool show)
        {
            Debug.Log($"[Tile.SetHighlight] Coordinate: {coordinate}, Show: {show}, highlightObject: {highlightObject}");
            if (highlightObject != null)
            {
                highlightObject.SetActive(show);
            }
            else
            {
                Debug.LogWarning($"[Tile] highlightObject is null on tile {coordinate}");
            }
        }

        public void SetMoveable(bool show)
        {
            if (moveableIndicator != null)
            {
                moveableIndicator.SetActive(show);
            }
        }

        public void SetAttackable(bool show)
        {
            if (attackableIndicator != null)
            {
                attackableIndicator.SetActive(show);
            }
        }

        public void ClearIndicators()
        {
            SetHighlight(false);
            SetMoveable(false);
            SetAttackable(false);
        }

        public void OnClick()
        {
            // 타일 클릭 시 처리
        }

        // ========== ITooltipProvider 구현 ==========
        
        public string GetTooltipContent()
        {
            // 타일 위에 기물이 있으면 기물 정보 반환
            if (occupyingPiece != null)
            {
                return occupyingPiece.GetTooltipContent();
            }
            
            // 빈 타일이면 null 반환 (툴팅 표시 안함)
            return null;
        }
        
        public Team? GetTooltipTeam()
        {
            // 타일 위에 기물이 있으면 기물의 팀 반환
            return occupyingPiece?.Team;
        }
    }
}
