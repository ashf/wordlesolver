using WordleSolver.Guessers;

namespace WordleSolver;

public static class Solver
{
    public static async Task<List<string>> Play(
        Guesser guesser,
        List<string> possibleGuesses,
        List<string> possibleSolutions,
        string solution)
    {
        var guesses = new List<string>();

        // init knowns
        var reds = new HashSet<char>();
        var greens = Enumerable.Repeat('-', solution.Length).ToArray();
        var yellows = new Dictionary<char, bool[]>();
        for (var c = 'a'; c <= 'z'; c++)
        {
            yellows[c] = Enumerable.Repeat(false, solution.Length).ToArray();
        }

        // Initial guess
        // Console.WriteLine($"Possible words: {possibleSolutions.Count}");
        var guess = "tarse"; // start with a guess that is known to be good
        // var guess = await guesser.Guess(possibleGuesses, possibleSolutions, reds, greens, yellows);

        var solved = false;

        // Guess until solved
        while (!solved)
        {
            // update hint and state of knowns
            var hint = Guesser.EvaluateGuess(guess, solution);

            UpdateState(guess, ref guesses, ref possibleGuesses, ref possibleSolutions, hint, ref reds, ref greens, ref yellows);

            guess.PrintResult(hint, possibleSolutions.Count);

            if (guess == solution)
            {
                Console.WriteLine($"{guesser.Name}: Correct in {guesses.Count} guesses");
                solved = true;
            }
            else
            {
                if (!possibleSolutions.Any())
                {
                    await Console.Error.WriteLineAsync($"Impossible, no words left to guess. Answer was \"{solution}\"");
                    Environment.Exit(-1);
                }

                if (!possibleSolutions.Contains(solution))
                {
                    await Console.Error.WriteLineAsync($"Impossible, solver was unable to find the answer. Answer was \"{solution}\"");
                    Environment.Exit(-1);
                }

                // Console.WriteLine($"Possible words: {possibleSolutions.Count}");

                guess = await guesser.Guess(possibleGuesses, possibleSolutions, reds, greens, yellows);
            }
        }

        return guesses;
    }

    private static void UpdateState(
        string guess,
        ref List<string> guesses,
        ref List<string> possibleGuesses,
        ref List<string> possibleSolutions,
        IReadOnlyList<GuessResult> hint,
        ref HashSet<char> reds,
        ref char[] greens,
        ref Dictionary<char, bool[]> yellows)
    {
        guesses.Add(guess);
        possibleGuesses.Remove(guess);

        possibleSolutions.Remove(guess);

        UpdateKnownsInplace(guess, hint, reds, greens, yellows);

        possibleSolutions = possibleSolutions.FilterToPossibleWords(reds, greens, yellows, true);

        // shouldn't need to filter guess list here, but it tends to give a better result
        possibleGuesses = possibleGuesses.FilterToPossibleWords(reds, greens, yellows, true);
    }

    public static void UpdateKnownsInplace(
        string guess,
        IReadOnlyList<GuessResult> hint,
        ISet<char> reds,
        char[] greens,
        IReadOnlyDictionary<char, bool[]> yellows)
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

    private static List<string> FilterToPossibleWords(
        this IEnumerable<string> wordList,
        ISet<char> reds,
        IList<char> greens,
        IReadOnlyDictionary<char, bool[]> yellows,
        bool excludeYellows)
    {
        return wordList.Where(word => IsWordPossible(word.ToCharArray(), reds, greens, yellows, excludeYellows)).ToList();
    }

    public static bool IsWordPossible(
        IList<char> word,
        ISet<char> reds,
        IList<char> greens,
        IReadOnlyDictionary<char, bool[]> yellows,
        bool exludeYellows)
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

            if (exludeYellows && yellows[letter][i])
            {
                return false;
            }
        }

        return true;
    }

    private static void PrintResult(this string guess, GuessResult[] hint, int wordsLeft)
    {
        for (var index = 0; index < guess.Length; index++)
        {
            var color = hint[index] switch
            {
                GuessResult.Correct => ConsoleColor.Green,
                GuessResult.DoesNotExist => ConsoleColor.Red,
                GuessResult.ExistsInDifferentSpot => ConsoleColor.Yellow,
                _ => throw new ArgumentOutOfRangeException(nameof(hint), hint, null)
            };

            Console.ForegroundColor = color;
            Console.Write(guess[index]);
        }

        Console.ResetColor();

        Console.WriteLine($" : {wordsLeft} words left");
    }

    public static async Task<(IEnumerable<string> guesses, int wordsLeft)> SolutionsAfterThreeGuesses(
        IEnumerable<string[]> possibleGuessesSubsets,
        List<string> possibleSolutions,
        string solution)
    {
        var result = new Dictionary<string[], int>();

        var tasks = possibleGuessesSubsets.Select(possibleGuessesSubset => Task.Run(() =>
        {
            // init knowns
            var reds = new HashSet<char>();
            var greens = Enumerable.Repeat('-', solution.Length).ToArray();
            var yellows = new Dictionary<char, bool[]>();
            for (var c = 'a'; c <= 'z'; c++)
            {
                yellows[c] = Enumerable.Repeat(false, solution.Length).ToArray();
            }

            var possibleGuesses = new List<string>();

            var guesses = new List<string>();

            foreach (var guess in possibleGuessesSubset)
            {
                var hint = Guesser.EvaluateGuess(guess, solution);

                UpdateState(guess, ref guesses, ref possibleGuesses, ref possibleSolutions, hint, ref reds, ref greens,
                    ref yellows);
            }

            result[possibleGuessesSubset] = possibleSolutions.Count;
            // Console.WriteLine($"guesses: {string.Join(", ", guesses)} - {possibleSolutions.Count} words left");
        }));

        await Task.WhenAll(tasks);

        var minResult = result.MinBy(x => x.Value);
        return (minResult.Key.OrderBy(x => x), minResult.Value);
    }

    public static IEnumerable<string[]> GetCombinations(this IList<string> source)
    {
        var subsets = new HashSet<string>();
        for (var i = 0; i < source.Count; i++)
        {
            for (var j = 0; j < source.Count; j++)
            {
                if (i == j)
                {
                    continue;
                }

                for (var k = 0; k < source.Count; k++)
                {
                    if (k == i || k == j)
                    {
                        continue;
                    }

                    var subset = new [] {source[i] , source[j] , source[k]};

                    subsets.Add(string.Join(',',subset.OrderBy(x => x)));
                }
            }
        }

        return subsets.Select(x => x.Split(','));
    }
}