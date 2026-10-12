using System;
using UnityEngine;

namespace ProjectZ.Classes
{
    public enum ClassId : byte
    {
        Hacker = 0,
        SysAdmin = 1,
        NetworkEngineer = 2,
        SecurityAnalyst = 3
    }

    public enum AbilityId : byte
    {
        None = 0,
        ExploitFamiliarity = 1,
        KernelPanic = 2,
        Failover = 3,
        Hotfix = 4,
        PriorityQueue = 5,
        DropFirewall = 6,
        SignatureMatch = 7,
        Quarantine = 8
    }

    [Serializable]
    public class AbilitySpec
    {
        public AbilityId abilityId = AbilityId.None;
        public string displayName = "";
        [TextArea(2, 4)] public string description = "";

        [Tooltip("Seconds before this skill can be used again. Passives stay at 0.")]
        public float cooldownSeconds;

        [Tooltip("How long the effect lasts. Kernel Panic freeze, Firewall lifetime, Quarantine outline.")]
        public float durationSeconds;

        [Tooltip("Reach of a targeted skill, in meters. Hotfix uses this.")]
        public float rangeMeters;

        [Tooltip("Radius or width, in meters. Quarantine pulse, Firewall width, Priority Queue aura.")]
        public float radiusMeters;

        [Tooltip("Stored charges. Drop Firewall uses 2. Other skills stay at 0 or 1.")]
        public int charges = 1;

        [Tooltip("Skill-specific strength. Heal fraction, damage-taken multiplier, slow multiplier, or revive health fraction.")]
        public float magnitude = 1f;

        [Tooltip("How many wrong submissions this passive forgives. Exploit Familiarity uses 1.")]
        public int forgivenMistakes;
    }

    [Serializable]
    public class CharacterStats
    {
        public int maxHealth = 100;
        public float moveSpeed = 5f;
        [Tooltip("Seconds a downed player can be revived before they bleed out.")]
        public float bleedOutSeconds = 20f;
        [Tooltip("Seconds to revive another player. Failover shortens this for the SysAdmin.")]
        public float reviveSeconds = 5f;
    }

    [CreateAssetMenu(fileName = "Character", menuName = "Project Z/Character")]
    public class CharacterData : ScriptableObject
    {
        [SerializeField] private ClassId classId = ClassId.Hacker;
        [SerializeField] private string displayName = "Hacker";
        [SerializeField] private string roleSummary = "";
        [SerializeField, TextArea(3, 6)] private string lore = "";
        [SerializeField] private CharacterStats stats = new CharacterStats();
        [SerializeField] private AbilitySpec passive = new AbilitySpec();
        [SerializeField] private AbilitySpec active = new AbilitySpec();

        public ClassId ClassId => classId;
        public string DisplayName => displayName;
        public string RoleSummary => roleSummary;
        public string Lore => lore;
        public CharacterStats Stats => stats;
        public AbilitySpec Passive => passive;
        public AbilitySpec Active => active;
    }

    [CreateAssetMenu(fileName = "CharacterCatalog", menuName = "Project Z/Character Catalog")]
    public class CharacterCatalog : ScriptableObject
    {
        public const int RequiredClassCount = 4;

        [SerializeField] private CharacterData[] characters = new CharacterData[RequiredClassCount];

        public int Count => characters != null ? characters.Length : 0;

        public bool TryGet(ClassId id, out CharacterData data)
        {
            data = null;
            if (characters == null)
            {
                return false;
            }

            for (int i = 0; i < characters.Length; i++)
            {
                CharacterData candidate = characters[i];
                if (candidate != null && candidate.ClassId == id)
                {
                    data = candidate;
                    return true;
                }
            }

            return false;
        }

        public bool TryGetAt(int index, out CharacterData data)
        {
            data = null;
            if (characters == null || index < 0 || index >= characters.Length)
            {
                return false;
            }

            data = characters[index];
            return data != null;
        }

        public bool HasUniqueRoster()
        {
            if (characters == null || characters.Length != RequiredClassCount)
            {
                return false;
            }

            int seen = 0;
            for (int i = 0; i < characters.Length; i++)
            {
                CharacterData data = characters[i];
                if (data == null)
                {
                    return false;
                }

                int bit = 1 << (int)data.ClassId;
                if ((seen & bit) != 0)
                {
                    return false;
                }

                seen |= bit;
            }

            return seen == (1 << RequiredClassCount) - 1;
        }

        private void OnValidate()
        {
            if (HasUniqueRoster())
            {
                return;
            }

            Debug.LogWarning("CharacterCatalog needs exactly one asset for each of the 4 classes.", this);
        }
    }
}
