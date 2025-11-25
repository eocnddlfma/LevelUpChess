using UnityEngine;
using Unity.Netcode;

public struct Move : INetworkSerializable
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

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref from);
        serializer.SerializeValue(ref to);
        serializer.SerializeValue(ref isCapture);
        serializer.SerializeValue(ref isPromotion);
        serializer.SerializeValue(ref isEnPassant);
        serializer.SerializeValue(ref enPassantCapturePos);
        serializer.SerializeValue(ref isCastling);
        serializer.SerializeValue(ref rookFromPos);
        serializer.SerializeValue(ref rookToPos);
    }
}
