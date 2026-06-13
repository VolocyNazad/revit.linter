public static class CollectionExtensions
{
    extension<T>(IEnumerable<T> first)
    {
        public bool SetEquals(IEnumerable<T> second)
        => first.Count() == second.Count() &&
            !first.Except(second).Any() &&
            !second.Except(first).Any();
    }
}
