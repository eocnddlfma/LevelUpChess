using UnityEngine;
using System.Collections.Generic;
using LevelUpChess.Pieces;
using LevelUpChess.Board;
using LevelUpChess.Core;

namespace LevelUpChess.Upgrades.Abilities
{
    /// <summary>
    /// 프렌즈 쉴드: 피해를 받을때 주변 8칸에 다른 폰이 있다면 해당 폰에게 1/4 데미지를 주고 본인은 1/2 데미지만 받음.
    /// </summary>
    [CreateAssetMenu(fileName = "FriendsShieldAbility", menuName = "LevelUpChess/Upgrades/Abilities/Pawn/FriendsShield")]
    public class FriendsShieldAbilitySO : AbilityBaseSO
    {
        private const string DEFAULT_NAME = "프렌즈 쉴드";
        private const string DEFAULT_DESC = "피해를 받을때 주변 8칸에 다른 폰이 있다면 해당 폰에게 1/4 데미지를 주고 본인은 1/2 데미지만 받음.";

        public override void OnApply(ChessPiece piece)
        {
            Debug.Log($"[FriendsShield] {piece.name} gained Friends Shield");
        }

        public override void OnRemove(ChessPiece piece)
        {
            Debug.Log($"[FriendsShield] {piece.name} lost Friends Shield");
        }

        public override void Execute(AbilityContext context)
        {
            if (context.Owner == null) return;
            if (context.Trigger != AbilityTrigger.OnHit) return;

            var currentTile = context.Owner.CurrentTile;
            if (currentTile == null) return;

            var boardManager = ServiceLocator.Get<BoardManager>();
            if (boardManager == null) return;

            // Check adjacent tiles (8 directions)
            Vector2Int[] offsets =
            {
                new Vector2Int(1, 0), new Vector2Int(-1, 0),
                new Vector2Int(0, 1), new Vector2Int(0, -1),
                new Vector2Int(1, 1), new Vector2Int(1, -1),
                new Vector2Int(-1, 1), new Vector2Int(-1, -1)
            };

            ChessPiece redirectPawn = null;
            foreach (var offset in offsets)
            {
                var tile = boardManager.GetTileAt(currentTile.coordinate + offset);
                if (tile != null && tile.OccupyingPiece != null && tile.OccupyingPiece.Team == context.Owner.Team)
                {
                    redirectPawn = tile.OccupyingPiece;
                    break;
                }
            }

            if (redirectPawn != null)
            {
                int incoming = Mathf.Max(0, context.Damage + context.BonusDamage);
                int allyDamage = Mathf.Max(1, incoming / 4);
                int ownerReduction = Mathf.Max(1, incoming / 2);
                context.BonusDamage -= ownerReduction;
                redirectPawn.TakeDamage(allyDamage, context.Attacker);
                Debug.Log($"[FriendsShield] {context.Owner.name} splits damage. {redirectPawn.name} takes {allyDamage}, owner damage reduced by {ownerReduction}");
            }
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            if (string.IsNullOrEmpty(upgradeName)) upgradeName = DEFAULT_NAME;
            if (string.IsNullOrEmpty(description)) description = DEFAULT_DESC;
            trigger = AbilityTrigger.OnHit;
            pieceFilter = PieceTypeFilter.Pawn;
        }
#endif
    }
}
