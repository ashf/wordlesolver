using MoreLinq;

namespace WordleSolver.Guessers;

public class MinMaxGuesser : Guesser
{
    public override string Name => nameof(MinMaxGuesser);

    public override async Task<string> Guess(
        IReadOnlyList<string> possibleGuesses,
        IList<string> possibleSolutions,
        IReadOnlySet<char> reds,
        char[] greens,
        IReadOnlyDictionary<char, bool[]> yellows)
    {
        var semaphore = new Semaphore(0, 1);
        semaphore.Release();

        var progressCount = Enumerable.Repeat(false, possibleGuesses.Count).ToArray();
        var bestGuesses = Enumerable.Repeat(new GuessRanking(), possibleGuesses.Count).ToArray();

        var tasks = possibleGuesses
            .Select((guess, i) => Task.Run(() =>
            {
                var ranking = new GuessRanking
                {
                    MaxScore = 0,
                    AverageScore = 0,
                    BestScore = int.MaxValue,
                    Index = i
                };

                foreach (var actual in possibleSolutions)
                {
                    if (guess != actual)
                    {
                        var hint = EvaluateGuess(guess, actual);

                        var (newReds, newGreens, newYellows) = UpdateKnowns(hint, guess, reds, greens, yellows);
                        var score = FilteredWordListSize(newReds, newGreens, newYellows, possibleSolutions);
                        if (score == 0)
                        {
                            score = possibleSolutions.Count;
                        }

                        ranking.AverageScore += score;
                        ranking.MaxScore = Math.Max(score, ranking.MaxScore);
                        ranking.BestScore = Math.Min(score, ranking.BestScore);
                    }
                    else
                    {
                        ranking.BestScore = 0;
                    }
                }

                bestGuesses[i] = ranking;

                progressCount[i] = true;
                if (i % 40 == 0)
                {
                    // semaphore.WaitOne();
                    // PrintProgress(progressCount);
                    // semaphore.Release();
                }
            }));

        await Task.WhenAll(tasks);

        //select best word
        var bestGuess = bestGuesses[0];
        for (var i = 1; i < bestGuesses.Length; i++)
        {
            if (bestGuesses[i].IsLessThan(bestGuess))
            {
                bestGuess = bestGuesses[i];
            }
        }

        return possibleGuesses[bestGuess.Index];
    }

    private static int FilteredWordListSize(
        ISet<char> reds,
        IList<char> greens,
        IReadOnlyDictionary<char, bool[]> yellows,
        IEnumerable<string> wordList)
    {
        return wordList.Count(word => Solver.IsWordPossible(word.ToCharArray(), reds, greens, yellows, true));
    }

    private static (HashSet<char> reds, IList<char> greens, IReadOnlyDictionary<char, bool[]> yellows) UpdateKnowns(
        IReadOnlyList<GuessResult> hint,
        string guess,
        IEnumerable<char> reds,
        char[] greens,
        IReadOnlyDictionary<char, bool[]> yellows)
    {
        var newReds = new HashSet<char>(reds);
        var newGreens = greens.Clone() as char[];
        var newYellows = yellows.ToDictionary(x =>
            x.Key,
            x => x.Value.Clone() as bool[])
            .AsReadOnly();

        Solver.UpdateKnownsInplace(guess, hint, newReds, newGreens!, newYellows!);

        return (newReds, newGreens!, newYellows!);
    }

    private static void PrintProgress(IReadOnlyCollection<bool> progress)
    {
        var binSize = progress.Count / 20;

        progress.Batch(binSize).ForEach(bin =>
        {
            var color = bin.All(x => x) ? ConsoleColor.Green : ConsoleColor.Red;
            Console.ForegroundColor = color;
            Console.Write("█");
        });
        Console.ResetColor();

        Console.WriteLine($"{progress.Count(x => x)} / {progress.Count}");
        Console.WriteLine();
    }
}