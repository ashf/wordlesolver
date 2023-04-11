﻿namespace WordleSolver.Guessers;

public abstract class Guesser
{
    public abstract string Name { get; }

    public abstract Task<string> Guess(
        IReadOnlyList<string> possibleGuesses,
        IList<string> possibleSolutions,
        IReadOnlySet<char> reds,
        IList<char> greens,
        IReadOnlyDictionary<char, List<bool>> yellows);

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