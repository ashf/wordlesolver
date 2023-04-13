namespace WordleSolver.Guessers;

public class MinMaxGuesser : Guesser
{
    public override string Name => nameof(MinMaxGuesser);

    public override async Task<string> Guess(
        HashSet<string> possibleGuesses,
        HashSet<string> possibleSolutions,
        IReadOnlySet<char> reds,
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

                        var (newReds, newYellows) = UpdateKnowns(hint, guess, reds, yellows);
                        var score = FilteredWordListSize(newReds, newYellows, possibleSolutions);
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

        return possibleGuesses.ElementAt(bestGuess.Index);
    }

    private static int FilteredWordListSize(
        ISet<char> reds,
        IReadOnlyDictionary<char, bool[]> yellows,
        IEnumerable<string> wordList)
    {
        return wordList.Count(word => Solver.IsWordPossible(word, reds, yellows, true));
    }

    private static (HashSet<char> reds, IReadOnlyDictionary<char, bool[]> yellows) UpdateKnowns(
        IReadOnlyList<GuessResult> hint,
        string guess,
        IEnumerable<char> reds,
        IReadOnlyDictionary<char, bool[]> yellows)
    {
        var newReds = new HashSet<char>(reds);
        var newYellows = yellows.ToDictionary(x =>
            x.Key,
            x => x.Value.Clone() as bool[]);

        Solver.UpdateKnownsInplace(guess, hint, ref newReds, ref newYellows!);

        return (newReds, newYellows!);
    }
}