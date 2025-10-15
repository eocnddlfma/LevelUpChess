using UnityEngine;

/// <summary>
/// 향후 턴 관리, 피스 선택, 이동/공격 로직을 추가할 GameManager 기본 골격.
/// 현재는 BoardGenerator를 찾아서 참조하는 기본 기능만 제공합니다.
/// </summary>
public class GameManager : MonoBehaviour
{
    public BoardGenerator boardGenerator;

    private void Awake()
    {
        if (boardGenerator == null)
            // FindFirstObjectByType replaces the deprecated FindObjectOfType API.
            // It returns the first matching instance in the scene and is the recommended replacement.
            boardGenerator = Object.FindFirstObjectByType<BoardGenerator>();

        if (boardGenerator == null)
            Debug.LogError("No BoardGenerator found in scene. Attach GameManager to scene and assign or add a BoardGenerator.");
    }
}
