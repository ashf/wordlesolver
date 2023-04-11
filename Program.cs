using System.Text.Json;
using System.Text.Json.Serialization;
using MoreLinq;
using WordleSolver.Guessers;

namespace WordleSolver;

internal static class Program
{
	private const string PossibleGuessesFileName = "wordlewords.txt";
	private const string PossibleSolutionsFileName = "wordlewords_edited.txt";
	// private const string PossibleSolutionsFileName = "wordlewords.txt";
	private const int WordLength = 5;

	private static async Task Main()
	{
		var possibleGuesses = LoadWordsFromFile(PossibleGuessesFileName, WordLength);
		var possibleSolutions = LoadWordsFromFile(PossibleSolutionsFileName, WordLength);

		// await MultipleGames(possibleGuesses, possibleSolutions);
		await OptimalStartingWords(possibleGuesses, possibleSolutions);
	}

	private static List<string> LoadWordsFromFile(
		string filename,
		int wordLength)
	{
		if (!File.Exists(filename))
		{
			Console.Error.WriteLineAsync("Fatal error: Word list file not found");
			Environment.Exit(-1);
		}

		var words = File.ReadAllLines(filename)
			.Select(line => line.ToLower())
			.Where(word => word.Length == wordLength)
			.Where(word => word.All(char.IsLetter))
			.ToList();

		if (words.Any())
		{
			return words;
		}

		Console.Error.WriteLineAsync("Fatal error: No words were loaded from the file.");
		Environment.Exit(-1);

		// need a return statement here because the compiler doesn't know that the program will exit
		return words;
	}

	private static async Task SingleGame(List<string> possibleGuesses, List<string> possibleSolutions)
	{
		var entropyGuesser = new EntropyGuesser();

		var randgen = new Random();
		var answer = possibleSolutions[randgen.Next(0, possibleSolutions.Count)];

		await Solver.Play(entropyGuesser, possibleGuesses, possibleSolutions, answer);
	}

	private static async Task MultipleGames(List<string> possibleGuesses, List<string> possibleSolutions)
	{
		var entropyGuesser = new EntropyGuesser();

		var answerToNumGuesses = new Dictionary<string, int>();

		var allGuessResults = new List<IEnumerable<string>>();

		foreach (var answer in possibleSolutions.TakeEvery(25))
		{
			var guesses = await Solver.Play(entropyGuesser, possibleGuesses, possibleSolutions, answer);
			answerToNumGuesses.Add(answer, guesses.Count);
			allGuessResults.AddResultsToGuessResults(guesses);
		}

		Console.WriteLine($"average number of guesses: {answerToNumGuesses.Values.Average()}");

		Console.WriteLine($"guess #1: {allGuessResults[0].MostFrequent()}");
		Console.WriteLine($"guess #2: {allGuessResults[1].MostFrequent()}");
		Console.WriteLine($"guess #3: {allGuessResults[2].MostFrequent()}");
		Console.WriteLine($"guess #4: {allGuessResults[3].MostFrequent()}");
		Console.WriteLine($"guess #5: {allGuessResults[4].MostFrequent()}");
	}

	private static async Task OptimalStartingWords(IEnumerable<string> possibleGuesses, List<string> possibleSolutions)
	{
		var allGuessResults = new Dictionary<IEnumerable<string>, (int occurances, int wordsLeft)>();

		var subsets = possibleGuesses.GetDifferentCombinations(3).ToList();

		await File.WriteAllTextAsync("c:/temp/subsets.txt",JsonSerializer.Serialize(subsets));

		foreach (var word in possibleSolutions)
		{
			var (guesses, wordsLeft) = await Solver.SolutionsAfterThreeGuesses(subsets, possibleSolutions, word);

			if (!allGuessResults.ContainsKey(guesses))
			{
				allGuessResults[guesses] = (1, wordsLeft);
			}
			else
			{
				var temp = allGuessResults[guesses];
				temp.occurances++;
				temp.wordsLeft += wordsLeft;
				allGuessResults[guesses] = temp;
			}
		}

		var averageGuesses = allGuessResults
			.ToDictionary(
				kvp => kvp.Key.ToArray(),
				kvp => (double) kvp.Value.wordsLeft / kvp.Value.occurances);

		var minGuesses = Enumerable.MinBy(averageGuesses, kvp => kvp.Value);

		Console.WriteLine($"average number of words left: {minGuesses.Value}");
		Console.WriteLine($"guess #1: {string.Join(", ", minGuesses.Key[0])}");
		Console.WriteLine($"guess #2: {string.Join(", ", minGuesses.Key[1])}");
		Console.WriteLine($"guess #3: {string.Join(", ", minGuesses.Key[2])}");
	}

	private static void AddResultsToGuessResults(
		this IList<IEnumerable<string>> allGuessResults,
		IReadOnlyList<string> guesses)
	{
		for (var index = 0; index < guesses.Count; index++)
		{
			var guess = guesses[index];
			if (allGuessResults.Count <= index)
			{
				allGuessResults.Add(new List<string>());
			}

			var foo = allGuessResults[index];
			allGuessResults[index] = Enumerable.Append(foo, guess);
		}
	}

	private static T MostFrequent<T>(this IEnumerable<T> source)
	{
		return source.GroupBy(i => i)
			.OrderByDescending(g => g.Count())
			.Select(g => g.Key)
			.First();
	}
}