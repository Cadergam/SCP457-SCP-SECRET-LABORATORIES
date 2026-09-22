using System;
using CommandSystem;
using LabApi.Features.Permissions;
using LabApi.Features.Wrappers;

namespace Scp457.Commands
{
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public sealed class Scp457Command : ICommand
    {
        public string Command => "scp457";
        public string[] Aliases => new[] { "457" };
        public string Description => "Transforme un joueur en SCP-457 : scp457 <PlayerID>";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            Player executor = Player.Get(sender);

            if (executor != null && !executor.HasPermission("scp457.spawn"))
            {
                response = "Permission requise : scp457.spawn";
                return false;
            }

            if (arguments.Count < 1 || arguments.Array == null)
            {
                response = "Utilisation : scp457 <PlayerID>";
                return false;
            }

            string rawId = arguments.Array[arguments.Offset];

            if (!int.TryParse(rawId, out int playerId))
            {
                response = "Le PlayerID doit etre un nombre. Exemple : scp457 3";
                return false;
            }

            Player target = Player.Get(playerId);

            if (target == null)
            {
                response = "Joueur introuvable.";
                return false;
            }

            if (Scp457Plugin.Instance == null)
            {
                response = "Le plugin SCP-457 n'est pas charge.";
                return false;
            }

            Scp457Plugin.Instance.SpawnScp457(target);

            response = $"{target.Nickname} devient SCP-457.";
            return true;
        }
    }
}
