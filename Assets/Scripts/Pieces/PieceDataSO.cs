using UnityEngine;

namespace LevelUpChess.Pieces
{
    /// <summary>
    /// 체스 기물의 기본 데이터를 정의하는 ScriptableObject
    /// Inspector에서 기물 타입별로 설정 가능
    /// </summary>
    [CreateAssetMenu(fileName = "PieceData", menuName = "Chess/Piece Data")]
    public class PieceDataSO : ScriptableObject
    {
        [Header("기본 정보")]
        [SerializeField] private PieceType pieceType;
        [SerializeField] private string displayName;
        
        [Header("스탯")]
        [SerializeField] private int maxHealth = 1;
        [SerializeField] private int attackPower = 1;
        
        [Header("레벨업 시 증가 스탯")]
        [SerializeField] private int healthPerLevel = 1;
        [SerializeField] private int attackPerLevel = 1;
        
        [Header("기물 가치 (경험치)")]
        [Tooltip("이 기물을 처치하면 얻는 경험치 (폰=1, 비숍/나이트=3, 뢩=5, 퀘=9)")]
        [SerializeField] private int pieceValue = 1;
        
        [Header("이동")]
        [SerializeField] private float moveDuration = 0.1f;
        [SerializeField] private PieceMovement[] movementStrategies;
        
        
        // 읽기 전용 프로퍼티
        public PieceType PieceType => pieceType;
        public string DisplayName => displayName;
        public int MaxHealth => maxHealth;
        public int AttackPower => attackPower;
        public int HealthPerLevel => healthPerLevel;
        public int AttackPerLevel => attackPerLevel;
        public int PieceValue => pieceValue;
        public float MoveDuration => moveDuration;
        public PieceMovement[] MovementStrategies => movementStrategies;
    }
}
