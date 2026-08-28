using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Local_Sums.Models;
using System.Collections.Generic;
using System.Linq;

namespace Local_Sums.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;

    public string Message { get; set; } = "";

    // Bound fields for the form
    [BindProperty]
    public string GuessInput { get; set; } = "";

    // Stores serialized correct guesses across posts (example format: "1,2,3,1,2;2,1,3,1,2")
    [BindProperty]
    public string CorrectGuessesSerialized { get; set; } = "";

    // Stores serialized hint so the same puzzle is preserved across posts (example: "3,4,5,4,3")
    [BindProperty]
    public string HintSerialized { get; set; } = "";

    // For display
    public List<string> CorrectGuessesDisplay { get; set; } = new List<string>();

    public bool YouWin { get; set; } = false;

    public string InputError { get; set; } = "";

    public IndexModel(ILogger<IndexModel> logger)
    {
        _logger = logger;
    }

    //Support optional query parameters to redirect back to a stable GET with persisted state.
    public void OnGet(string? hintSerialized = null, string? correctGuessesSerialized = null)
    {
        InputError = "";

        if (string.IsNullOrWhiteSpace(hintSerialized))
        {
            // Start a fresh puzzle
            Puzzle puzzle = new Puzzle();
            Message = puzzle.GetHintAsString();
            HintSerialized = string.Join(",", puzzle.Hint);
            CorrectGuessesSerialized = "";
            CorrectGuessesDisplay = new List<string>();
            YouWin = false;
            return;
        }

        //Use supplied serialized values to rebuild page state
        HintSerialized = hintSerialized;
        CorrectGuessesSerialized = correctGuessesSerialized ?? "";

        // Reconstruct puzzle and displays from serialized data
        List<int> hint;
        try
        {
            hint = HintSerialized.Split(',', System.StringSplitOptions.RemoveEmptyEntries)
                .Select(s => int.Parse(s.Trim())).ToList();
        }
        catch
        {
            // Fallback to a fresh puzzle if something is wrong
            Puzzle puzzle = new Puzzle();
            Message = puzzle.GetHintAsString();
            HintSerialized = string.Join(",", puzzle.Hint);
            CorrectGuessesSerialized = "";
            CorrectGuessesDisplay = new List<string>();
            YouWin = false;
            return;
        }

        Puzzle reconstructed = new Puzzle(hint);
        Message = reconstructed.GetHintAsString();

        // parse correct guesses for display and win check
        var parsedGuesses = new List<List<int>>();
        if (!string.IsNullOrWhiteSpace(CorrectGuessesSerialized))
        {
            var parts = CorrectGuessesSerialized.Split(';', System.StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                var nums = part.Split(',', System.StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => int.TryParse(s.Trim(), out var v) ? v : -1).ToList();
                if (nums.Count == 5 && nums.All(n => n >= 1 && n <= 3))
                {
                    parsedGuesses.Add(nums);
                }
            }
        }

        CorrectGuessesDisplay = parsedGuesses.Select(g => string.Join(", ", g)).ToList();

        //normalize keys and check win
        static string ToKey(IEnumerable<int> nums) => string.Join(",", nums);
        var possibleKeys = new HashSet<string>(reconstructed.PossibleSolutions.Select(ToKey));
        var guessedKeys = new HashSet<string>(parsedGuesses.Select(ToKey));
        YouWin = possibleKeys.All(k => guessedKeys.Contains(k));
    }

    public IActionResult OnPost()
    {
        InputError = "";

        // Parse hint from bound hidden field
        List<int> hint;
        try
        {
            hint = HintSerialized?.Split(',', System.StringSplitOptions.RemoveEmptyEntries)
                .Select(s => int.Parse(s.Trim())).ToList() ?? new List<int>();
            if (hint.Count != 5)
            {
                InputError = "Invalid puzzle hint. Start a new game.";
                return Page();
            }
        }
        catch
        {
            InputError = "Invalid puzzle hint. Start a new game.";
            return Page();
        }

        // Build puzzle from the persisted hint
        Puzzle puzzle = new Puzzle(hint);
        Message = puzzle.GetHintAsString();

        //Parse existing correct guesses from bound hidden field
        List<List<int>> correctGuesses = new List<List<int>>();
        if (!string.IsNullOrWhiteSpace(CorrectGuessesSerialized))
        {
            var parts = CorrectGuessesSerialized.Split(';', System.StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                var nums = part.Split(',', System.StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => int.TryParse(s.Trim(), out var v) ? v : -1).ToList();
                if (nums.Count == 5 && nums.All(n => n >= 1 && n <= 3))
                {
                    correctGuesses.Add(nums);
                }
            }
        }

        // Helper to normalize a solution/guess to a key string
        static string ToKey(IEnumerable<int> nums) => string.Join(",", nums);

        // Build set of possible-solution keys
        var possibleKeys = new HashSet<string>(puzzle.PossibleSolutions.Select(ToKey));

        // Parse and validate player input: accept digits anywhere but must result in exactly 5 numbers 1-3
        var digits = GuessInput?.Where(char.IsDigit).Select(c => c - '0').ToList() ?? new List<int>();
        if (digits.Count != 5 || digits.Any(d => d < 1 || d > 3))
        {
            InputError = "Guess must contain exactly 5 numbers between 1 and 3 (for example: 12312 or 1,2,3,1,2).";
            CorrectGuessesDisplay = correctGuesses.Select(g => string.Join(", ", g)).ToList();

            // compute guessed keys set and win status for the rendered page
            var guessedKeys = new HashSet<string>(correctGuesses.Select(ToKey));
            YouWin = possibleKeys.All(k => guessedKeys.Contains(k));

            // keep serialized data as-is and show page with error
            CorrectGuessesSerialized = string.Join(";", correctGuesses.Select(g => string.Join(",", g)));
            return Page();
        }

        // Check if guess matches one of the possible solutions using the normalized key
        var guessKey = ToKey(digits);
        bool isCorrect = possibleKeys.Contains(guessKey);

        if (isCorrect)
        {
            // Add if not already present
            bool already = correctGuesses.Any(g => ToKey(g) == guessKey);
            if (!already)
            {
                correctGuesses.Add(digits);
            }
        }
        else
        {
            // incorrect guess -> display feedback
            InputError = "That guess is not one of the possible solutions.";
            CorrectGuessesDisplay = correctGuesses.Select(g => string.Join(", ", g)).ToList();
            CorrectGuessesSerialized = string.Join(";", correctGuesses.Select(g => string.Join(",", g)));
            YouWin = possibleKeys.All(k => correctGuesses.Select(ToKey).Contains(k));
            return Page();
        }

        // Update serialized storage
        CorrectGuessesSerialized = string.Join(";", correctGuesses.Select(g => string.Join(",", g)));
        CorrectGuessesDisplay = correctGuesses.Select(g => string.Join(", ", g)).ToList();

        // Win condition: require that every possible solution key is present among the recorded correct guess keys
        var guessedKeysFinal = new HashSet<string>(correctGuesses.Select(ToKey));
        var won = possibleKeys.All(k => guessedKeysFinal.Contains(k));

        if (won)
        {
            // Redirect to GET with persisted state so the page will render the You Win! state reliably.
            return RedirectToPage(new { hintSerialized = HintSerialized, correctGuessesSerialized = CorrectGuessesSerialized });
        }

        // Not yet won, also redirect to GET so the hidden fields are always consistent with displayed state (prevents loss).
        return RedirectToPage(new { hintSerialized = HintSerialized, correctGuessesSerialized = CorrectGuessesSerialized });
    }

    //Handler for the New Game button, use RedirectToPage to implement Post-Redirect-Get and avoid stale POST state
    public IActionResult OnPostNewGame()
    {
        // redirect to a fresh GET of this page so the page model and form state are reset.
        return RedirectToPage();
    }
}