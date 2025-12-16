using UnityEngine;
using LevelUpChess.Pieces;

namespace LevelUpChess.Upgrades
{
    /// <summary>
    /// 행마법(이동 방식) 업그레이드 ScriptableObject
    /// </summary>
    [CreateAssetMenu(fileName = "NewMovementUpgrade", menuName = "LevelUpChess/Upgrades/Movement Upgrade")]
    public class MovementUpgradeSO : UpgradeBaseSO
    {
        [Header("행마법 설정")]
        [SerializeField] private PieceMovementSO movementToAdd;
        [SerializeField] private bool replaceExisting = false;
        
        public PieceMovementSO MovementToAdd => movementToAdd;
        public bool ReplaceExisting => replaceExisting;
        
        public override void Apply(ChessPiece piece)
        {
            if (piece == null || movementToAdd == null) return;
            
            piece.AddMovementStrategy(movementToAdd, replaceExisting);
            Debug.Log($"[MovementUpgrade] {upgradeName} applied to {piece.name}");
        }
        
        public override void Remove(ChessPiece piece)
        {
            if (piece == null || movementToAdd == null) return;
            
            piece.RemoveMovementStrategy(movementToAdd);
        }

        public void Initialize(PieceMovementSO movement)
        {
            movementToAdd = movement;
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }
        
#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
        }

        protected override void SetDefaultNameAndDescription()
        {
            upgradeType = UpgradeType.Movement;
        }
#endif
    }
}
