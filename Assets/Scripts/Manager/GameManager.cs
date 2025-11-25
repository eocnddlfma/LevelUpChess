using UnityEngine;
using UnityEngine.SceneManagement;
using Events;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public BoardGenerator boardGenerator;
    public InputManager inputManager;

    private ChessPiece lastMovedPiece;
    private Vector2Int lastMoveFrom;
    private Vector2Int lastMoveTo;

    public ChessPiece LastMovedPiece => lastMovedPiece;
    public Vector2Int LastMoveFrom => lastMoveFrom;
    public Vector2Int LastMoveTo => lastMoveTo;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        ValidateComponents();
    }

    private void Start()
    {
        // 네트워크 전용 - NetworkGameManager가 턴 관리
        Bus<GameOverEvent>.OnEvent += OnGameOver;
    }

    private void OnDisable()
    {
        Bus<GameOverEvent>.OnEvent -= OnGameOver;
    }

    private void ValidateComponents()
    {
        if (boardGenerator == null)
            boardGenerator = FindFirstObjectByType<BoardGenerator>();
        if (boardGenerator == null)
            Debug.LogError("[GameManager] No BoardGenerator found");

        if (inputManager == null)
            inputManager = FindFirstObjectByType<InputManager>();
        if (inputManager == null)
            Debug.LogError("[GameManager] No InputManager found");

        // 네트워크 전용 - NetworkMovementManager 확인
        var networkMovement = FindFirstObjectByType<NetworkMovementManager>();
        if (networkMovement == null)
            Debug.LogError("[GameManager] No NetworkMovementManager found - Network mode only!");
    }

    public void RecordLastMove(ChessPiece piece, Vector2Int from, Vector2Int to)
    {
        lastMovedPiece = piece;
        lastMoveFrom = from;
        lastMoveTo = to;
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
