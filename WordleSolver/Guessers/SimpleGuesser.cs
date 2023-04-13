namespace WordleSolver.Guessers;

public class SimpleGuesser : Guesser
{
    public override string Name => nameof(SimpleGuesser);

    public override async Task<string> Guess(
        HashSet<string> possibleGuesses,
        HashSet<string> possibleSolutions,
        IReadOnlySet<char> reds,
        char[] greens,
        IReadOnlyDictionary<char, bool[]> yellows)
    {
        var scores = Enumerable.Repeat(int.MinValue, possibleGuesses.Count).ToArray();

        var tasks = possibleGuesses
            .Select((guess, i) => Task.Run(() =>
            {
                var score = possibleSolutions.Sum(actual => EvaluateGuess(guess, actual).ToScore());

                scores[i] = score;
            }));

        await Task.WhenAll(tasks);

        var bestAverageWordIndex = Array.IndexOf(scores, scores.Max());

        return possibleGuesses.ElementAt(bestAverageWordIndex);
    }
}