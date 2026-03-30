namespace Epsilon.Web.Contracts;

public record CreateFlashcardRequest(string Front, string Back, string Category = "General");
public record ReviewFlashcardRequest(int Quality);

public record FlashcardDto(
    string Id, string Front, string Back, string Category,
    double EaseFactor, int IntervalDays, int Repetitions,
    DateTime NextReview, DateTime CreatedAt);
