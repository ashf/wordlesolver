namespace WordleSolver;

public class GuessCombination : IEquatable<GuessCombination>
{
    public HashSet<string> PossibleGuesses { get; }
    private readonly int _hashCode;

    public GuessCombination(HashSet<string> possibleGuesses)
    {
        PossibleGuesses = possibleGuesses;

        var hashcode = new HashCode();
        foreach (var guess in PossibleGuesses)
        {
            hashcode.Add(guess.GetHashCode());
        }
        _hashCode = hashcode.ToHashCode();
    }

    public string ElementAt(int index)
    {
        return PossibleGuesses.ElementAt(index);
    }

    public bool Equals(GuessCombination? other)
    {
        return this.PossibleGuesses.SequenceEqual(other.PossibleGuesses);
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as GuessCombination);
    }

    public override int GetHashCode()
    {
        return _hashCode;
    }
}