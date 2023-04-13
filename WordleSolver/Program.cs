using System.Diagnostics;
using System.Text.Json;
using WordleSolver.Guessers;

namespace WordleSolver;

internal static class Program
{
	// private const string PossibleGuessesFileName = "wordlewords_edited.txt";
	private const string PossibleGuessesFileName = "quordlewords.txt";
	// private const string PossibleGuessesFileName = "wordlewords.txt";
	// private const string PossibleSolutionsFileName = "wordlewords_edited.txt";
	private const string PossibleSolutionsFileName = "quordlewords.txt";
	// private const string PossibleSolutionsFileName = "wordlewords.txt";

	private const int WordLength = 5;

	private static async Task Main(string[] args)
	{
		var possibleGuesses = LoadWordsFromFile(PossibleGuessesFileName, WordLength);
		var possibleSolutions = LoadWordsFromFile(PossibleSolutionsFileName, WordLength);

		// await MultipleGames(possibleGuesses, possibleSolutions);

		if (args is {Length: > 0})
		{
			var seedWords = args[0].Split(',');
			Console.WriteLine($"seedwords: {args[0]}");
			foreach (var seedWord in seedWords)
			{
				await OptimalStartingWords(seedWord, possibleGuesses, possibleSolutions);
			}
		}
		else
		{
			const string seedword = "roate";
			Console.WriteLine($"seedword: {seedword}");
			await OptimalStartingWords(seedword, possibleGuesses, possibleSolutions);
		}

		await OptimalStartingWords("roate", possibleGuesses, possibleSolutions);
		await OptimalStartingWords("salet", possibleGuesses, possibleSolutions);
	}

	private static HashSet<string> LoadWordsFromFile(
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
			.Where(word => word.All(char.IsLetter)).ToHashSet();

		if (words.Any())
		{
			return words;
		}

		Console.Error.WriteLineAsync("Fatal error: No words were loaded from the file.");
		Environment.Exit(-1);

		// need a return statement here because the compiler doesn't know that the program will exit
		return words;
	}

	private static async Task SingleGame(HashSet<string> possibleGuesses, HashSet<string> possibleSolutions)
	{
		var entropyGuesser = new EntropyGuesser();

		var randgen = new Random();
		var answer = possibleSolutions.ElementAt(randgen.Next(0, possibleSolutions.Count));

		await Solver.Play(entropyGuesser, possibleGuesses, possibleSolutions, answer);
	}

	private static async Task MultipleGames(HashSet<string> possibleGuesses, HashSet<string> possibleSolutions)
	{
		var entropyGuesser = new EntropyGuesser();

		var answerToNumGuesses = new Dictionary<string, int>();

		var allGuessResults = new List<IEnumerable<string>>();

		foreach (var answer in possibleSolutions)
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

	private static async Task OptimalStartingWords(string seedWord, HashSet<string> possibleGuesses, HashSet<string> possibleSolutions)
	{
		var combinationsFile = $"combinations/combinations_{seedWord}.txt";

		List<HashSet<string>>? combinationsAsSet;

		if (File.Exists(combinationsFile))
		{
			combinationsAsSet = JsonSerializer.Deserialize<List<HashSet<string>>>(await File.ReadAllTextAsync(combinationsFile));
		}
		else
		{
			Console.WriteLine($"{possibleGuesses.Count} possible guesses loaded");

			possibleGuesses = possibleGuesses.Where(word =>
			{
				return word.All(letter => word.Count(x => x == letter) <= 1) && word != seedWord;
			}).ToHashSet();

			Console.WriteLine($"{possibleGuesses.Count} possible guesses filtered (no duplicate letters)");

			combinationsAsSet = possibleGuesses.GetDifferentCombinations(2, seedWord).ToList();

			await File.WriteAllTextAsync(combinationsFile, JsonSerializer.Serialize(combinationsAsSet));
		}

		var combination = combinationsAsSet!.Select(x => new GuessCombination(x)).ToList();

		Console.WriteLine($"{combination.Count} combinations that have no shared letters");

		var solutionsToTry = possibleSolutions.Count;
		if (solutionsToTry != possibleSolutions.Count)
		{
			possibleSolutions = possibleSolutions.Take(solutionsToTry).ToHashSet();
		}

		var stopwatch = Stopwatch.StartNew();

		var foo = Solver.MinAverageSolutionsAfterThreeGuesses(possibleSolutions, combination, seedWord);

		var elapsed = stopwatch.Elapsed;

		Console.WriteLine($"average number of words left for optimal guesses: {foo.wordsLeft}");
		Console.WriteLine($"guess #1: {string.Join(", ", foo.combo.ElementAt(0))}");
		Console.WriteLine($"guess #2: {string.Join(", ", foo.combo.ElementAt(1))}");
		Console.WriteLine($"guess #3: {string.Join(", ", foo.combo.ElementAt(2))}");
		Console.WriteLine($"Total time {elapsed}, time/possibleSolution = {elapsed / solutionsToTry}");
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
			allGuessResults[index] = foo.Append(guess);
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