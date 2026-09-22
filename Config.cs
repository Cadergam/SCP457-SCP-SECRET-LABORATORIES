namespace Scp457
{
    public sealed class Scp457Config
    {
        public float Health { get; set; } = 2200f;
        public float BurnDuration { get; set; } = 8f;
        public float BurnDamagePerTick { get; set; } = 7f;
        public float BurnTickInterval { get; set; } = 1f;

        public float FireBurstRadius { get; set; } = 6f;
        public float FireBurstCooldown { get; set; } = 25f;

        public int ExtinguisherUses { get; set; } = 8;
        public float ExtinguisherPower { get; set; } = 20f;
        public float ExtinguisherRange { get; set; } = 10f;
        public float ExtinguisherAimAngle { get; set; } = 6f;
        public float ExtinguisherCooldown { get; set; } = 1.25f;
        public string ExtinguisherDeathReason { get; set; } = "SCP-457 a ete eteint avec un extincteur";

        // Anciens paramètres conservés pour accepter les fichiers YAML des versions précédentes.
        // Ils ne sont plus utilisés afin qu'une ancienne valeur mémorisée ne replace pas le modèle au plafond.
        public float ExtinguisherModelX { get; set; } = 0.18f;
        public float ExtinguisherModelY { get; set; } = 0.30f;
        public float ExtinguisherModelZ { get; set; } = 0.24f;
        public float ExtinguisherModelRotationZ { get; set; } = -8f;
        public float ExtinguisherModelScale { get; set; } = 0.72f;

        // Nouvel ancrage local près de la main droite. Ces noms forcent LabAPI à
        // créer des valeurs propres même si l'ancien fichier de configuration existe déjà.
        public float ExtinguisherHandAnchorX { get; set; } = 0.18f;
        public float ExtinguisherHandAnchorY { get; set; } = 0.30f;
        public float ExtinguisherHandAnchorZ { get; set; } = 0.24f;
        public float ExtinguisherHandRotationZ { get; set; } = -8f;
        public float ExtinguisherHandScale { get; set; } = 0.72f;

        public bool OverrideDisplayName { get; set; } = true;
        public string DisplayName { get; set; } = "SCP-457";

        public string CustomInfo { get; set; } = "SCP-457";

        public float SpawnHintDuration { get; set; } = 8f;
        public string BurnDeathReason { get; set; } = "Brule par SCP-457";
    }
}
