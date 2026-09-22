using System;
using System.Collections.Generic;
using CustomPlayerEffects;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using LabApi.Features;
using LabApi.Features.Console;
using LabApi.Features.Wrappers;
using LabApi.Loader.Features.Plugins;
using PlayerRoles;
using UnityEngine;

namespace Scp457
{
    public sealed class Scp457Plugin : Plugin<Scp457Config>
    {
        public static Scp457Plugin Instance { get; private set; }

        public override string Name => "SCP-457";
        public override string Description => "Ajoute SCP-457, ses capacites et des extincteurs utilisables sans mod client.";
        public override string Author => "Cadergam";
        public override Version Version => new Version(1, 6, 0);
        public override Version RequiredApiVersion => new Version(LabApiProperties.CompiledVersion);

        private readonly HashSet<string> _scp457Players = new HashSet<string>();
        private readonly Dictionary<string, string> _originalDisplayNames =
            new Dictionary<string, string>();
        private readonly Dictionary<string, string> _originalCustomInfos =
            new Dictionary<string, string>();
        private readonly Dictionary<string, PlayerInfoArea> _originalInfoAreas =
            new Dictionary<string, PlayerInfoArea>();
        private readonly Dictionary<string, float> _fireBurstReadyAt =
            new Dictionary<string, float>();
        private readonly Dictionary<string, int> _extinguisherCharges =
            new Dictionary<string, int>();
        private readonly Dictionary<string, float> _extinguisherReadyAt =
            new Dictionary<string, float>();
        private readonly Dictionary<string, float> _fireIntensity =
            new Dictionary<string, float>();
        private readonly Dictionary<string, Scp457ExtinguisherModel> _extinguisherModels =
            new Dictionary<string, Scp457ExtinguisherModel>();

        private GameObject _controllerObject;
        private Scp457Controller _controller;
        private Scp457ServerSpecificSettings _serverSpecificSettings;

        public override void Enable()
        {
            Instance = this;

            _controllerObject = new GameObject("SCP457_Controller");
            UnityEngine.Object.DontDestroyOnLoad(_controllerObject);

            _controller = _controllerObject.AddComponent<Scp457Controller>();
            _controller.Initialize(this);

            _serverSpecificSettings = new Scp457ServerSpecificSettings(this);
            _serverSpecificSettings.Enable();

            PlayerEvents.Hurting += OnPlayerHurting;
            PlayerEvents.Spawned += OnPlayerSpawned;
            PlayerEvents.Dying += OnPlayerDying;
            PlayerEvents.Left += OnPlayerLeft;

            LabApi.Features.Console.Logger.Info("SCP-457 v1.6.0 active - auteur : Cadergam - extincteurs virtuels actifs.");
        }

        public override void Disable()
        {
            PlayerEvents.Hurting -= OnPlayerHurting;
            PlayerEvents.Spawned -= OnPlayerSpawned;
            PlayerEvents.Dying -= OnPlayerDying;
            PlayerEvents.Left -= OnPlayerLeft;

            _serverSpecificSettings?.Disable();
            _serverSpecificSettings = null;

            RestoreAllPlayerPresentation();
            _scp457Players.Clear();
            _originalDisplayNames.Clear();
            _originalCustomInfos.Clear();
            _originalInfoAreas.Clear();
            _fireBurstReadyAt.Clear();
            _extinguisherCharges.Clear();
            _extinguisherReadyAt.Clear();
            _fireIntensity.Clear();
            DestroyAllExtinguisherModels();

            if (_controllerObject != null)
                UnityEngine.Object.Destroy(_controllerObject);

            _controller = null;
            _controllerObject = null;
            Instance = null;
        }

        public bool IsScp457(Player player)
        {
            return player != null
                && player.Role == RoleTypeId.Scp0492
                && !string.IsNullOrEmpty(player.UserId)
                && _scp457Players.Contains(player.UserId);
        }

        public void SpawnScp457(Player player)
        {
            if (player == null || string.IsNullOrEmpty(player.UserId))
                return;

            if (!_originalDisplayNames.ContainsKey(player.UserId))
                _originalDisplayNames[player.UserId] = player.DisplayName;

            if (!_originalCustomInfos.ContainsKey(player.UserId))
                _originalCustomInfos[player.UserId] = player.CustomInfo;

            if (!_originalInfoAreas.ContainsKey(player.UserId))
                _originalInfoAreas[player.UserId] = player.InfoArea;

            _scp457Players.Add(player.UserId);

            // SCP-049-2 sert de corps technique car il possède déjà
            // une attaque de mêlée utilisable par le plugin.
            player.SetRole(RoleTypeId.Scp0492);

            // On applique les statistiques après le changement de rôle.
            _controller?.ScheduleRoleSetup(player.UserId, 0.75f);
        }

