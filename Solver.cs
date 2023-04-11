using WordleSolver.Guessers;

namespace WordleSolver;

public static class Solver
{
    public static async Task<List<string>> Play(
        Guesser guesser,
        List<string> possibleGuesses,
        List<string> possibleSolutions,
        string answer)
    {
        var guesses = new List<string>();

        var reds = new HashSet<char>();
        var greens = Enumerable.Repeat('-', answer.Length).ToList();
        var yellows = new Dictionary<char, List<bool>>();
        for (var c = 'a'; c <= 'z'; c++)
        {
            yellows[c] = Enumerable.Repeat(false, answer.Length).ToList();
        }

        // Console.WriteLine($"Possible words: {possibleSolutions.Count}");
        var guess = "tarse";
        // var guess = await guesser.Guess(possibleGuesses, possibleSolutions, reds, greens, yellows);
        possibleSolutions.Remove(guess);
        possibleGuesses.Remove(guess);
        guesses.Add(guess);

        var guessNum = 1;

        var solved = false;

        while (!solved)
        {
            var hint = Guesser.EvaluateGuess(guess, answer);

            // Console.WriteLine($"guess #{guessNum}: {guess}");
            hint.Print(guess);

            if (guess == answer)
            {
                Console.WriteLine($"{guesser.Name}:  Correct in {guessNum} tries");
                solved = true;
            }
            else
            {
                UpdateKnownsInplace(hint, guess, reds, greens, yellows);
                possibleSolutions = FilterWordList(reds, greens, yellows, possibleSolutions);

                if (!possibleSolutions.Any())
                {
                    await Console.Error.WriteLineAsync($"Impossible, no words left to guess. Answer was \"{answer}\"");
                    Environment.Exit(-1);
                }

                if (!possibleSolutions.Contains(answer))
                {
                    await Console.Error.WriteLineAsync($"Impossible, solver was unable to find the answer. Answer was \"{answer}\"");
                    Environment.Exit(-1);
                }

                // Console.WriteLine($"Possible words: {possibleSolutions.Count}");

                guess = await guesser.Guess(possibleGuesses, possibleSolutions, reds, greens, yellows);
                possibleSolutions.Remove(guess);
                possibleGuesses.Remove(guess);
                guesses.Add(guess);
                guessNum++;
            }
        }

        return guesses;
    }

    public static void UpdateKnownsInplace(
        IReadOnlyList<GuessResult> hint,
        string guess,
        ISet<char> reds,
        IList<char> greens,
        IReadOnlyDictionary<char, List<bool>> yellows)
    {
        for (var i = 0; i < hint.Count; i++)
        {
            if (hint[i] == GuessResult.Correct)
            {
                greens[i] = guess[i];
            }

            if (hint[i] == GuessResult.DoesNotExist)
            {
                reds.Add(guess[i]);
            }

            if (hint[i] == GuessResult.ExistsInDifferentSpot)
            {
                yellows[guess[i]][i] = true;
            }
        }
    }

    private static List<string> FilterWordList(
        IReadOnlySet<char> reds,
        IList<char> greens,
        IReadOnlyDictionary<char, List<bool>> yellows,
        IEnumerable<string> wordList)
    {
        return wordList.Where(word => IsWordPossible(word.ToCharArray(), reds, greens, yellows)).ToList();
    }

    public static bool IsWordPossible(
        IList<char> word,
        IReadOnlySet<char> reds,
        IList<char> greens,
        IReadOnlyDictionary<char, List<bool>> yellows)
    {
        for (var i = 0; i < word.Count; i++)
        {
            var letter = word[i];

            if (greens[i] == letter)
            {
                continue;
            }

            if (reds.Contains(letter))
            {
                return false;
            }

            if (yellows[letter][i])
            {
                return false;
            }
        }

        return true;
    }
}