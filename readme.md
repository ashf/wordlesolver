Requires:
 - .NET 7

to run:
   - `cd WordleSolver` 
   - `dotnet run --configuration Release`

Currently configured to find the three optimal starting words for Quordle.

In order to speed things up a seed word (predefined one of the three optimal starting words) is required.
 - the default seedword is `roate`
 - to manually specify a seedword, add the argument `<seedword>` to the command line, where `<seedword>` is a comma delimited list of seedwords to use.
   - ex: `dotnet run --configuration Release slate,roate,raise`


Wordle solvers:
- EntropyGuesser is based off of: https://github.com/idofrizler/wordle-hacker/blob/main/EntropyWordleBot.py
- Simple and MinMax guessers are based off of: https://github.com/jstlwy/CSharpWordleSolver