using System.Collections.Concurrent;
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
            var guessResults = GuessAndUpdateState(
                solution,
                guess,
                new List<string>(), // not needed
                possibleGuesses,
                possibleSolutions,
                reds,
                greens,
                yellows,
                updatePossibleGuesses: false);

            guess.PrintResult(guessResults, possibleSolutions.Count);

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

    // Updates the state based on the guess and guessResults
    // The state includes the knowns (reds, greens, yellows) and the possible solutions (and guesses)
    private static void UpdateState(
        string guess,
        ICollection<string> guesses,
        HashSet<string> possibleGuesses,
        HashSet<string> possibleSolutions,
        IReadOnlyList<GuessResult> guessResults,
        HashSet<char> reds,
        char[] greens,
        Dictionary<char, bool[]> yellows,
        bool updatePossibleGuesses = true)
    {
        guesses.Add(guess);
        possibleGuesses.Remove(guess);

        possibleSolutions.Remove(guess);

        UpdateKnownsInplace(guess, guessResults, ref reds, ref greens, ref yellows);

        possibleSolutions.FilterToPossibleWords(reds, greens, yellows, false);

        // shouldn't need to filter guess list here, but it tends to give a better result
        if (updatePossibleGuesses)
        {
            possibleGuesses.FilterToPossibleWords(reds, greens, yellows, true);
        }
    }

    // Updates the knowns (reds, greens, yellows) based on the guess and guessResults
    public static void UpdateKnownsInplace(
        string guess,
        IReadOnlyList<GuessResult> guessResults,
        ref HashSet<char> reds,
        ref char[] greens,
        ref Dictionary<char, bool[]> yellows)
    {
        for (var i = 0; i < guessResults.Count; i++)
        {
            if (guessResults[i] == GuessResult.DoesNotExist)
            {
                reds.Add(guess[i]);
            }

            if (guessResults[i] == GuessResult.Correct)
            {
                greens[i] = guess[i];
            }

            if (guessResults[i] == GuessResult.ExistsInDifferentSpot)
            {
                yellows[guess[i]][i] = true;
            }
        }
    }

    // Removes words from the list that are not possible given the knowns (reds, greens, yellows)
    private static void FilterToPossibleWords(
        this HashSet<string> wordList,
        ISet<char> reds,
        char[] greens,
        IReadOnlyDictionary<char, bool[]> yellows,
        bool excludeYellows)
    {
        wordList.RemoveWhere(word => !IsWordPossible(word, reds, greens, yellows, excludeYellows));
    }

    // Determines if a word is possible given the knowns (reds, greens, yellows)
    public static bool IsWordPossible(
        string word,
        IEnumerable<char> reds,
        char[] greens,
        IReadOnlyDictionary<char, bool[]> yellows,
        bool exludeYellows)
    {
        // if any of the letters are in the red list, it's not possible
        if (reds.Intersect(word).Any())
        {
            return false;
        }

        // loop through each letter in the word
        for (var index = 0; index < word.Length; index++)
        {
            var letter = word[index];

            // if the letter is in the green list, the letter must be in the same spot
            if (greens[index] != '-' && word[index] != greens[index])
            {
                return false;
            }

            // if the letter is in the yellow list, the letter must not be in the same spot
            if (exludeYellows && yellows[letter][index])
            {
                return false;
            }
        }

        return true;
    }

    private static void PrintResult(this string guess, GuessResult[] guessResults, int wordsLeft)
    {
        for (var index = 0; index < guess.Length; index++)
        {
            var color = guessResults[index] switch
            {
                GuessResult.Correct => ConsoleColor.Green,
                GuessResult.DoesNotExist => ConsoleColor.Red,
                GuessResult.ExistsInDifferentSpot => ConsoleColor.Yellow,
                _ => throw new ArgumentOutOfRangeException(nameof(guessResults), guessResults, null)
            };

            Console.ForegroundColor = color;
            Console.Write(guess[index]);
        }

        Console.ResetColor();

        Console.WriteLine($" : {wordsLeft} words left");
    }

    private static void PrintProgress(IReadOnlyCollection<bool> progress, string additionalInfo)
    {
        const int binCount = 30;
        var binSize = progress.Count < binCount
            ? progress.Count
            : progress.Count / binCount;

        var progressChunks = progress.Chunk(binSize);
        foreach (var chunk in progressChunks)
        {
            var color = ConsoleColor.Red;
            var count = chunk.Count(x => x);
            if (count == chunk.Length)
            {
                color = ConsoleColor.Green;
            }

            else if (count != 0 && count < chunk.Length / 2)
            {
                color = ConsoleColor.DarkYellow;
            }
            else if (count > chunk.Length / 2)
            {
                color = ConsoleColor.Yellow;
            }

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
        var finished = 0;

        // preallocate the dictionary to reduce key look up later on
        var guessesToWordsLeft = new ConcurrentDictionary<GuessCombination, int>();
        foreach (var combination in combinations)
        {
            guessesToWordsLeft[combination] = 0;
        }

        // loop through each possible solution
        // for each solution determine words left for each guess combination (and add it to the total)
        Parallel.For((long)0, possibleSolutions.Count, index =>
        {
            var word = possibleSolutions.ElementAt((int)index);
            var localPossibleSolutions = new HashSet<string>(possibleSolutions);
            var results = SolutionsAfterThreeGuesses(combinations, localPossibleSolutions, word, seedWord);

            Parallel.ForEach(results, result =>
            {
                guessesToWordsLeft[result.guesses] += result.wordsLeft;
            });

            progress[index] = true;
            Interlocked.Increment(ref finished);
            if (finished % 50 == 0)
            {
                var timePerSolutionToTry = stopwatch.Elapsed / finished;
                var numRemaining = possibleSolutions.Count - finished;
                var timeRemaining = timePerSolutionToTry * numRemaining;
                var minSoFar = MinAverageGuesses(finished, guessesToWordsLeft);
                var minSoFarGueses = JsonSerializer.Serialize(minSoFar.guesses);

                var additionalInfo =
                    $"Min so far: {minSoFarGueses} w/ {minSoFar.averageWordsLeft:f2} avg words left | Time elapsed: {stopwatch.Elapsed} | Est time remaining: {timeRemaining}";

                PrintProgress(progress, additionalInfo);
            }
        });

        // based on toal number of solutions tried, determine the average number of words left for each guess combination
        return MinAverageGuesses(possibleSolutions.Count, guessesToWordsLeft);
    }

    // Returns the guess combination that has the lowest average number of words left (based on the number of solutions tried)
    private static (GuessCombination guesses, double averageWordsLeft) MinAverageGuesses(
        long solutionsToTry,
        ConcurrentDictionary<GuessCombination,int> guessesToWordsLeft)
    {
        var kvp = guessesToWordsLeft
            .Where(kvp => kvp.Value != 0)
            .ToDictionary(
                kvp => kvp.Key,
                kvp => (double) kvp.Value / solutionsToTry)
            .MinBy(kvp => kvp.Value);

        return (kvp.Key, kvp.Value);
    }

    // Given a solution and the possible solutions available,
    // determine how many solutions would be left for each supplied guess combination
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

        // parallel process each guess combination
        return possibleGuessesCombinations
            .Select(possibleGuessesCombination =>
            {
                // copy knowns to local
                var localPossibleSolutions = new HashSet<string>(possibleSolutions);
                var localReds = new HashSet<char>(reds);
                var localGreens = greens.ToArray();
                var localYellows = new Dictionary<char, bool[]>(yellows);

                // we've already processed the seedWord (since every combo has it) so skip it
                var nonSeedWordGuesses = possibleGuessesCombination.PossibleGuesses
                    .Where(guess => guess != seedWord);

                // process the guess and update the sate for each non seedword guess
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
        // evaluate the guess
        var guessResults = Guesser.EvaluateGuess(guess, solution);

        UpdateState(
            guess,
            guesses,
            possibleGuesses,
            possibleSolutions,
            guessResults,
            reds,
            greens,
            yellows,
            updatePossibleGuesses);

        return guessResults;
    }
}