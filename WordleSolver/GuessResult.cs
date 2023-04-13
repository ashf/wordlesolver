using System.Text;

namespace WordleSolver;

public enum GuessResult
{
    DoesNotExist = 0,
    ExistsInDifferentSpot = 1,
    Correct = 2,
};

public static class GuessResultExtensions
{
    public static int ToScore(this IEnumerable<GuessResult> hint)
    {
        return hint.Sum(x => (int)x);
    }

    public static string Stringify(this IEnumerable<GuessResult> hint)
    {
        var sb = new StringBuilder();
        foreach (var guessResult in hint)
        {
            sb.Append(guessResult switch
            {
                GuessResult.Correct => "G",
                GuessResult.DoesNotExist => "X",
                GuessResult.ExistsInDifferentSpot => "?",
                _ => throw new ArgumentOutOfRangeException(nameof(hint), hint, null)
            });
        }

        return sb.ToString();
    }
}
