using System;
using System.Collections.Generic;
using System.Linq;

namespace Local_Sums.Models;

public class Puzzle
{
    private static int _puzzleLen = 5;
    private List<int> _hint = new List<int>();
    public List<int> Hint => _hint;
    private List<List<int>> _possibleSolutions = new List<List<int>>();
    public List<List<int>> PossibleSolutions => _possibleSolutions;
    private List<List<int>> _guesses = new List<List<int>>();
    public List<List<int>> Guesses => _guesses;

    public Puzzle()
    {
        List<int> initialSolution = GenerateRandomSolution();
        _hint = CalculateHint(initialSolution);
        _possibleSolutions = CalculatePossibleSolutions(_hint);
    }

    public Puzzle(List<int> hint)
    {
        _hint = new List<int>(hint);
        _possibleSolutions = CalculatePossibleSolutions(_hint);
    }

    private List<int> GenerateRandomSolution()
    {
        List<int> solution = new List<int>();
        Random rand = new Random();
        for (int i = 0; i < _puzzleLen; i++)
        {
            solution.Add(rand.Next(1, 4)); // Generate random numbers between 1 and 3
        }
        return solution;
    }

    private List<int> CalculateHint(List<int> solution)
    {
        List<int> outHint = new List<int>();
        for (int i = 0; i < _puzzleLen; i++)
        {
            int hintNumeral = 0;
            if (i - 1 >= 0) hintNumeral += solution[i - 1];
            hintNumeral += solution[i];
            if (i + 1 < _puzzleLen) hintNumeral += solution[i + 1];

            outHint.Add(hintNumeral);
        }
        return outHint;
    }

    private List<List<int>> CalculatePossibleSolutions(List<int> hint)
    {
        List<List<int>> possibleSolutions = new List<List<int>>();

        for (int firstNumeral = 1; firstNumeral <= 3; firstNumeral++)
        {
            List<int> solution = new List<int>
            {
                firstNumeral
            };

            solution.Add(hint[0] - firstNumeral);

            if (solution[1] < 1 || solution[1] > 3)
            {
                continue;
            }

            bool validSolution = true;

            for (int i = 1; i < _puzzleLen - 1; i++)
            {
                int nextNumeral = hint[i] - solution[i - 1] - solution[i];

                if (nextNumeral < 1 || nextNumeral > 3)
                {
                    validSolution = false;
                    break;
                }

                solution.Add(nextNumeral);
            }

            if (!validSolution)
            {
                continue;
            }

            int finalHint = solution[^2] + solution[^1];

            if (finalHint == hint[^1])
            {
                possibleSolutions.Add(solution);
            }
        }

        return possibleSolutions;
    }

    public string GetHintAsString()
    {
        return string.Join(", ", _hint);
    }

    public string getPossibleSolutionsAsString()
    {
        List<string> solutionStrings = new List<string>();
        foreach (var solution in _possibleSolutions)
        {
            solutionStrings.Add(string.Join(", ", solution));
        }
        return string.Join(" \n ", solutionStrings);
    }

    public string getGuessesAsString()
    {
        List<string> guessStrings = new List<string>();
        foreach (var guess in _guesses)
        {
            guessStrings.Add(string.Join(", ", guess));
        }
        return string.Join(" \n ", guessStrings);
    }
}