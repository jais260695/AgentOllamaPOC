namespace AgentOllamaPOC.Guardrails.Tokens;

public sealed class TokenReservation
{
    private readonly TokenBudget _budget;
    private int _completed;

    internal TokenReservation(
        TokenBudget budget,
        int reservedInputTokens,
        int reservedOutputTokens)
    {
        _budget = budget;
        ReservedInputTokens = reservedInputTokens;
        ReservedOutputTokens = reservedOutputTokens;
    }

    public int ReservedInputTokens { get; }

    public int ReservedOutputTokens { get; }

    public void Reconcile(int actualInputTokens, int actualOutputTokens)
    {
        if (Interlocked.Exchange(ref _completed, 1) != 0)
            throw new InvalidOperationException(
                "Reservation has already been completed.");

        _budget.Reconcile(
            ReservedInputTokens,
            ReservedOutputTokens,
            actualInputTokens,
            actualOutputTokens);
    }

    public void CommitReservation()
    {
        if (Interlocked.Exchange(ref _completed, 1) != 0)
            return;

        // Conservatively charge the full reservation when actual usage is unknown.
        _budget.Commit(
            ReservedInputTokens,
            ReservedOutputTokens);
    }
}