using UnityEngine;
using UnityEngine.SceneManagement;
using Events;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public BoardGenerator boardGenerator;
    public InputManager inputManager;
    public MovementManager movementManager;

    private Team currentTeam = Team.White;
    private ChessPiece lastMovedPiece;
    private Vector2Int lastMoveFrom;
    private Vector2Int lastMoveTo;
    private bool isGameOver = false;
    private Team winnerTeam;

    public Team CurrentTeam => currentTeam;
    public ChessPiece LastMovedPiece => lastMovedPiece;
    public Vector2Int LastMoveFrom => lastMoveFrom;
    public Vector2Int LastMoveTo => lastMoveTo;
    public bool IsGameOver => isGameOver;
    public Team WinnerTeam => winnerTeam;

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
        Bus<TurnChangedEvent>.Raise(new TurnChangedEvent { NewTeam = currentTeam });
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

        if (movementManager == null)
            movementManager = FindFirstObjectByType<MovementManager>();
        if (movementManager == null)
            Debug.LogError("[GameManager] No MovementManager found");
    }

    public void EndTurn()
    {
        currentTeam = currentTeam == Team.White ? Team.Black : Team.White;
        Bus<TurnChangedEvent>.Raise(new TurnChangedEvent { NewTeam = currentTeam });
    }

    public void RecordLastMove(ChessPiece piece, Vector2Int from, Vector2Int to)
    {
        lastMovedPiece = piece;
        lastMoveFrom = from;
        lastMoveTo = to;
    }

    private void OnGameOver(GameOverEvent eventData)
    {
        isGameOver = true;
        winnerTeam = eventData.WinnerTeam;
    }

    public void Replay()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