        internal void ApplyScp457Stats(string userId)
        {
            Player player = Player.Get(userId);

            if (player == null || !IsScp457(player))
                return;

            player.MaxHealth = Config.Health;
            player.Health = Config.Health;
            player.CustomInfo = Config.CustomInfo;

            // Retire la ligne vanilla "SCP-049-2" visible lorsque l'on vise le joueur
            // et la remplace par le CustomInfo "SCP-457".
            player.InfoArea = (player.InfoArea | PlayerInfoArea.CustomInfo) & ~PlayerInfoArea.Role;

            if (Config.OverrideDisplayName)
                player.DisplayName = Config.DisplayName;

            _fireIntensity[player.UserId] = 100f;

            player.SendHint(
                "<size=32><color=#ff5a00><b>SCP-457</b></color></size>\n" +
                "Tes attaques de mêlée enflamment les humains.\n" +
                "Configure ta touche Brasier dans Server-Specific Settings.",
                Config.SpawnHintDuration);
        }

        public bool GiveExtinguisher(Player player, out int charges)
        {
            charges = 0;

            if (player == null || !player.IsHuman || string.IsNullOrEmpty(player.UserId))
                return false;

            // L'extincteur est entièrement virtuel : aucun objet vanilla n'est
            // ajouté à l'inventaire et aucune pièce n'apparaît dans la main.
            charges = Config.ExtinguisherUses;
            _extinguisherCharges[player.UserId] = charges;
            _extinguisherReadyAt.Remove(player.UserId);
            ShowExtinguisherModel(player);

            player.SendHint(
                "<size=28><color=#bdefff><b>EXTINCTEUR RECU</b></color></size>\n" +
                "Équipement virtuel activé : vise directement SCP-457 puis utilise la touche Pulvériser.\n" +
                "Charges : " + charges,
                7f);

            return true;
        }

        internal bool TryUseExtinguisher(Player player, out string message)
        {
            message = string.Empty;

            if (player == null || !player.IsHuman || string.IsNullOrEmpty(player.UserId))
            {
                message = "Seuls les humains peuvent utiliser un extincteur.";
                return false;
            }

            if (!_extinguisherCharges.TryGetValue(player.UserId, out int uses))
            {
                message = "Tu ne possèdes pas d'extincteur.";
                return false;
            }

            if (uses <= 0)
            {
                message = "L'extincteur est vide.";
                return false;
            }

            float now = Time.unscaledTime;

            if (_extinguisherReadyAt.TryGetValue(player.UserId, out float readyAt) && now < readyAt)
            {
                message = "Attends " + Math.Ceiling(readyAt - now) + " seconde(s).";
                return false;
            }

            _extinguisherReadyAt[player.UserId] = now + Config.ExtinguisherCooldown;
            uses--;
            _extinguisherCharges[player.UserId] = uses;

            Player target = FindAimedScp457(player);

            if (target == null)
            {
                message = "Aucun SCP-457 dans le jet. Charges restantes : " + uses + ".";
                return false;
            }

            float intensity = _fireIntensity.TryGetValue(target.UserId, out float currentIntensity)
                ? currentIntensity
                : 100f;

            intensity = Mathf.Max(0f, intensity - Config.ExtinguisherPower);
            _fireIntensity[target.UserId] = intensity;

            if (intensity <= 0f)
            {
                target.SendHint(
                    "<size=34><color=#8ee8ff><b>TU AS ETE ETEINT</b></color></size>",
                    4f);

                target.Damage(50000f, Config.ExtinguisherDeathReason);
                message = "SCP-457 est complètement éteint !";
                return true;
            }

            target.SendHint(
                "<color=#8ee8ff><b>Un extincteur te refroidit !</b></color>\n" +
                "Intensité du feu : " + Math.Ceiling(intensity) + "%",
                3f);

            message = "SCP-457 touché. Intensité restante : " + Math.Ceiling(intensity) +
                "%. Charges : " + uses + ".";
            return true;
        }

        private Player FindAimedScp457(Player user)
        {
            Player bestTarget = null;
            float bestAngle = Config.ExtinguisherAimAngle;
            Vector3 origin = user.Camera.position;
            Vector3 forward = user.Camera.forward;

            foreach (Player candidate in Player.List)
            {
                if (!IsScp457(candidate))
                    continue;

                Vector3 direction = candidate.Position + Vector3.up - origin;

                float distanceSquared = direction.sqrMagnitude;

                if (distanceSquared > Config.ExtinguisherRange * Config.ExtinguisherRange)
                    continue;

                float angle = Vector3.Angle(forward, direction);

                if (angle > bestAngle)
                    continue;

                bestAngle = angle;
                bestTarget = candidate;
            }

            // Aucun secours par proximité : le joueur doit réellement garder
            // SCP-457 dans le viseur pour que la pulvérisation fonctionne.
            return bestTarget;
        }

        internal bool TryUseFireBurst(Player player, out float remainingCooldown, out int ignitedPlayers)
        {
            remainingCooldown = 0f;
            ignitedPlayers = 0;

            if (!IsScp457(player) || string.IsNullOrEmpty(player.UserId))
                return false;

            float now = Time.unscaledTime;

            if (_fireBurstReadyAt.TryGetValue(player.UserId, out float readyAt) && now < readyAt)
            {
                remainingCooldown = readyAt - now;
                return false;
            }

            float radiusSquared = Config.FireBurstRadius * Config.FireBurstRadius;

            foreach (Player target in Player.List)
            {
                if (target == null || !target.IsHuman || target == player)
                    continue;

                if ((target.Position - player.Position).sqrMagnitude > radiusSquared)
                    continue;

                Ignite(target);
                ignitedPlayers++;
            }

            if (ignitedPlayers == 0)
                return false;

            _fireBurstReadyAt[player.UserId] = now + Config.FireBurstCooldown;
            return true;
        }

