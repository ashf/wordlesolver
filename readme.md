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


Optimal Quordle starting words (as of 2023-04-13):
```
2315 possible solutions

seedword: slate
2852 combinations that have no shared letters
average number of words left for optimal guesses: 8.453995680345573
guess #1: mudir
guess #2: poncy
guess #3: slate

seedword: salet
2852 combinations that have no shared letters
average number of words left for optimal guesses: 8.61900647948164
guess #1: crudy
guess #2: pingo
guess #3: salet

seedword: stale
2852 combinations that have no shared letters
average number of words left for optimal guesses: 8.900647948164147
guess #1: mudir
guess #2: poncy
guess #3: stale

seedword: roate
2872 combinations that have no shared letters
average number of words left for optimal guesses: 8.952051835853132
guess #1: chynd
guess #2: simul
guess #3: roate

seedword: raise
573 combinations that have no shared letters
average number of words left for optimal guesses: 9.27170626349892
guess #1: chynd
guess #2: poult
guess #3: raise

seedword: trace
10299 combinations that have no shared letters
average number of words left for optimal guesses: 9.54816414686825
guess #1: mould
guess #2: snipy
guess #3: trace

seedword: adieu
202 combinations that have no shared letters
average number of words left for optimal guesses: 10.046652267818574
guess #1: crwth
guess #2: sonly
guess #3: adieu
```