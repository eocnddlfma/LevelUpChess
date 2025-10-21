using UnityEngine;

public struct Move
{
    public Vector2Int from;
    public Vector2Int to;
    public bool isCapture;
    public bool isPromotion;
    public bool isEnPassant;
    public Vector2Int enPassantCapturePos; // 앙파상으로 제거할 폰의 위치
    public bool isCastling;
    public Vector2Int rookFromPos; // 캐슬링 시 룩의 원래 위치
    public Vector2Int rookToPos;   // 캐슬링 시 룩의 이동할 위치

    public Move(Vector2Int f, Vector2Int t)
    {
        from = f;
        to = t;
        isCapture = false;
        isPromotion = false;
        isEnPassant = false;
        enPassantCapturePos = Vector2Int.zero;
        isCastling = false;
        rookFromPos = Vector2Int.zero;
        rookToPos = Vector2Int.zero;
    }
}
