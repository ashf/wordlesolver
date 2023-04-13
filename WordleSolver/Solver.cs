using System.Diagnostics;
using System.Text.Json;
using WordleSolver.Guessers;

namespace WordleSolver;

public static class Solver
{
    public static async Task<List<string>> Play(
        Guesser guesser,
        HashSet<string> possibleGuesses,
        HashSet<string> possibleSolutions,
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
        // var guess = await guesser.Guess(possibleGuesses, possibleSolutions, reds, yellows);

        var solved = false;

        // Guess until solved
        while (!solved)
        {
            var hint = GuessAndUpdateState(
                solution,
                guess,
                new List<string>(), // not needed
                possibleGuesses,
                possibleSolutions,
                reds,
                greens,
                yellows,
                updatePossibleGuesses: false);

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
        ICollection<string> guesses,
        HashSet<string> possibleGuesses,
        HashSet<string> possibleSolutions,
        IReadOnlyList<GuessResult> hint,
        HashSet<char> reds,
        char[] greens,
        Dictionary<char, bool[]> yellows,
        bool updatePossibleGuesses = true)
    {
        guesses.Add(guess);
        possibleGuesses.Remove(guess);

        possibleSolutions.Remove(guess);

        UpdateKnownsInplace(guess, hint, ref reds, ref greens, ref yellows);

        possibleSolutions.FilterToPossibleWords(reds, greens, yellows, false);

        // shouldn't need to filter guess list here, but it tends to give a better result
        if (updatePossibleGuesses)
        {
            possibleGuesses.FilterToPossibleWords(reds, greens, yellows, true);
        }
    }

    public static void UpdateKnownsInplace(
        string guess,
        IReadOnlyList<GuessResult> hint,
        ref HashSet<char> reds,
        ref char[] greens,
        ref Dictionary<char, bool[]> yellows)
    {
        for (var i = 0; i < hint.Count; i++)
        {
            if (hint[i] == GuessResult.DoesNotExist)
            {
                reds.Add(guess[i]);
            }

            if (hint[i] == GuessResult.Correct)
            {
                greens[i] = guess[i];
            }

            if (hint[i] == GuessResult.ExistsInDifferentSpot)
            {
                yellows[guess[i]][i] = true;
            }
        }
    }

    private static void FilterToPossibleWords(
        this HashSet<string> wordList,
        ISet<char> reds,
        char[] greens,
        IReadOnlyDictionary<char, bool[]> yellows,
        bool excludeYellows)
    {
        wordList.RemoveWhere(word => !IsWordPossible(word, reds, greens, yellows, excludeYellows));
    }

    public static bool IsWordPossible(
        string word,
        IEnumerable<char> reds,
        char[] greens,
        IReadOnlyDictionary<char, bool[]> yellows,
        bool exludeYellows)
    {
        if (reds.Intersect(word).Any())
        {
            return false;
        }

        for (var index = 0; index < word.Length; index++)
        {
            var letter = word[index];
            if (greens[index] != '-' && word[index] != greens[index])
            {
                return false;
            }

            if (exludeYellows && yellows[letter][index])
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

    private static void PrintProgress(IReadOnlyCollection<bool> progress, string additionalInfo)
    {
        const int binCount = 20;
        var binSize = progress.Count < binCount
            ? progress.Count
            : progress.Count / 20;

        var progressChunks = progress.Chunk(binSize);
        foreach (var chunk in progressChunks)
        {
            var color = chunk.All(x => x) ? ConsoleColor.Green : ConsoleColor.Red;
            Console.ForegroundColor = color;
            Console.Write("█");
        }
        Console.ResetColor();

        Console.WriteLine($" {progress.Count(x => x)} / {progress.Count} : {additionalInfo}");
    }

    public static (GuessCombination combo, double wordsLeft) MinAverageSolutionsAfterThreeGuesses(
        HashSet<string> possibleSolutions,
        List<GuessCombination> combinations,
        string seedWord)
    {
        var stopwatch = Stopwatch.StartNew();

        var progress = Enumerable.Repeat(false, possibleSolutions.Count).ToArray();

        // preallocate the dictionary to reduce key look up later on
        var guessesToWordsLeft = new Dictionary<GuessCombination, int>();
        foreach (var combination in combinations)
        {
            guessesToWordsLeft[combination] = 0;
        }

        for (var index = 0; index < possibleSolutions.Count; index++)
        {
            var word = possibleSolutions.ElementAt(index);
            var localPossibleSolutions = new HashSet<string>(possibleSolutions);
            var results = SolutionsAfterThreeGuesses(combinations, localPossibleSolutions, word, seedWord);

            foreach (var result in results)
            {
                guessesToWordsLeft[result.guesses] += result.wordsLeft;
            }

            progress[index] = true;
            if (index % 20 == 0)
            {
                var timePerSolutionToTry = stopwatch.Elapsed / (index + 1);
                var numRemaining = possibleSolutions.Count - (index + 1);
                var timeRemaining = timePerSolutionToTry * numRemaining;
                var minSoFar = MinAverageGuesses((index + 1), guessesToWordsLeft);
                var minSoFarGueses = JsonSerializer.Serialize(minSoFar.guesses);

                var additionalInfo =
                    $"Min so far: {minSoFarGueses} w/ {minSoFar.averageWordsLeft} avg words left | Time elapsed: {stopwatch.Elapsed} | Est time remaining: {timeRemaining}";

                PrintProgress(progress, additionalInfo);
            }
        }

        return MinAverageGuesses(possibleSolutions.Count, guessesToWordsLeft);
    }

    private static (GuessCombination guesses, double averageWordsLeft) MinAverageGuesses(
        int solutionsToTry,
        Dictionary<GuessCombination,int> guessesToWordsLeft)
    {
        var kvp = guessesToWordsLeft
            .Where(kvp => kvp.Value != 0)
            .ToDictionary(
                kvp => kvp.Key,
                kvp => (double) kvp.Value / solutionsToTry)
            .MinBy(kvp => kvp.Value);

        return (kvp.Key, kvp.Value);
    }

    private static IEnumerable<(GuessCombination guesses, int wordsLeft)> SolutionsAfterThreeGuesses(
        IEnumerable<GuessCombination> possibleGuessesCombinations,
        HashSet<string> possibleSolutions,
        string solution,
        string seedWord)
    {
        // init knowns
        var reds = new HashSet<char>();
        var greens =  Enumerable.Repeat('-', solution.Length).ToArray();
        var yellows = new Dictionary<char, bool[]>();
        for (var c = 'a'; c <= 'z'; c++)
        {
            yellows[c] = Enumerable.Repeat(false, solution.Length).ToArray();
        }

        // every combination has the seedWord so pre-filter the possible solutions;
        GuessAndUpdateState(
            solution,
            guess: seedWord,
            new List<string>(),
            new HashSet<string>(),
            possibleSolutions,
            reds,
            greens,
            yellows,
            updatePossibleGuesses: false);

        return possibleGuessesCombinations
            .AsParallel()
            .Select(possibleGuessesCombination =>
            {
                // copy knowns to local
                var localPossibleSolutions = new HashSet<string>(possibleSolutions);
                var localReds = new HashSet<char>(reds);
                var localGreens = greens.ToArray();
                var localYellows = new Dictionary<char, bool[]>(yellows);

                var nonSeedWordGuesses = possibleGuessesCombination.PossibleGuesses
                    .Where(guess => guess != seedWord);

                foreach (var guess in nonSeedWordGuesses)
                {
                    GuessAndUpdateState(
                        solution,
                        guess,
                        new List<string>(), // not needed
                        new HashSet<string>(), // not needed
                        localPossibleSolutions,
                        localReds,
                        localGreens,
                        localYellows,
                        updatePossibleGuesses: false);
                }

                return (combination: possibleGuessesCombination, count: localPossibleSolutions.Count);
            });
    }

    private static GuessResult[] GuessAndUpdateState(
        string solution,
        string guess,
        ICollection<string> guesses,
        HashSet<string> possibleGuesses,
        HashSet<string> possibleSolutions,
        HashSet<char> reds,
        char[] greens,
        Dictionary<char, bool[]> yellows,
        bool updatePossibleGuesses = true)
    {
        var hint = Guesser.EvaluateGuess(guess, solution);

        UpdateState(
            guess,
            guesses,
            possibleGuesses,
            possibleSolutions,
            hint,
            reds,
            greens,
            yellows,
            updatePossibleGuesses);

        return hint;
    }
}