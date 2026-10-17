using System;
using System.Linq;
using UnityEngine;
using WhereDarknessDwells.Core;

namespace WhereDarknessDwells.Content
{
    /// <summary>The villain behind an expedition: the boss, the clues about it, its secret room and the two endings.</summary>
    [CreateAssetMenu(menuName = "Where Darkness Dwells/Villain", fileName = "Villain")]
    public sealed class VillainAsset : ScriptableObject
    {
        [Tooltip("Unique, lower case, never changed once used.")]
        [SerializeField] string id;
        [Tooltip("The boss enemy. It must be in the expedition's boss encounter; any other enemy there is one of its allies.")]
        [SerializeField] EnemyAsset boss;
        [Tooltip("The name the boss goes by until a clue reveals it.")]
        [SerializeField] string hiddenName;
        [SerializeField] ClueAsset[] clues = new ClueAsset[0];
        [SerializeField] SecretRoomData secretRoom = new SecretRoomData();
        [Tooltip("Shown after beating the boss.")]
        [SerializeField, TextArea(2, 5)] string ending;
        [Tooltip("Shown instead after beating the boss with a True Ending clue.")]
        [SerializeField, TextArea(2, 5)] string trueEnding;

        public string Id => id;

        /// <summary>Empty slots are left out here as unknown ids, so validation reports them.</summary>
        public VillainDef ToDef() => new VillainDef(id, boss != null ? boss.Id : "(empty slot)", hiddenName,
            (clues ?? new ClueAsset[0]).Select(c => c != null ? c.ToDef() : new ClueDef("(empty slot)", "(empty slot)", ClueSource.Investigation, 0, 0, 0, "", "", "")),
            secretRoom != null && secretRoom.enabled ? secretRoom.ToDef() : null, ending, trueEnding);
    }

    [Serializable]
    public sealed class SecretRoomData
    {
        [Tooltip("Off if this villain has no secret room.")]
        public bool enabled = true;
        public string name;
        [Tooltip("The map layer it sits on, from 1 to 5. Every room on the layer before leads to it.")]
        [Min(1)] public int layer = 4;
        [TextArea(3, 6)] public string scene;
        [Tooltip("The button, such as \"Destroy the heart\".")]
        public string actionLabel;
        [Min(0)] public int healthCostPerHero = 4;
        [Range(0, 90)] public int bossHealthLossPercent = 30;

        public SecretRoomDef ToDef() => new SecretRoomDef(name, layer, scene, actionLabel, healthCostPerHero, bossHealthLossPercent);
    }
}
