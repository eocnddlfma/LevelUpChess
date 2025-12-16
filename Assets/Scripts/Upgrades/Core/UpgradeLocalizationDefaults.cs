using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace LevelUpChess.Upgrades
{
#if UNITY_EDITOR
    /// <summary>
    /// 에디터에서 이름/설명이 비어 있을 때 기본 텍스트를 채워준다.
    /// 런타임 빌드에는 포함되지 않는다.
    /// </summary>
    public static class UpgradeLocalizationDefaults
    {
        private static readonly Dictionary<string, (string name, string desc)> DefaultText = new()
        {
            // Stat
            { "AttackUpgradeSO", ("공격력 증가", "공격력이 증가합니다.") },
            { "HealthUpgradeSO", ("체력 증가", "최대 체력이 증가합니다.") },
            { "DefenseUpgradeSO", ("방어력 증가", "방어력이 증가합니다.") },
            { "ShieldUpgradeSO", ("보호막 획득", "공격을 막아줄 보호막을 얻습니다.") },
            { "RegenUpgradeSO", ("체력 회복량 증가", "턴마다 회복되는 체력이 증가합니다.") },

            // Movement - Pawn
            { "PawnSidewayUpgradeSO", ("사이드웨이", "옆 칸으로 이동하며 공격합니다.") },
            { "PawnFrontAttackUpgradeSO", ("프론트 어택", "앞 한 칸을 공격합니다.") },
            { "PawnLargerAttackSpaceUpgradeSO", ("라저 어택 스페이스", "앞 1칸, 양옆 2칸 범위를 공격합니다.") },
            { "PawnTwoStepFrontUpgradeSO", ("투스탭 프론트", "앞으로 두 칸 이동합니다.") },
            { "PawnDiagonalMoveUpgradeSO", ("다이에고널 무브", "앞 대각선으로 이동합니다.") },
            { "PawnBackstepUpgradeSO", ("백스탭", "뒤로 이동합니다.") },

            // Movement - Knight
            { "KnightCrossUpgradeSO", ("크로스 십자", "십자 형태로 이동합니다.") },
            { "KnightZebraUpgradeSO", ("제브라", "제브라 패턴으로 이동합니다.") },
            { "KnightDashUpgradeSO", ("대시 앞두칸", "앞으로 두 칸 돌진합니다.") },
            { "KnightCamelUpgradeSO", ("스퀘어 무브 캐멀", "캐멀(스퀘어 무브) 형태로 이동합니다.") },

            // Movement - Rook
            { "RookBishopAttackUpgradeSO", ("비숍 어택", "대각선으로 공격합니다.") },
            { "RookKnightAttackUpgradeSO", ("나이트 어택", "나이트처럼 공격합니다.") },
            { "RookBishopMoveUpgradeSO", ("비숍 무브", "대각선으로 이동합니다.") },
            { "RookKnightMoveUpgradeSO", ("나이트 무브", "나이트처럼 이동합니다.") },

            // Movement - Bishop
            { "BishopRookMoveUpgradeSO", ("룩 무브", "직선으로 이동합니다.") },
            { "BishopRookAttackUpgradeSO", ("룩 어택", "직선으로 공격합니다.") },
            { "BishopKnightMoveUpgradeSO", ("나이트 무브", "나이트처럼 이동합니다.") },
            { "BishopKnightAttackUpgradeSO", ("나이트 어택", "나이트처럼 공격합니다.") },
            { "BishopReflectAttackUpgradeSO", ("리플렉트 어택", "공격을 반사하는 어택을 사용합니다.") },

            // Movement - Queen
            { "QueenKnightMoveUpgradeSO", ("나이트 무브", "나이트처럼 이동합니다.") },
            { "QueenKnightAttackUpgradeSO", ("나이트 어택", "나이트처럼 공격합니다.") },
            { "QueenReflectAttackUpgradeSO", ("리플렉트 어택", "공격을 반사하는 어택을 사용합니다.") },

            // Movement - King
            { "KingKnightMoveUpgradeSO", ("나이트", "나이트처럼 이동/공격합니다.") },
            { "KingBishop3UpgradeSO", ("비숍 최대 3칸", "비숍처럼 최대 3칸 이동/공격합니다.") },
            { "KingRook3UpgradeSO", ("룩 최대 3칸", "룩처럼 최대 3칸 이동/공격합니다.") },

            // Common abilities
            { "HitAndRunAbilitySO", ("히트앤런", "공격 후에도 이동하지 않습니다.") },
            { "AccelerationAbilitySO", ("가속도", "이동마다 공격력이 2 증가하고 공격 시 초기화됩니다.") },
            { "PoisonAbilitySO", ("독", "공격 시 적에게 독 상태이상을 부여합니다.") },
            { "VampirismAbilitySO", ("흡혈", "공격 시 공격력의 1/4만큼 체력을 회복합니다.") },
            { "ForceMassAccelAbilitySO", ("F=ma", "공격 거리에 비례해 피해가 증가합니다.") },
            { "KamikazeAbilitySO", ("카미카제", "사망 시 주변 8칸에 현재 공격력만큼 피해를 줍니다.") },

            // Pawn abilities
            { "PawnAutoMoveAbilitySO", ("자동이동", "행동 후 자동으로 한 칸 전진하며 앞에 적이 있으면 공격합니다.") },
            { "FriendsShieldAbilitySO", ("프렌즈 쉴드", "피격 시 주변 폰에게 피해를 분산하고 자신은 절반만 받습니다.") },
            { "StructuralViolenceAbilitySO", ("폰 구조적 폭력", "연결된 폰 수만큼 공격 피해가 배로 증가합니다.") },
            { "LoneWolfAbilitySO", ("나 혼자 산다", "아군 폰을 모두 처치하고 경험치 5배를 흡수, 체력/공격력이 2배가 됩니다.") },
            { "PawnGraceAbilitySO", ("폰은정", "한 번 공격을 무효화하며 두 번 행동하면 효과가 충전됩니다.") },
            { "DragonFromStreamAbilitySO", ("개천에서 용난다", "즉시 퀸으로 승급합니다.") },
            { "PawnSellAbilitySO", ("폰 팔이", "해당 폰을 제거하고 단체 강화 1개를 획득합니다.") },

            // Rook abilities
            { "MovingManAbilitySO", ("무빙맨", "이동한 칸 수만큼 경험치가 증가합니다.") },
            { "DeepThrustAbilitySO", ("깊은 찌르기", "공격 방향 뒤에 있는 대상까지 피해를 입힙니다.") },
            { "LookAtMeAbilitySO", ("룩엣미", "이 룩을 공격한 적은 이 룩이 죽기 전까지 다른 대상을 공격할 수 없습니다.") },
            { "DisableCrossAbilitySO", ("떼껄룩", "십자 방향 막히지 않은 위치의 적은 행동할 수 없습니다.") },
            { "GreatWallAbilitySO", ("만리장성", "방어력 1 증가, 같은 줄 아군 방어력이 추가로 상승합니다.") },
            { "ShoulderBashRookAbilitySO", ("어깨빵", "십자 이동 시 지나간 칸 옆 적을 밀칩니다.") },

            // Bishop abilities
            { "GroundEffectAbilitySO", ("장판", "공격 위치에 4턴 지속 장판을 깔아 피해를 줍니다.") },
            { "BombDropAbilitySO", ("폭탄 투하", "공격 범위를 기준으로 십자 스플래시 피해를 줍니다.") },
            { "StealthAbilitySO", ("은신", "이동 후 1턴간 무적 상태가 됩니다.") },
            { "PatientHunterAbilitySO", ("은밀하게 위대하게", "움직이지 않은 턴만큼 피해가 증가합니다.") },
            { "LongRangeSniperAbilitySO", ("초장거리 저격", "5칸 이상 거리에서 공격하면 즉사시킵니다.") },
            { "PawnJumpAbilitySO", ("이폰은 이제 제껍니다", "폰을 뛰어넘어 공격할 수 있습니다.") },
            { "PoisonShotAbilitySO", ("독 쏘는 맛", "공격 받은 적에게 매턴 최대 체력 10% 독을 5턴 부여합니다.") },
            { "ChainAttackAbilitySO", ("연계 공격", "아군을 공격해 적에게 피해를 전달합니다.") },

            // Knight abilities
            { "ForkAttackAbilitySO", ("포크킄", "포크에 성공하면 두 대상 모두 공격합니다.") },
            { "KnightOfKnightsAbilitySO", ("나이트 오브 나이츠", "다른 나이트를 제거하고 체력/공격력/방어/재생이 상승합니다.") },
            { "KnightInGaleAbilitySO", ("나이트 인 게일", "아군을 공격하면 공격력만큼 체력을 회복합니다.") },
            { "KnightTimeAbilitySO", ("나이트 타임", "현재 턴 수를 3으로 나눈 값이 2일 때는 공격을 받아도 죽지 않습니다.") },
            { "GoodNightAbilitySO", ("굿 나잇", "이 나이트에게 공격받은 아군은 공격력이 절반이 되는 약화를 5턴 받습니다.") },
            { "ChivalryAbilitySO", ("기사도 정신", "방어력+1, 이동 가능한 아군이 공격받으면 위치를 바꾸고 피해를 대신 받습니다.") },
            { "CounterAttackAbilitySO", ("반격", "공격을 받으면 대상에게 반격합니다.") },
            { "DoubleStrikeAbilitySO", ("이중 타격", "한 번의 행동으로 두 번 공격합니다.") },
            { "PersistentAttackAbilitySO", ("끈질긴 공격", "공격 후 추가 타격을 시도합니다.") },
            { "StompAbilitySO", ("스톰프", "인접 적을 짓밟아 피해를 줍니다.") },

            // Queen abilities
            { "SpawnPawnAbilitySO", ("도티낳음", "이동 시 이동 전 자리에 폰을 생성합니다.") },
            { "OverloadedControlTowerAbilitySO", ("오버로디드 컨트롤 타워", "사거리 내 적이 있을 때 아군이 공격으로 죽으면 즉시 자동 공격합니다.") },
            { "MothersGreatnessAbilitySO", ("어머니의 위대함", "0이 되는 피해를 3회까지 막아냅니다.") },
            { "GivingTreeAbilitySO", ("아낌없이 주는 나무", "회복력 +2, 매턴 체력이 가장 낮은 아군을 회복하고 자신의 체력을 소모합니다.") },
            { "RoyalGuardAbilitySO", ("왕가의 호위", "퀸을 보호하기 위해 근접 적을 견제합니다.") },
            { "QueensMajestyAbilitySO", ("퀸의 위엄", "퀸의 존재만으로도 적을 위압합니다.") },
            { "PowerTransferAbilitySO", ("힘의 전달", "자신의 힘을 다른 아군에게 전달합니다.") },

            // Global
            { "GambitUpgradeSO", ("갬빗", "모든 아군 공격력 +1") },
            { "MachoismUpgradeSO", ("마쵸이즘", "모든 아군 이동력 +1") },
            { "CannibalismUpgradeSO", ("동족 포식", "아군을 죽이면 경험치 3배") },
            { "RainbowReflectUpgradeSO", ("레인보우 리플렉트", "받은 피해의 절반을 반사") },
            { "FocusBandUpgradeSO", ("집중 밴드", "모든 아군 회피율 +1") },
            { "PushbackUpgradeSO", ("푸시백", "모든 아군이 공격받을 때 공격자를 밀어내기") },
            { "GlobalAttackUpgradeSO", ("글로벌 어택 업그레이드", "모든 아군 공격력 +1") },
            { "NecromancerUpgradeSO", ("네크로맨서", "죽은 아군을 좀비로 부활시키기") },
            { "SolidarityUpgradeSO", ("솔리다리티", "모든 아군 체력 +1") },
            { "KingsPrestigeUpgradeSO", ("킹즈 프레스티지", "킹이 죽으면 게임 종료") },
            { "GlobalHealthUpgradeSO", ("글로벌 헬스 업그레이드", "모든 아군 최대 체력 +1") },
            { "GlobalDefenseUpgradeSO", ("글로벌 디펜스 업그레이드", "모든 아군 방어력 +1") },
            { "ExpBonusUpgradeSO", ("경험치 보너스 업그레이드", "모든 아군 경험치 획득량 +10%") },
            { "StartBonusUpgradeSO", ("시작 보너스 업그레이드", "게임 시작 시 모든 아군 레벨 +1") },
            { "ResurrectionUpgradeSO", ("리저렉션", "죽은 아군을 부활시키기") },
            { "TeamSynergyUpgradeSO", ("팀 시너지", "같은 팀 기물들이 서로 도와주기") },
            { "BasicGlobalUpgradeSO", ("베이직 글로벌 업그레이드", "기본 글로벌 업그레이드") },
        };

        public static void ApplyIfEmpty(UpgradeBaseSO target)
        {
            if (target == null) return;
            if (!DefaultText.TryGetValue(target.GetType().Name, out var nd)) return;

            var so = new SerializedObject(target);
            bool changed = false;

            var nameProp = so.FindProperty("upgradeName");
            if (nameProp != null &&
                (string.IsNullOrEmpty(nameProp.stringValue) || nameProp.stringValue == target.GetType().Name))
            {
                nameProp.stringValue = nd.name;
                changed = true;
            }

            var descProp = so.FindProperty("description");
            if (descProp != null && string.IsNullOrEmpty(descProp.stringValue))
            {
                descProp.stringValue = nd.desc;
                changed = true;
            }

            if (changed)
            {
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(target);
            }
        }
    }
#endif
}
