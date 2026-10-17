namespace WhereDarknessDwells.Core
{
    /// <summary>
    /// One copy of a card in the party's deck. Two copies of the same card are two instances with different ids.
    /// Anything that changes a card during a run belongs here, never on its <see cref="CardDef"/>.
    /// </summary>
    public sealed class CardInstance
    {
        public readonly int Id;
        public readonly string DefId;

        public CardInstance(int id, string defId)
        {
            Id = id;
            DefId = defId;
        }

        public override string ToString() => $"{DefId}#{Id}";
    }
}
