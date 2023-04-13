Requires:
 - .NET 7

To run:
   - `cd WordleSolver` 
   - `dotnet run --configuration Release`

Currently configured to find the three optimal starting words for [Quordle](https://www.merriam-webster.com/games/quordle/#/).
 - given all possible quordle words find all three word combinations (including the seedword) that have no letters in common.
 - apply the all combinations to the quordle list to find the combination that results in the lowest average remaining words left.
 - In order to speed things up a seed word (predefined one of the three optimal starting words) is required.
   - the default seedword is `roate`
   - to manually specify a seedword, add the argument `<seedword>` to the command line, where `<seedword>` is a comma delimited list of seedwords to use.
     - ex: `dotnet run --configuration Release slate,roate,raise`


Wordle solvers (need to manually update `Main` to use):
- EntropyGuesser is based off of: https://github.com/idofrizler/wordle-hacker/blob/main/EntropyWordleBot.py
- Simple and MinMax guessers are based off of: https://github.com/jstlwy/CSharpWordleSolver