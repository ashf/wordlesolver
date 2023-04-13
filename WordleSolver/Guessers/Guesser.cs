namespace WordleSolver.Guessers;

public abstract class Guesser
{
    public abstract string Name { get; }

    public abstract Task<string> Guess(
        HashSet<string> possibleGuesses,
        HashSet<string> possibleSolutions,
        IReadOnlySet<char> reds,
        char[] greens,
        IReadOnlyDictionary<char, bool[]> yellows);

    protected internal static GuessResult[] EvaluateGuess(string guess, string answer)
    {
        var result = Enumerable.Repeat(GuessResult.DoesNotExist, answer.Length).ToArray();

        for (var i = 0; i < result.Length; i++)
        {
            var guessLetter = guess[i];
            var answerLetter = answer[i];

            if (answerLetter == guessLetter)
            {
                result[i] = GuessResult.Correct;
            }
            else if (guessLetter != answerLetter && answer.Contains(guessLetter))
            {
                result[i] = GuessResult.ExistsInDifferentSpot;
            }
        }

        return result;
    }
}