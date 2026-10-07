using System.Numerics;
using System.Text.Json.Serialization;

namespace Game.Core.Determinism;

/// <summary>
/// Deterministischer Zufallsgenerator (PCG32, Variante XSH-RR). Der Zustand ist vollständig
/// serialisierbar, damit Spielstände und Tests reproduzierbar sind. Ersetzt <see cref="System.Random"/>,
/// dessen Zustand nicht serialisierbar und dessen Algorithmus nicht garantiert stabil ist.
/// </summary>
/// <remarks>
/// Die Zufallsmethoden sind <c>internal</c>: Nur Core darf den Zustand verändern, Godot liest nur.
/// </remarks>
public sealed class DeterministicRandom
{
    private const ulong Multiplier = 6364136223846793005UL;

    [JsonConstructor]
    internal DeterministicRandom(ulong state, ulong increment)
    {
        if ((increment & 1UL) == 0)
        {
            throw new ArgumentException("Das Inkrement muss ungerade sein.", nameof(increment));
        }

        State = state;
        Increment = increment;
    }

    /// <summary>Aktueller interner Zustand.</summary>
    public ulong State { get; private set; }

    /// <summary>Stream-Inkrement, immer ungerade. Unterschiedliche Streams liefern unabhängige Folgen.</summary>
    public ulong Increment { get; }

    /// <summary>Erzeugt einen Generator aus Seed und Stream (Seeding wie in der PCG-Referenzimplementierung).</summary>
    public static DeterministicRandom FromSeed(ulong seed, ulong stream = 0)
    {
        var random = new DeterministicRandom(0, (stream << 1) | 1UL);
        random.NextUInt();
        random.State += seed;
        random.NextUInt();
        return random;
    }

    /// <summary>Gleichverteilte 32-Bit-Zahl.</summary>
    internal uint NextUInt()
    {
        ulong oldState = State;
        State = oldState * Multiplier + Increment;
        uint xorShifted = (uint)(((oldState >> 18) ^ oldState) >> 27);
        int rotation = (int)(oldState >> 59);
        return BitOperations.RotateRight(xorShifted, rotation);
    }

    /// <summary>Gleichverteilte Zahl in [0, <paramref name="maxExclusive"/>).</summary>
    internal int NextInt(int maxExclusive)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxExclusive);
        return (int)NextBounded((uint)maxExclusive);
    }

    /// <summary>Gleichverteilte Zahl in [<paramref name="minInclusive"/>, <paramref name="maxExclusive"/>).</summary>
    internal int NextInt(int minInclusive, int maxExclusive)
    {
        if (maxExclusive <= minInclusive)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxExclusive), "maxExclusive muss größer als minInclusive sein.");
        }

        uint range = (uint)((long)maxExclusive - minInclusive);
        return (int)(minInclusive + (long)NextBounded(range));
    }

    // Ohne Modulo-Verzerrung: Werte unterhalb der Schwelle werden verworfen.
    private uint NextBounded(uint bound)
    {
        uint threshold = (0u - bound) % bound;
        while (true)
        {
            uint value = NextUInt();
            if (value >= threshold)
            {
                return value % bound;
            }
        }
    }
}
