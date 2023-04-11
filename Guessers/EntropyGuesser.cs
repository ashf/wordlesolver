namespace WordleSolver.Guessers;

public class EntropyGuesser : Guesser
{
    public override string Name => nameof(EntropyGuesser);

    public override async Task<string> Guess(
        IReadOnlyList<string> possibleGuesses,
        IList<string> possibleSolutions,
        IReadOnlySet<char> reds,
        IList<char> greens,
        IReadOnlyDictionary<char, List<bool>> yellows)
    {
        if (possibleSolutions.Count > 2)
        {
            var entropies = await CalcInformationGainByEntroyForAllWords(possibleGuesses, possibleSolutions);

            return WordWithMaxEntropy(entropies);
        }

        var randgen = new Random();
        return possibleSolutions[randgen.Next(0, possibleSolutions.Count)];
    }

    private static string WordWithMaxEntropy(IEnumerable<(string word, double entropy)> entropies)
    {
        var maxEntropy = (word: string.Empty, entropy: double.MinValue);
        foreach (var (word, entropy) in entropies)
        {
            if (entropy > maxEntropy.entropy)
            {
                maxEntropy = (word, entropy);
            }
        }

        return maxEntropy.word;
    }

    private static async Task<(string word, double entropy)[]> CalcInformationGainByEntroyForAllWords(
        IEnumerable<string> possibleGuesses,
        ICollection<string> possibleSolutions)
    {
        var tasks = possibleGuesses.Select(possibleGuess =>
            Task.Run(() => (
                word: possibleGuess,
                entropy: CalcInformationGainByEntropy(possibleSolutions, possibleGuess))));

        return await Task.WhenAll(tasks);
    }

    private static double CalcInformationGainByEntropy(ICollection<string> possibleSolutions, string possibleGuess)
    {
        var evaluatedSolutions = possibleSolutions.Select(solution =>
            EvaluateGuess(possibleGuess, solution)
                .Stringify());

        var patternDict = new Dictionary<string, double>();
        foreach (var guessResult in evaluatedSolutions)
        {
            if (!patternDict.ContainsKey(guessResult))
            {
                patternDict[guessResult] = 0;
            }
            patternDict[guessResult] += 1.0 / possibleSolutions.Count;
        }

        return -1 * patternDict.Values.Sum(val => val * Math.Log(val));
    }
}