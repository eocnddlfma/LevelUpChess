using System.Collections.Generic;
using UnityEngine;
using LevelUpChess.Core;
using LevelUpChess.Pieces;

namespace LevelUpChess.Upgrades.Abilities
{
    /// <summary>
    /// Minimal resource manager that tracks team gold for upgrades relying on economy.
    /// </summary>
    public class ResourceManager : MonoBehaviour
    {
        private readonly Dictionary<Team, int> _teamGold = new Dictionary<Team, int>();

        private void Awake()
        {
            if (ServiceLocator.Has<ResourceManager>())
            {
                Destroy(gameObject);
                return;
            }

            ServiceLocator.Register(this);
        }

        private void OnDestroy()
        {
            if (ServiceLocator.Has<ResourceManager>() && ServiceLocator.Get<ResourceManager>() == this)
            {
                ServiceLocator.Unregister<ResourceManager>();
            }
        }

        public void AddGold(Team team, int amount)
        {
            if (amount == 0) return;

            if (!_teamGold.ContainsKey(team))
            {
                _teamGold[team] = 0;
            }

            _teamGold[team] += amount;
            Debug.Log($"[ResourceManager] Team {team} gold changed by {amount}, now {_teamGold[team]}");
        }

        public int GetGold(Team team)
        {
            return _teamGold.TryGetValue(team, out var value) ? value : 0;
        }
    }
}
