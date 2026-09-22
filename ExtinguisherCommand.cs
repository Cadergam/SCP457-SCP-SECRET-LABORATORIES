using System;
using CommandSystem;
using LabApi.Features.Permissions;
using LabApi.Features.Wrappers;

namespace Scp457.Commands
{
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public sealed class ExtinguisherCommand : ICommand
    {
        public string Command => "extincteur";
        public string[] Aliases => new[] { "extinguisher", "giveextincteur" };
        public string Description => "Donne un extincteur a un joueur : extincteur <PlayerID>";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            Player executor = Player.Get(sender);

            if (executor != null && !executor.HasPermission("scp457.extinguisher"))
            {
                response = "Permission requise : scp457.extinguisher";
                return false;
            }

            if (arguments.Count < 1 || arguments.Array == null)
            {
                response = "Utilisation : extincteur <PlayerID>";
                return false;
            }

            string rawId = arguments.Array[arguments.Offset];

            if (!int.TryParse(rawId, out int playerId))
            {
                response = "Le PlayerID doit etre un nombre. Exemple : extincteur 3";
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

            if (!Scp457Plugin.Instance.GiveExtinguisher(target, out int charges))
            {
                response = "Impossible de donner l'extincteur. Le joueur doit etre humain.";
                return false;
            }

            response = "Extincteur virtuel donne a " + target.Nickname + " (" + charges + " charges).";
            return true;
        }
    }
}
