using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Epsilon.Core.Database;
using Epsilon.Core.Models;

namespace Epsilon.App.ViewModels;

public partial class FlashcardViewModel : ObservableObject
{
    private readonly DatabaseService _db;
    private List<Flashcard> _dueCards = new();
    private int _currentIndex;

    [ObservableProperty]
    private Flashcard? _currentCard;

    [ObservableProperty]
    private bool _isShowingAnswer;

    [ObservableProperty]
    private int _dueCount;

    [ObservableProperty]
    private int _totalCount;

    [ObservableProperty]
    private string _selectedCategory = "All";

    [ObservableProperty]
    private string _newFront = "";

    [ObservableProperty]
    private string _newBack = "";

    [ObservableProperty]
    private string _newCategory = "General";

    [ObservableProperty]
    private string _statusMessage = "";

    public ObservableCollection<string> Categories { get; } = new();

    // Events for WebView2 bridge
    public event Action<string, string>? CardReady;   // front, back
    public event Action? AnswerRevealed;
    public event Action<string>? EmptyStateRequested; // message

    public FlashcardViewModel(DatabaseService db)
    {
        _db = db;
        LoadCategories();
        LoadDueCards();
    }

    private void LoadCategories()
    {
        Categories.Clear();
        Categories.Add("All");
        var all = _db.ListFlashcards();
        var cats = all.Select(c => c.Category).Distinct().OrderBy(c => c).ToList();
        foreach (var cat in cats)
            if (!Categories.Contains(cat))
                Categories.Add(cat);
    }

    private void LoadDueCards()
    {
        var all = _db.ListFlashcards();
        TotalCount = all.Count;

        var due = _db.GetDueFlashcards();

        if (SelectedCategory != "All")
            due = due.Where(c => c.Category == SelectedCategory).ToList();

        _dueCards = due;
        DueCount = _dueCards.Count;
        _currentIndex = 0;

        ShowCurrentCard();
    }

    private void ShowCurrentCard()
    {
        if (_dueCards.Count == 0)
        {
            CurrentCard = null;
            IsShowingAnswer = false;
            EmptyStateRequested?.Invoke(TotalCount == 0
                ? "No flashcards yet. Add some above!"
                : "All caught up! No cards due for review.");
            return;
        }

        if (_currentIndex >= _dueCards.Count)
            _currentIndex = 0;

        CurrentCard = _dueCards[_currentIndex];
        IsShowingAnswer = false;
        CardReady?.Invoke(CurrentCard.Front, CurrentCard.Back);
    }

    partial void OnSelectedCategoryChanged(string value) => LoadDueCards();

    [RelayCommand]
    private void ShowAnswer()
    {
        if (CurrentCard == null) return;
        IsShowingAnswer = true;
        AnswerRevealed?.Invoke();
    }

    [RelayCommand]
    private void RateEasy()  => Rate(5);

    [RelayCommand]
    private void RateGood()  => Rate(3);

    [RelayCommand]
    private void RateHard()  => Rate(1);

    private void Rate(int quality)
    {
        if (CurrentCard == null) return;
        _db.UpdateFlashcardReview(CurrentCard.Id, quality);
        _dueCards.RemoveAt(_currentIndex);
        DueCount = _dueCards.Count;
        StatusMessage = quality >= 3 ? "Good work!" : "Marked for review.";
        ShowCurrentCard();
    }

    [RelayCommand]
    private void AddCard()
    {
        if (string.IsNullOrWhiteSpace(NewFront) || string.IsNullOrWhiteSpace(NewBack))
        {
            StatusMessage = "Front and back are required.";
            return;
        }

        var card = new Flashcard
        {
            Front = NewFront.Trim(),
            Back = NewBack.Trim(),
            Category = string.IsNullOrWhiteSpace(NewCategory) ? "General" : NewCategory.Trim(),
            NextReview = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
        };

        _db.InsertFlashcard(card);
        NewFront = "";
        NewBack = "";
        StatusMessage = "Card added!";

        LoadCategories();
        LoadDueCards();
    }

    [RelayCommand]
    private void DeleteCard()
    {
        if (CurrentCard == null) return;
        _db.DeleteFlashcard(CurrentCard.Id);
        _dueCards.RemoveAt(_currentIndex);
        DueCount = _dueCards.Count;
        TotalCount--;
        StatusMessage = "Card deleted.";
        ShowCurrentCard();
    }

    [RelayCommand]
    private void Refresh() => LoadDueCards();
}
