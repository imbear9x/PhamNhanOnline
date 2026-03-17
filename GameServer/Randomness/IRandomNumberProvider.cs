namespace GameServer.Randomness;

public interface IRandomNumberProvider
{
    int NextInt(int exclusiveMax);
}

public sealed class CryptoRandomNumberProvider : IRandomNumberProvider
{
    public int NextInt(int exclusiveMax)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(exclusiveMax);
        return System.Security.Cryptography.RandomNumberGenerator.GetInt32(exclusiveMax);
    }
}

public sealed class DeterministicRandomNumberProvider : IRandomNumberProvider
{
    private readonly Random _random;

    public DeterministicRandomNumberProvider(int seed)
    {
        _random = new Random(seed);
    }

    public int NextInt(int exclusiveMax)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(exclusiveMax);
        return _random.Next(exclusiveMax);
    }
}
