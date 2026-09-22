using System;
using System.Collections.Generic;
using System.Linq;
using LabApi.Features.Wrappers;
using UnityEngine;
using UserSettings.ServerSpecific;

namespace Scp457
{
    internal sealed class Scp457ServerSpecificSettings
    {
        private const int FireBurstKeyId = 457001;
        private const int ExtinguisherKeyId = 457002;

        private readonly Scp457Plugin _plugin;
        private ServerSpecificSettingBase[] _ownedSettings;

        public Scp457ServerSpecificSettings(Scp457Plugin plugin)
        {
            _plugin = plugin;
        }

        public void Enable()
        {
            _ownedSettings = new ServerSpecificSettingBase[]
            {
                new SSGroupHeader("SCP-457 - Capacites"),
                new SSKeybindSetting(
                    FireBurstKeyId,
                    "Brasier de SCP-457",
                    KeyCode.G,
                    hint: "SCP-457 uniquement : enflamme tous les humains proches. La touche doit etre confirmee dans les parametres du jeu."),
                new SSKeybindSetting(
                    ExtinguisherKeyId,
                    "Pulveriser l'extincteur",
                    KeyCode.H,
                    hint: "Avec un extincteur virtuel, vise SCP-457 et maintiens-toi a moins de 10 metres.")
            };

            List<ServerSpecificSettingBase> settings =
                new List<ServerSpecificSettingBase>(ServerSpecificSettingsSync.DefinedSettings ?? new ServerSpecificSettingBase[0]);

            settings.AddRange(_ownedSettings);
            ServerSpecificSettingsSync.DefinedSettings = settings.ToArray();
            ServerSpecificSettingsSync.Version++;
            ServerSpecificSettingsSync.ServerOnSettingValueReceived += OnSettingValueReceived;
            ServerSpecificSettingsSync.SendToAll();
        }

        public void Disable()
        {
            ServerSpecificSettingsSync.ServerOnSettingValueReceived -= OnSettingValueReceived;

            if (_ownedSettings != null && ServerSpecificSettingsSync.DefinedSettings != null)
            {
                ServerSpecificSettingsSync.DefinedSettings = ServerSpecificSettingsSync.DefinedSettings
                    .Where(setting => !_ownedSettings.Contains(setting))
                    .ToArray();

                ServerSpecificSettingsSync.Version++;
                ServerSpecificSettingsSync.SendToAll();
            }

            _ownedSettings = null;
        }

        private void OnSettingValueReceived(ReferenceHub sender, ServerSpecificSettingBase setting)
        {
            if (setting == null)
                return;

            SSKeybindSetting keybind = setting as SSKeybindSetting;

            if (keybind == null || !keybind.SyncIsPressed)
                return;

            Player player = Player.Get(sender);

            if (setting.SettingId == ExtinguisherKeyId)
            {
                bool success = _plugin.TryUseExtinguisher(player, out string extinguisherMessage);

                player?.SendHint(
                    success
                        ? "<color=#8ee8ff><b>EXTINCTEUR</b></color>\n" + extinguisherMessage
                        : "<color=#d8f5ff>" + extinguisherMessage + "</color>",
                    2.5f);
                return;
            }

            if (setting.SettingId != FireBurstKeyId)
                return;

            if (!_plugin.IsScp457(player))
                return;

            if (_plugin.TryUseFireBurst(player, out float cooldown, out int ignited))
            {
                player.SendHint(
                    "<color=#ff5a00><b>BRASIER ACTIVE</b></color>\n" +
                    ignited + " humain(s) enflamme(s).",
                    3f);
                return;
            }

            if (cooldown > 0f)
            {
                player.SendHint(
                    "<color=#ffae00>Brasier en recharge : " + Math.Ceiling(cooldown) + " seconde(s).</color>",
                    2f);
            }
            else
            {
                player.SendHint(
                    "<color=#ffae00>Aucun humain assez proche.</color>",
                    2f);
            }
        }
    }
}
