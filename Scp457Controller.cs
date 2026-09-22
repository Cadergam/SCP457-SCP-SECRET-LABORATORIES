using System.Collections.Generic;
using LabApi.Features.Wrappers;
using UnityEngine;

namespace Scp457
{
    public sealed class Scp457Controller : MonoBehaviour
    {
        private sealed class BurnState
        {
            public float EndAt;
            public float NextTickAt;
        }

        private sealed class RoleSetupState
        {
            public string UserId;
            public float ApplyAt;
        }

        private readonly Dictionary<string, BurnState> _burning =
            new Dictionary<string, BurnState>();

        private readonly List<RoleSetupState> _pendingRoleSetups =
            new List<RoleSetupState>();

        private readonly List<string> _removeBuffer =
            new List<string>();

        private Scp457Plugin _plugin;

        public void Initialize(Scp457Plugin plugin)
        {
            _plugin = plugin;
        }

        public void StartBurn(string userId)
        {
            if (_plugin == null || string.IsNullOrEmpty(userId))
                return;

            float now = Time.unscaledTime;

            _burning[userId] = new BurnState
            {
                EndAt = now + _plugin.Config.BurnDuration,
                NextTickAt = now + _plugin.Config.BurnTickInterval
            };
        }

        public void StopBurn(string userId)
        {
            if (!string.IsNullOrEmpty(userId))
                _burning.Remove(userId);
        }

        public void ScheduleRoleSetup(string userId, float delay)
        {
            if (string.IsNullOrEmpty(userId))
                return;

            _pendingRoleSetups.Add(new RoleSetupState
            {
                UserId = userId,
                ApplyAt = Time.unscaledTime + delay
            });
        }

        private void Update()
        {
            if (_plugin == null)
                return;

            ProcessRoleSetups();
            ProcessBurns();
        }

        private void ProcessRoleSetups()
        {
            float now = Time.unscaledTime;

            for (int i = _pendingRoleSetups.Count - 1; i >= 0; i--)
            {
                RoleSetupState state = _pendingRoleSetups[i];

                if (now < state.ApplyAt)
                    continue;

                _plugin.ApplyScp457Stats(state.UserId);
                _pendingRoleSetups.RemoveAt(i);
            }
        }

        private void ProcessBurns()
        {
            if (_burning.Count == 0)
                return;

            float now = Time.unscaledTime;
            _removeBuffer.Clear();

            foreach (KeyValuePair<string, BurnState> pair in _burning)
            {
                string userId = pair.Key;
                BurnState state = pair.Value;

                Player target = Player.Get(userId);

                if (target == null || !target.IsHuman || now >= state.EndAt)
                {
                    _removeBuffer.Add(userId);
                    continue;
                }

                if (now < state.NextTickAt)
                    continue;

                target.Damage(
                    _plugin.Config.BurnDamagePerTick,
                    _plugin.Config.BurnDeathReason);

                state.NextTickAt = now + _plugin.Config.BurnTickInterval;
            }

            foreach (string userId in _removeBuffer)
                _burning.Remove(userId);
        }

        private void OnDestroy()
        {
            _burning.Clear();
            _pendingRoleSetups.Clear();
            _removeBuffer.Clear();
            _plugin = null;
        }
    }
}
