# 🔍 잠재적 네트워크 문제 분석

## 1. ⚠️ **Lobby 하트비트 문제 (심각도: 높음)**
**파일:** `UnityLobbyManager.cs`

**문제:**
- HOST가 Relay Code를 Lobby에 저장하지만, CLIENT 연결 후 계속 폴링하는 동안 Lobby 하트비트가 끊길 수 있음
- 네트워크 지연이나 브라우저 폴링 중에 `SendHeartbeatAsync()` 미실행 가능

**영향:**
- Lobby 자동 삭제 (기본 30초 타임아웃)
- CLIENT가 Relay Code를 받기 전에 Lobby가 사라질 수 있음

**해결책:**
- `PollForRelayCodeCoroutine()` 실행 중에도 하트비트 계속 실행
- 또는 Relay Code 수신 후 하트비트 재개

---

## 2. ⚠️ **WaitForRelayCode() 무한 루프 (심각도: 높음)**
**파일:** `ChessNetworkManager.cs` Line 402

**문제:**
```csharp
while (elapsed < timeout)
{
    string code = lobbyManager.GetRelayJoinCode();
    // ...
    elapsed += 0.1f;  // 시뮬레이션 시간만 증가 - 실제 시간과 다름!
}
```

- `elapsed` 시간이 실제 경과 시간과 무관
- 무한 루프 또는 타임아웃 동작 불명확
- CPU 낭비 + 느린 폴링

**해결책:**
- 실제 경과 시간 추적: `Time.realtimeSinceStartup` 사용

---

## 3. ⚠️ **이중 폴링 구조 (심각도: 중간)**
**파일:** `UnityLobbyManager.cs` + `ChessNetworkManager.cs`

**문제:**
- `UnityLobbyManager.WaitForRelayCodeAsync()` → 즉시 반환
- `PollForRelayCodeCoroutine()` 코루틴 시작
- `ChessNetworkManager.WaitForRelayCode()` → `elapsed += 0.1f` 루프

2개의 다른 코루틴이 동시에 Relay Code를 폴링 중

**해결책:**
- 하나의 단일 폴링 로직으로 통합

---

## 4. ⚠️ **Lobby 타임아웃 설정 없음 (심각도: 중간)**
**파일:** `UnityLobbyManager.cs` Line 168+

**문제:**
```csharp
private async Task CreateLobbyAsync()
{
    // max idle seconds 설정 없음 - 기본 30초
    // IsPrivate = false - 누구나 찾을 수 있음
}
```

- Lobby가 30초 후 자동 삭제 (기본값)
- 늦게 들어오는 플레이어를 위한 대기 시간 부족

**해결책:**
- `isPrivate: true` 또는 더 긴 `maxIdleSeconds` 설정

---

## 5. ⚠️ **CLIENT 연결 확인 없음 (심각도: 높음)**
**파일:** `ChessNetworkManager.cs` 

**문제:**
- CLIENT가 실제로 HOST에 연결되었는지 확인하지 않음
- Relay Code를 받았다고 해서 Netcode 연결이 성공한 것은 아님
- `StartClient()` 성공 ≠ 실제 HOST 연결 완료

**해결책:**
- `NetworkManager.Singleton.ConnectedClients.Count >= 2` 확인 전까지 대기
- 또는 `OnClientConnectedCallback` 이벤트로 확인

---

## 6. ⚠️ **씬 로드 타임아웃 (심각도: 중간)**
**파일:** `ChessNetworkManager.cs` Line 493+

**문제:**
```csharp
private IEnumerator WaitForClientsAndLoadScene()
{
    while (NetworkManager.Singleton.ConnectedClients.Count < MAX_PLAYERS)
    {
        yield return new WaitForSeconds(0.5f);
        // 무한 대기...
    }
}
```

- CLIENT가 도착하지 않으면 무한 대기
- 사용자 경험 나쁨

**해결책:**
- 타임아웃 추가 (예: 60초)
- 사용자에게 명확한 상태 알림

---

## 7. ⚠️ **에러 복구 로직 없음 (심각도: 높음)**
**전체 파일**

**문제:**
- Relay 설정 실패 → 에러 메시지만 표시
- CLIENT는 계속 Relay Code 기다림 (타임아웃 30초)
- Lobby 참가 실패 → 명확한 재시도 방법 없음

**해결책:**
- "Quick Match 다시 시도" 버튼
- 에러 상태에서 Lobby 자동 정리

---

