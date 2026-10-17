namespace WhereDarknessDwells.Core
{
    /// <summary>The one place attack damage is worked out, so every attack follows the same order.</summary>
    public static class DamageRules
    {
        /// <summary>
        /// Attack damage before Block: the base (with any flat bonus already added), then Weak on the attacker
        /// (x0.75), then Exposed on the defender (x1.5), rounded down once at the end.
        /// </summary>
        public static int Attack(int baseDamage, bool attackerWeak, bool defenderExposed)
        {
            if (baseDamage <= 0) return 0;
            int numerator = baseDamage, denominator = 1;
            if (attackerWeak) { numerator *= 3; denominator *= 4; }
            if (defenderExposed) { numerator *= 3; denominator *= 2; }
            return numerator / denominator;
        }
    }
}
