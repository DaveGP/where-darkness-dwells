using System;
using System.Collections.Generic;

namespace WhereDarknessDwells.Core
{
    /// <summary>
    /// Everything needed to carry on a run between rooms, as plain fields the Game layer can write to a file. The map
    /// isn't here: the seed builds it again. Take one with <see cref="RunState.TrySnapshot"/> and load it with
    /// <see cref="RunState.TryRestore"/>.
    /// </summary>
    [Serializable]
    public sealed class RunSnapshot
    {
        /// <summary>Bumped when the fields change, so an old save is refused instead of misread.</summary>
        public const int CurrentVersion = 1;

        public int Version = CurrentVersion;
        public string ExpeditionId;
        /// <summary>The run seed and the reward stream's position, as decimal text so any JSON reader keeps every digit.</summary>
        public string Seed;
        public string RewardRngState;
        public List<HeroSnapshot> Heroes = new List<HeroSnapshot>();
        public List<CardSnapshot> Deck = new List<CardSnapshot>();
        public int NextCardId;
        public List<string> Keepsakes = new List<string>();
        public List<string> Clues = new List<string>();
        public List<string> EventsSeen = new List<string>();
        public List<int> Route = new List<int>();
        public int FightsWon;
        public bool SecretUsed;
        /// <summary>The room the party was entering when this was taken, or -1. Restoring enters it again.</summary>
        public int EnteringRoomId = -1;
    }

    [Serializable]
    public sealed class HeroSnapshot
    {
        public string Id;
        public int Health;
        public int MaxHealthBonus;
    }

    [Serializable]
    public sealed class CardSnapshot
    {
        public int Id;
        public string DefId;
    }
}
