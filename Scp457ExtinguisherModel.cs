using System.Collections.Generic;
using AdminToys;
using LabApi.Features.Wrappers;
using Mirror;
using UnityEngine;
using PrimitiveToy = LabApi.Features.Wrappers.PrimitiveObjectToy;

namespace Scp457
{
    /// <summary>
    /// Petit extincteur construit avec les primitives natives du jeu.
    /// Il utilise un ancrage fixe et fiable près de la main droite du joueur.
    /// </summary>
    internal sealed class Scp457ExtinguisherModel
    {
        private readonly List<AdminToy> _parts = new List<AdminToy>();
        private PrimitiveToy _modelRoot;

        public static Scp457ExtinguisherModel Create(Player player, Scp457Config config)
        {
            Scp457ExtinguisherModel model = new Scp457ExtinguisherModel();

            try
            {
                Transform playerRoot = player.GameObject.transform;
                Vector3 handPosition = new Vector3(
                    config.ExtinguisherHandAnchorX,
                    config.ExtinguisherHandAnchorY,
                    config.ExtinguisherHandAnchorZ);

                // L'ancrage est directement parenté au joueur. Le suivi d'un os
                // Animator n'est pas fiable côté serveur et plaçait parfois le modèle au sol.
                model._modelRoot = PrimitiveToy.Create(
                    handPosition,
                    Quaternion.Euler(0f, 0f, config.ExtinguisherHandRotationZ),
                    Vector3.one * config.ExtinguisherHandScale,
                    playerRoot,
                    false);
                model._modelRoot.Type = PrimitiveType.Cube;
                model._modelRoot.Flags = PrimitiveFlags.None;
                model._modelRoot.IsStatic = true;
                model._modelRoot.Spawn();
                HideFromOwner(model._modelRoot, player);

                Transform parent = model._modelRoot.Transform;
                Color red = new Color(0.78f, 0.025f, 0.018f, 1f);
                Color darkRed = new Color(0.34f, 0.012f, 0.008f, 1f);
                Color black = new Color(0.015f, 0.015f, 0.015f, 1f);
                Color metal = new Color(0.34f, 0.36f, 0.38f, 1f);
                Color label = new Color(0.92f, 0.92f, 0.86f, 1f);

                // Bouteille compacte suspendue sous la main.
                model.AddPart(player, parent, PrimitiveType.Cylinder,
                    new Vector3(0f, -0.20f, 0f), Quaternion.identity,
                    new Vector3(0.12f, 0.24f, 0.12f), red);
                model.AddPart(player, parent, PrimitiveType.Cylinder,
                    new Vector3(0f, -0.44f, 0f), Quaternion.identity,
                    new Vector3(0.13f, 0.025f, 0.13f), darkRed);
                model.AddPart(player, parent, PrimitiveType.Cylinder,
                    new Vector3(0f, 0.055f, 0f), Quaternion.identity,
                    new Vector3(0.052f, 0.05f, 0.052f), metal);
                model.AddPart(player, parent, PrimitiveType.Cube,
                    new Vector3(0.055f, 0.14f, 0f), Quaternion.Euler(0f, 0f, -8f),
                    new Vector3(0.20f, 0.04f, 0.055f), black);
                model.AddPart(player, parent, PrimitiveType.Cube,
                    new Vector3(0f, -0.20f, 0.125f), Quaternion.identity,
                    new Vector3(0.15f, 0.16f, 0.012f), label);

                // Flexible et embout courts pour éviter de traverser le bras.
                model.AddPart(player, parent, PrimitiveType.Capsule,
                    new Vector3(0.15f, 0.00f, 0f), Quaternion.Euler(0f, 0f, 68f),
                    new Vector3(0.025f, 0.14f, 0.025f), black);
                model.AddPart(player, parent, PrimitiveType.Cube,
                    new Vector3(0.25f, -0.07f, 0f), Quaternion.Euler(0f, 0f, -8f),
                    new Vector3(0.13f, 0.045f, 0.045f), black);

                return model;
            }
            catch
            {
                model.Destroy();
                throw;
            }
        }

        public void Destroy()
        {
            foreach (AdminToy part in _parts)
            {
                if (part != null && !part.IsDestroyed)
                    part.Destroy();
            }

            _parts.Clear();

            if (_modelRoot != null && !_modelRoot.IsDestroyed)
                _modelRoot.Destroy();

            _modelRoot = null;
        }

        private void AddPart(
            Player owner,
            Transform parent,
            PrimitiveType type,
            Vector3 position,
            Quaternion rotation,
            Vector3 scale,
            Color color)
        {
            PrimitiveToy part = PrimitiveToy.Create(position, rotation, scale, parent, false);
            part.Type = type;
            part.Flags = PrimitiveFlags.Visible;
            part.Color = color;
            part.IsStatic = true;
            part.Spawn();
            HideFromOwner(part, owner);
            _parts.Add(part);
        }

        private static void HideFromOwner(AdminToy toy, Player owner)
        {
            if (owner == null || owner.IsHost || owner.ReferenceHub.connectionToClient == null)
                return;

            owner.ReferenceHub.connectionToClient.Send(new ObjectHideMessage
            {
                netId = toy.Base.netIdentity.netId
            });
        }
    }
}