## 8. ⚠️ **브라우저 새로고침 대응 (심각도: 높음)**
**파일:** `ChessNetworkManager.cs` Line 109

**문제:**
- `#if !UNITY_WEBGL` 추가했지만, 이미 로비 데이터가 있을 수 있음
- 이전 SESSION ID와 새 SESSION ID 불일치 가능

**해결책:**
- 브라우저 새로고침 감지 후 Lobby 정리
- 또는 PlayerId 일관성 검증

---

## 9. ⚠️ **PlayerPrefs WebGL 비지원 (심각도: 낮음)**
**파일:** `ChessNetworkManager.cs` Line 70

**주석 처리:**
```csharp
// #if UNITY_EDITOR
//     playerId = System.Guid.NewGuid().ToString().Substring(0, 8);
// #else
//     playerId = PlayerPrefs.GetString("PlayerId");  // ← WebGL에서 비지원
```

**현재:** 매번 새로운 `playerId` 생성 ✓ (올바름)

---

## 10. ⚠️ **메모리 누수 우려 (심각도: 낮음)**
**파일:** `ChessNetworkManager.cs`, `UnityLobbyManager.cs`

**문제:**
- `OnClientConnectedCallback` 구독 해제 안 함
- `OnClientDisconnectCallback` 구독 해제 안 함
- Scene 전환 시 이벤트 리스너가 남아있을 수 있음

**해결책:**
```csharp
private void OnDestroy()
{
    if (NetworkManager.Singleton != null)
    {
        NetworkManager.Singleton.OnClientConnectedCallback -= ...;
        NetworkManager.Singleton.OnClientDisconnectCallback -= ...;
    }
}
```

---

## 📊 우선순위 순서

| 순위 | 문제 | 심각도 | 발생 확률 |
|------|------|--------|---------|
| 1️⃣ | Lobby 하트비트 끊김 | 🔴 높음 | 높음 |
| 2️⃣ | WaitForRelayCode() 타임아웃 동작 불명확 | 🔴 높음 | 높음 |
| 3️⃣ | CLIENT 연결 확인 없음 | 🔴 높음 | 중간 |
| 4️⃣ | 이중 폴링 구조 | 🟡 중간 | 중간 |
| 5️⃣ | 에러 복구 로직 없음 | 🔴 높음 | 낮음 |
| 6️⃣ | 씬 로드 무한 대기 | 🟡 중간 | 낮음 |
| 7️⃣ | 브라우저 새로고침 대응 | 🔴 높음 | 중간 |
| 8️⃣ | Lobby 타임아웃 설정 | 🟡 중간 | 중간 |
| 9️⃣ | 메모리 누수 | 🟢 낮음 | 중간 |
| 🔟 | PlayerPrefs | 🟢 낮음 | 매우 낮음 |

---

## 🔧 추천 즉시 수정 사항

### **수정 1: WaitForRelayCode() 실제 시간 추적**
```csharp
private async Task<string> WaitForRelayCode()
{
    float timeout = 30f;
    float startTime = Time.realtimeSinceStartup;  // ← 변경
    
    while (Time.realtimeSinceStartup - startTime < timeout)
    {
        string code = lobbyManager.GetRelayJoinCode();
        if (!string.IsNullOrEmpty(code))
            return code;
        
        // 약간의 지연 추가 (CPU 낭비 방지)
        await Task.Delay(100);
    }
    
    return null;
}
```

### **수정 2: CLIENT 연결 확인**
```csharp
private IEnumerator WaitForClientsAndLoadScene()
{
    float timeout = 60f;
    float startTime = Time.time;
    
    while (NetworkManager.Singleton.ConnectedClients.Count < MAX_PLAYERS 
           && Time.time - startTime < timeout)
    {
        yield return new WaitForSeconds(0.5f);
    }
    
    if (NetworkManager.Singleton.ConnectedClients.Count < MAX_PLAYERS)
    {
        Debug.LogError("[ChessNetwork] Timeout: Not all clients connected");
        OnError?.Invoke("Timeout waiting for all clients to connect");
        yield break;
    }
    
    // 씬 로드...
}
```

### **수정 3: 이벤트 구독 해제**
```csharp
private void OnDestroy()
{
    if (NetworkManager.Singleton != null)
    {
        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnect;
    }
    
    if (lobbyManager != null)
    {
        lobbyManager.OnMatchFound -= OnLobbyMatchFound;
        lobbyManager.OnError -= OnLobbyError;
    }
}
```
