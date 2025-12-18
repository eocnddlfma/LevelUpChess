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
            
            // 자동으로 PieceMovementSO 할당
            if (movementToAdd == null)
            {
                string className = this.GetType().Name;
                string path = GetMovementSOPath(className);
                if (!string.IsNullOrEmpty(path))
                {
                    var movementSO = UnityEditor.AssetDatabase.LoadAssetAtPath<PieceMovementSO>(path);
                    if (movementSO != null)
                    {
                        movementToAdd = movementSO;
                        UnityEditor.EditorUtility.SetDirty(this);
                    }
                }
            }
        }

        private string GetMovementSOPath(string className)
        {
            // 클래스 이름 기반으로 PieceMovementSO 경로 반환
            switch (className)
            {
                // Pawn 업그레이드
                case "PawnBackstepUpgradeSO":
                    return "Assets/Scripts/Pieces/Movements/UpgradableMovements/Pawn/MovementBackstepMoveSO.asset";
                case "PawnDiagonalMoveUpgradeSO":
                    return "Assets/Scripts/Pieces/Movements/UpgradableMovements/Pawn/MovementDiagonalMoveSO.asset";
                case "PawnFrontAttackUpgradeSO":
                    return "Assets/Scripts/Pieces/Movements/UpgradableMovements/Pawn/MovementFrontAttackSO.asset";
                case "PawnLargerAttackSpaceUpgradeSO":
                    return "Assets/Scripts/Pieces/Movements/UpgradableMovements/Pawn/MovementLargerAttackSpaceSO.asset";
                case "PawnSidewayUpgradeSO":
                    return "Assets/Scripts/Pieces/Movements/UpgradableMovements/Pawn/MovementSidewaySO.asset";
                case "PawnTwoStepFrontUpgradeSO":
                    return "Assets/Scripts/Pieces/Movements/UpgradableMovements/Pawn/MovementTwoStepFrontSO.asset";
                
                // Bishop 업그레이드
                case "BishopKnightAttackUpgradeSO":
                    return "Assets/Scripts/Pieces/Movements/UpgradableMovements/Queen/MovementQueenKnightAttackSO.asset";
                case "BishopKnightMoveUpgradeSO":
                    return "Assets/Scripts/Pieces/Movements/UpgradableMovements/Queen/MovementQueenKnightMoveSO.asset";
                case "BishopReflectAttackUpgradeSO":
                    return "Assets/Scripts/Pieces/Movements/UpgradableMovements/Queen/MovementReflectAttackSO.asset";
                case "BishopRookAttackUpgradeSO":
                    return "Assets/Scripts/Pieces/Movements/UpgradableMovements/Bishop/MovementRookAttackSO.asset";
                case "BishopRookMoveUpgradeSO":
                    return "Assets/Scripts/Pieces/Movements/UpgradableMovements/Bishop/MovementRookMoveSO.asset";
                
                // 다른 기물 업그레이드도 추가 가능
                default:
                    return null;
            }
        }

        protected override void SetDefaultNameAndDescription()
        {
            upgradeType = UpgradeType.Movement;
        }
#endif
    }
}
