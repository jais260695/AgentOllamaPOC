namespace AgentOllamaPOC.Guardrails.Tokens;

public sealed class TokenBudget
{
    private readonly object _lock = new();

    private readonly int _maxInputTokensPerCall;
    private readonly int _maxOutputTokensPerCall;
    private readonly long _maxTotalTokensPerRequest;

    private long _consumedTokens;
    private long _reservedTokens;

    public TokenBudget(
        int maxInputTokensPerCall,
        int maxOutputTokensPerCall,
        int maxTotalTokensPerRequest)
    {
        if (maxInputTokensPerCall <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxInputTokensPerCall));

        if (maxOutputTokensPerCall <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxOutputTokensPerCall));

        if (maxTotalTokensPerRequest <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxTotalTokensPerRequest));

        _maxInputTokensPerCall = maxInputTokensPerCall;
        _maxOutputTokensPerCall = maxOutputTokensPerCall;
        _maxTotalTokensPerRequest = maxTotalTokensPerRequest;
    }

    public long ConsumedTokens
    {
        get
        {
            lock (_lock)
                return _consumedTokens;
        }
    }

    public long ReservedTokens
    {
        get
        {
            lock (_lock)
                return _reservedTokens;
        }
    }

    public long RemainingTokens
    {
        get
        {
            lock (_lock)
                return Math.Max(
                    0,
                    _maxTotalTokensPerRequest - _consumedTokens - _reservedTokens);
        }
    }

    public bool IsExceeded
    {
        get
        {
            lock (_lock)
                return _consumedTokens + _reservedTokens > _maxTotalTokensPerRequest;
        }
    }

    public TokenReservation Reserve(int estimatedInputTokens)
    {
        if (estimatedInputTokens < 0)
            throw new ArgumentOutOfRangeException(nameof(estimatedInputTokens));

        if (estimatedInputTokens > _maxInputTokensPerCall)
            throw new InvalidOperationException("Input token limit per call exceeded.");

        lock (_lock)
        {
            var availableTokens =
                _maxTotalTokensPerRequest - _consumedTokens - _reservedTokens;

            var availableOutputTokens = availableTokens - estimatedInputTokens;

            if (availableOutputTokens <= 0)
                throw new InvalidOperationException("Request token budget exhausted.");

            var outputTokens = (int)Math.Min(
                _maxOutputTokensPerCall,
                availableOutputTokens);

            var totalReservation = (long)estimatedInputTokens + outputTokens;

            _reservedTokens += totalReservation;

            return new TokenReservation(
                this,
                estimatedInputTokens,
                outputTokens);
        }
    }

    internal void Reconcile(
        int reservedInputTokens,
        int reservedOutputTokens,
        int actualInputTokens,
        int actualOutputTokens)
    {
        if (actualInputTokens < 0)
            throw new ArgumentOutOfRangeException(nameof(actualInputTokens));

        if (actualOutputTokens < 0)
            throw new ArgumentOutOfRangeException(nameof(actualOutputTokens));

        lock (_lock)
        {
            var reservedTotal = (long)reservedInputTokens + reservedOutputTokens;
            var actualTotal = (long)actualInputTokens + actualOutputTokens;

            _reservedTokens -= reservedTotal;
            _consumedTokens += actualTotal;
        }
    }

    internal void Commit(
        int reservedInputTokens,
        int reservedOutputTokens)
    {
        lock (_lock)
        {
            var reservedTotal = (long)reservedInputTokens + reservedOutputTokens;

            _reservedTokens -= reservedTotal;
            _consumedTokens += reservedTotal;
        }
    }
}