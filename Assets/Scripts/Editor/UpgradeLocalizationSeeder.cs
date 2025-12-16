using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using LevelUpChess.Upgrades;

namespace LevelUpChess.Editor
{
    /// <summary>
    /// 업그레이드 이름/설명 자동 채우기(에디터 메뉴).
    /// </summary>
    public static class UpgradeLocalizationSeeder
    {
        private const string SO_BASE_PATH = "Assets/ScriptableObject/Upgrades";

        private struct NameDesc
        {
            public string Name;
            public string Desc;
            public NameDesc(string name, string desc) { Name = name; Desc = desc; }
        }

        private static readonly Dictionary<string, NameDesc> Map = new()
        {
            // Stat
            { "AttackUpgradeSO", new NameDesc("공격력 증가", "공격력이 증가합니다.") },
            { "HealthUpgradeSO", new NameDesc("체력 증가", "최대 체력이 증가합니다.") },
            { "DefenseUpgradeSO", new NameDesc("방어력 증가", "방어력이 증가합니다.") },
            { "ShieldUpgradeSO", new NameDesc("보호막 획득", "공격을 막아줄 보호막을 얻습니다.") },
            { "RegenUpgradeSO", new NameDesc("체력 회복량 증가", "턴마다 회복되는 체력이 증가합니다.") },

            // Pawn movement
            { "PawnSidewayUpgradeSO", new NameDesc("사이드웨이", "옆 칸 이동+공격") },
            { "PawnFrontAttackUpgradeSO", new NameDesc("프론트 어택", "앞 한칸 공격") },
            { "PawnLargerAttackSpaceUpgradeSO", new NameDesc("라저 어택 스페이스", "앞 1칸 옆 2칸 공격") },
            { "PawnTwoStepFrontUpgradeSO", new NameDesc("투스탭 프론트", "앞 2칸 이동") },
            { "PawnDiagonalMoveUpgradeSO", new NameDesc("다이에고널 무브", "앞 대각선 이동") },
            { "PawnBackstepUpgradeSO", new NameDesc("백스탭", "뒤 이동") },

            // Knight movement
            { "KnightCrossUpgradeSO", new NameDesc("크로스 십자", "십자 이동") },
            { "KnightZebraUpgradeSO", new NameDesc("제브라", "제브라 패턴 이동") },
            { "KnightDashUpgradeSO", new NameDesc("대시 앞두칸", "앞으로 두 칸 돌진") },
            { "KnightCamelUpgradeSO", new NameDesc("스퀘어 무브 캐멀", "캐멀 형태 이동") },

            // Rook movement
            { "RookBishopAttackUpgradeSO", new NameDesc("비숍 어택", "대각선 공격") },
            { "RookKnightAttackUpgradeSO", new NameDesc("나이트 어택", "나이트 공격") },
            { "RookBishopMoveUpgradeSO", new NameDesc("비숍 무브", "대각선 이동") },
            { "RookKnightMoveUpgradeSO", new NameDesc("나이트 무브", "나이트 이동") },

            // Bishop movement
            { "BishopRookMoveUpgradeSO", new NameDesc("룩 무브", "직선 이동") },
            { "BishopRookAttackUpgradeSO", new NameDesc("룩 어택", "직선 공격") },
            { "BishopKnightMoveUpgradeSO", new NameDesc("나이트 무브", "나이트 이동") },
            { "BishopKnightAttackUpgradeSO", new NameDesc("나이트 어택", "나이트 공격") },
            { "BishopReflectAttackUpgradeSO", new NameDesc("리플렉트 어택", "공격 반사 어택") },

            // Queen movement
            { "QueenKnightMoveUpgradeSO", new NameDesc("나이트 무브", "나이트 이동") },
            { "QueenKnightAttackUpgradeSO", new NameDesc("나이트 어택", "나이트 공격") },
            { "QueenReflectAttackUpgradeSO", new NameDesc("리플렉트 어택", "공격 반사 어택") },

            // King movement
            { "KingKnightMoveUpgradeSO", new NameDesc("나이트", "나이트 이동/공격") },
            { "KingBishop3UpgradeSO", new NameDesc("비숍 최대 3칸", "비숍처럼 최대 3칸 이동/공격") },
            { "KingRook3UpgradeSO", new NameDesc("룩 최대 3칸", "룩처럼 최대 3칸 이동/공격") },

            // Common abilities
            { "HitAndRunAbilitySO", new NameDesc("히트앤런", "상대방을 공격하고 상대방이 사망하더라도 이동하지 않습니다") },
            { "AccelerationAbilitySO", new NameDesc("가속도", "이동할때마다 공격력이 2 증가합니다. 공격시 초기화됩니다.") },
            { "PoisonAbilitySO", new NameDesc("독", "공격시 적에게 독 상태이상을 부여합니다.") },
            { "VampirismAbilitySO", new NameDesc("흡혈", "공격시 공격력/4를 회복합니다.") },
            { "ForceMassAccelAbilitySO", new NameDesc("F=ma", "공격시 거리만큼 데미지가 증가함.") },
            { "KamikazeAbilitySO", new NameDesc("카미카제", "사망시 현재 공격력만큼의 데미지를 주변 8칸에 줌.") },

            // Pawn abilities
            { "PawnAutoMoveAbilitySO", new NameDesc("자동이동", "자기 턴 행동이후 자동 한칸 전진, 전진시 앞에 적 있을 경우 공격") },
            { "FriendsShieldAbilitySO", new NameDesc("프렌즈 쉴드", "피해를 받을때 주변 8칸에 다른 폰이 있다면 해당 폰에게 1/4 데미지를 주고 본인은 1/2 데미지만 받음.") },
            { "StructuralViolenceAbilitySO", new NameDesc("폰 구조적 폭력", "연결된 폰 갯수만큼 가하는 데미지 배로 증가.") },
            { "LoneWolfAbilitySO", new NameDesc("나 혼자 산다", "즉시 내 팀의 모든 다른 폰들이 처치됨. 경험치 5배 흡수, 체력/공격력 2배.") },
            { "PawnGraceAbilitySO", new NameDesc("폰은정", "1회 공격을 받아도 무효화. 이 피스 2회 행동시 효과 충전.") },
            { "DragonFromStreamAbilitySO", new NameDesc("개천에서 용난다", "퀸으로 승급") },
            { "PawnSellAbilitySO", new NameDesc("폰 팔이", "이 피스를 제거합니다. 단체 강화 1개를 얻습니다.") },

            // Rook abilities
            { "MovingManAbilitySO", new NameDesc("무빙맨", "이동한 칸수만큼 경험치가 증가합니다.") },
            { "DeepThrustAbilitySO", new NameDesc("깊은 찌르기", "공격한 방향으로 뒤에 있는 대상까지 피해를 입음.") },
            { "LookAtMeAbilitySO", new NameDesc("룩엣미", "이 피스를 공격한 피스를 이 피스가 죽기 전까지 다른 대상을 공격할 수 없습니다.") },
            { "DisableCrossAbilitySO", new NameDesc("떼껄룩", "십자 방향중 막히지 않은 위치에 있는 적 피스는 행동할 수 없습니다.") },
            { "GreatWallAbilitySO", new NameDesc("만리장성", "이 룩의 방어력 1 증가, 같은 줄에 있는 아군 방어력 상승.") },
            { "ShoulderBashRookAbilitySO", new NameDesc("어깨빵", "십자 이동시 지나간 칸들의 옆칸의 적을 공격력/5만큼 밀침.") },

            // Bishop abilities
            { "GroundEffectAbilitySO", new NameDesc("장판", "공격한 위치에 장판을 깝니다. 4턴 지속.") },
            { "BombDropAbilitySO", new NameDesc("폭탄 투하", "공격한 범위 기준 십자로 스플뎀") },
            { "StealthAbilitySO", new NameDesc("은신", "이동한 후 1턴간 무적 상태가 됩니다.") },
            { "PatientHunterAbilitySO", new NameDesc("은밀하게 위대하게", "움직이지 않은 턴만큼 데미지가 증가합니다.") },
            { "LongRangeSniperAbilitySO", new NameDesc("초장거리 저격", "5칸 이상 거리에 있는 적을 공격할 경우 즉사합니다") },
            { "PawnJumpAbilitySO", new NameDesc("이폰은 이제 제껍니다", "이동칸 계산시 폰을 뛰어넘어 공격") },
            { "PoisonShotAbilitySO", new NameDesc("독 쏘는 맛", "공격 받은 적은 매턴 체력이 최대 체력의 10% 감소, 5턴") },
            { "ChainAttackAbilitySO", new NameDesc("연계 공격", "아군을 공격할 수 있고 아군 범위에 적이 있으면 비숍 데미지를 적에게 전달") },

            // Knight abilities
            { "ForkAttackAbilitySO", new NameDesc("포크킄", "포크에 성공했을때 둘다 때립니다") },
            { "KnightOfKnightsAbilitySO", new NameDesc("나이트 오브 나이츠", "다른 나이트 제거, 체력/공격력+4 방어력+2 재생+1") },
            { "KnightInGaleAbilitySO", new NameDesc("나이트 인 게일", "나이트가 아군을 공격할 경우 공격력만큼 체력을 회복합니다.") },
            { "KnightTimeAbilitySO", new NameDesc("나이트 타임", "현재 턴수를 3으로 나눈 값이 2일 경우 공격을 받아도 죽지 않습니다.") },
            { "GoodNightAbilitySO", new NameDesc("굿 나잇", "이 나이트에게 공격받은 아군은 공격력이 절반으로 감소, 5턴") },
            { "ChivalryAbilitySO", new NameDesc("기사도 정신", "방어력 +1, 이동 가능 위치의 아군이 공격받으면 위치 교대 후 대신 피해.") },
            { "CounterAttackAbilitySO", new NameDesc("반격", "공격을 받은 경우 대상에게 공격합니다.") },
            { "DoubleStrikeAbilitySO", new NameDesc("이중 타격", "한 번의 행동으로 두 번 공격합니다.") },
            { "PersistentAttackAbilitySO", new NameDesc("끈질긴 공격", "공격 후 추가 타격을 시도합니다.") },
            { "StompAbilitySO", new NameDesc("스톰프", "인접 적을 짓밟아 피해를 줍니다.") },

            // Queen abilities
            { "SpawnPawnAbilitySO", new NameDesc("도티낳음", "이동시 이동 전 자리에 폰 생성") },
            { "OverloadedControlTowerAbilitySO", new NameDesc("오버로디드 컨트롤 타워", "적이 사거리 내 있고 아군이 공격으로 죽으면 자동 공격") },
            { "MothersGreatnessAbilitySO", new NameDesc("어머니의 위대함", "0이 되는 피해를 3회까지 막아냅니다.") },
            { "GivingTreeAbilitySO", new NameDesc("아낌없이 주는 나무", "회복력 +2, 매턴 체력 낮은 아군 회복, 자신의 체력 소모") },
            { "RoyalGuardAbilitySO", new NameDesc("왕가의 호위", "퀸을 보호하기 위해 근접 적을 견제합니다.") },
            { "QueensMajestyAbilitySO", new NameDesc("퀸의 위엄", "퀸의 존재만으로도 적을 위압합니다.") },
            { "PowerTransferAbilitySO", new NameDesc("힘의 전달", "자신의 힘을 다른 아군에게 전달합니다.") },

            // Global upgrades
            { "GambitUpgradeSO", new NameDesc("갬빗", "모든 폰 50% 확률 사망, 생존 폰 경험치 10, 레벨 2배") },
            { "MachoismUpgradeSO", new NameDesc("마쵸이즘", "뒤로 이동 불가, 옆 이동은 공격 시만, 피해 2배") },
            { "CannibalismUpgradeSO", new NameDesc("동족 포식", "아군 공격시 즉사, 경험치 3배") },
            { "RainbowReflectUpgradeSO", new NameDesc("무지개반사", "받은 데미지 절반 반사") },
            { "FocusBandUpgradeSO", new NameDesc("기합의 띠", "체력이 0이 되는 공격을 한 번 무마하고 체력 1로") },
            { "PushbackUpgradeSO", new NameDesc("밀어", "공격시 공격력/5만큼 밀칩니다.") },
            { "GlobalAttackUpgradeSO", new NameDesc("때린곳 더 때리기", "공격시 공격력 1.2배 상승") },
            { "NecromancerUpgradeSO", new NameDesc("네크로멘서", "킹 체력 1, 폰이 죽지 않습니다.") },
            { "SolidarityUpgradeSO", new NameDesc("안아프게 맞는법", "우리팀 모든 피스 방어력 3 증가") },
            { "KingsPrestigeUpgradeSO", new NameDesc("왕의 위세", "왕의 존재감이 팀 전체를 강화") },
            { "GlobalHealthUpgradeSO", new NameDesc("체력 단체 강화", "아군 전체 체력 증가") },
            { "GlobalDefenseUpgradeSO", new NameDesc("방어 단체 강화", "아군 전체 방어력 증가") },
            { "ExpBonusUpgradeSO", new NameDesc("경험치 보너스", "획득 경험치 증가") },
            { "StartBonusUpgradeSO", new NameDesc("시작 보너스", "전투 시작 시 추가 보너스") },
            { "ResurrectionUpgradeSO", new NameDesc("부활", "전투에서 한번 부활") },
            { "TeamSynergyUpgradeSO", new NameDesc("시너지", "팀 조합에 따라 추가 효과") },
            { "BasicGlobalUpgradeSO", new NameDesc("기본 글로벌 강화", "아군 전체에 적용되는 기본 강화") },
        };

        [MenuItem("Tools/LevelUpChess/Fill Upgrade Names & Descriptions")]
        public static void FillNames()
        {
            string[] guids = AssetDatabase.FindAssets("t:UpgradeBaseSO", new[] { SO_BASE_PATH });
            int updated = 0;

            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var up = AssetDatabase.LoadAssetAtPath<UpgradeBaseSO>(path);
                if (up == null) continue;
                if (!Map.TryGetValue(up.GetType().Name, out var nd)) continue;

                var so = new SerializedObject(up);
                bool changed = false;

                var nameProp = so.FindProperty("upgradeName");
                if (nameProp != null && nameProp.stringValue != nd.Name)
                {
                    nameProp.stringValue = nd.Name;
                    changed = true;
                }

                var descProp = so.FindProperty("description");
                if (descProp != null && descProp.stringValue != nd.Desc)
                {
                    descProp.stringValue = nd.Desc;
                    changed = true;
                }

                if (!changed) continue;

                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(up);
                updated++;
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[UpgradeLocalizationSeeder] 이름/설명 {updated}개를 채웠습니다.");
        }
    }
}
