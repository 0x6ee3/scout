namespace Scout.Web.Features.InterviewPreparation;

public sealed record PreparationChoice(int Id, string Label, IReadOnlyList<string> Tags, IReadOnlyList<string?> SearchFields);