        private void OnPlayerHurting(PlayerHurtingEventArgs ev)
        {
            Player attacker = ev.Attacker;
            Player target = ev.Player;

            if (!IsScp457(attacker))
                return;

            if (target == null || !target.IsHuman)
                return;

            Ignite(target);
        }

        private void OnPlayerSpawned(PlayerSpawnedEventArgs ev)
        {
            Player player = ev.Player;

            if (player == null || string.IsNullOrEmpty(player.UserId))
                return;

            RemoveExtinguisher(player.UserId);

            if (_scp457Players.Contains(player.UserId))
            {
                if (player.Role == RoleTypeId.Scp0492)
                {
                    ApplyScp457Stats(player.UserId);
                }
                else
                {
                    RemoveScp457(player);
                }
            }
        }

        private void OnPlayerDying(PlayerDyingEventArgs ev)
        {
            Player player = ev.Player;

            if (player != null && !string.IsNullOrEmpty(player.UserId))
                RemoveExtinguisher(player.UserId);

            if (player != null && !string.IsNullOrEmpty(player.UserId) && _scp457Players.Contains(player.UserId))
                RemoveScp457(player);
        }

        private void OnPlayerLeft(PlayerLeftEventArgs ev)
        {
            Player player = ev.Player;

            if (player == null || string.IsNullOrEmpty(player.UserId))
                return;

            RemoveExtinguisher(player.UserId);
            RemoveScp457(player);
        }

        private void ShowExtinguisherModel(Player player)
        {
            if (player == null || string.IsNullOrEmpty(player.UserId))
                return;

            DestroyExtinguisherModel(player.UserId);

            try
            {
                _extinguisherModels[player.UserId] = Scp457ExtinguisherModel.Create(player, Config);
            }
            catch (Exception exception)
            {
                LabApi.Features.Console.Logger.Error("Impossible de creer le modele de l'extincteur : " + exception);
            }
        }

        private void DestroyExtinguisherModel(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                return;

            if (_extinguisherModels.TryGetValue(userId, out Scp457ExtinguisherModel model))
            {
                model.Destroy();
                _extinguisherModels.Remove(userId);
            }
        }

        private void RemoveExtinguisher(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                return;

            _extinguisherCharges.Remove(userId);
            _extinguisherReadyAt.Remove(userId);
            DestroyExtinguisherModel(userId);
        }

        private void DestroyAllExtinguisherModels()
        {
            foreach (Scp457ExtinguisherModel model in _extinguisherModels.Values)
                model.Destroy();

            _extinguisherModels.Clear();
        }

        internal void Ignite(Player target)
        {
            if (target == null || string.IsNullOrEmpty(target.UserId))
                return;

            target.EnableEffect<Burned>(1, Config.BurnDuration, false);

            target.SendHint(
                "<color=#ff4500><b>Tu brûles !</b></color>\n" +
                "SCP-457 t'a enflammé.",
                2.5f);

            _controller?.StartBurn(target.UserId);
        }

        private void RemoveScp457(Player player)
        {
            if (player == null || string.IsNullOrEmpty(player.UserId))
                return;

            string userId = player.UserId;

            _scp457Players.Remove(userId);
            _controller?.StopBurn(userId);
            _fireBurstReadyAt.Remove(userId);
            _fireIntensity.Remove(userId);

            if (_originalDisplayNames.TryGetValue(userId, out string oldDisplayName))
            {
                player.DisplayName = oldDisplayName;
                _originalDisplayNames.Remove(userId);
            }

            if (_originalCustomInfos.TryGetValue(userId, out string oldCustomInfo))
            {
                player.CustomInfo = oldCustomInfo;
                _originalCustomInfos.Remove(userId);
            }

            if (_originalInfoAreas.TryGetValue(userId, out PlayerInfoArea oldInfoArea))
            {
                player.InfoArea = oldInfoArea;
                _originalInfoAreas.Remove(userId);
            }
        }

        private void RestoreAllPlayerPresentation()
        {
            foreach (KeyValuePair<string, string> pair in _originalDisplayNames)
            {
                Player player = Player.Get(pair.Key);

                if (player != null)
                    player.DisplayName = pair.Value;
            }

            foreach (KeyValuePair<string, string> pair in _originalCustomInfos)
            {
                Player player = Player.Get(pair.Key);

                if (player != null)
                    player.CustomInfo = pair.Value;
            }

            foreach (KeyValuePair<string, PlayerInfoArea> pair in _originalInfoAreas)
            {
                Player player = Player.Get(pair.Key);

                if (player != null)
                    player.InfoArea = pair.Value;
            }
        }
    }
}
