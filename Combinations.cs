namespace WordleSolver;

// https://stackoverflow.com/a/54268753
public static class Combinations
{
    private static void SetIndexes(IList<int> indexes, int lastIndex, int count)
    {
        indexes[lastIndex]++;
        if (lastIndex <= 0 || indexes[lastIndex] != count)
        {
            return;
        }

        SetIndexes(indexes, lastIndex - 1, count - 1);
        indexes[lastIndex] = indexes[lastIndex - 1] + 1;
    }

    private static bool AllPlacesChecked(IReadOnlyList<int> indexes, int places)
    {
        for (var i = indexes.Count - 1; i >= 0; i--)
        {
            if (indexes[i] != places)
            {
                return false;
            }

            places--;
        }
        return true;
    }

    public static IEnumerable<T[]> GetDifferentCombinations<T>(this IEnumerable<T> c, int count)
    {
        var collection = c.ToList();
        var listCount = collection.Count;

        if (count > listCount)
            throw new InvalidOperationException($"{nameof(count)} is greater than the collection elements.");

        var indexes = Enumerable.Range(0, count).ToArray();

        do
        {
            yield return indexes.Select(i => collection[i]).ToArray();

            SetIndexes(indexes, indexes.Length - 1, listCount);
        }
        while (!AllPlacesChecked(indexes, listCount));
    }
}