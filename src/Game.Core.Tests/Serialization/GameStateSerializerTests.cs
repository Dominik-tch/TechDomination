using System.Text;
using System.Text.Json;
using Game.Core.Serialization;
using Game.Core.State;

namespace Game.Core.Tests.Serialization;

public class GameStateSerializerTests
{
    [Fact]
    public void SameState_SerializedTwice_ProducesIdenticalJson()
    {
        var state = CreateAdvancedState(seed: 7, ticks: 25);

        byte[] first = GameStateSerializer.SerializeToUtf8Bytes(state);
        byte[] second = GameStateSerializer.SerializeToUtf8Bytes(state);

        Assert.Equal(first, second);
    }

    [Fact]
    public void EqualStates_BuiltIndependently_ProduceIdenticalJson()
    {
        var first = CreateAdvancedState(seed: 7, ticks: 25);
        var second = CreateAdvancedState(seed: 7, ticks: 25);

        Assert.Equal(
            GameStateSerializer.SerializeToUtf8Bytes(first),
            GameStateSerializer.SerializeToUtf8Bytes(second));
    }

    [Fact]
    public void RoundTrip_ProducesIdenticalJson()
    {
        var state = CreateAdvancedState(seed: 7, ticks: 25);
        byte[] original = GameStateSerializer.SerializeToUtf8Bytes(state);

        byte[] roundTripped = GameStateSerializer.SerializeToUtf8Bytes(GameStateSerializer.Deserialize(original));

        Assert.Equal(original, roundTripped);
    }

    [Fact]
    public void RoundTrip_PreservesValues()
    {
        var state = CreateAdvancedState(seed: 7, ticks: 25);

        var loaded = GameStateSerializer.Deserialize(GameStateSerializer.SerializeToUtf8Bytes(state));

        Assert.Equal(state.Seed, loaded.Seed);
        Assert.Equal(state.Tick, loaded.Tick);
        Assert.Equal(state.Rng.State, loaded.Rng.State);
        Assert.Equal(state.Rng.Increment, loaded.Rng.Increment);
    }

    [Fact]
    public void RoundTrip_PreservesProvinceOwners()
    {
        var state = CreateAdvancedState(seed: 7, ticks: 5);
        state.GetProvince(new ProvinceId(2)).Owner = new NationId(0);

        var loaded = GameStateSerializer.Deserialize(GameStateSerializer.SerializeToUtf8Bytes(state));

        Assert.Equal(state.Provinces.Select(p => p.Owner), loaded.Provinces.Select(p => p.Owner));
        Assert.Equal(new NationId(0), loaded.GetProvince(new ProvinceId(2)).Owner);
    }

    [Fact]
    public void RoundTrip_ViaStream_ProducesIdenticalJson()
    {
        var state = CreateAdvancedState(seed: 3, ticks: 10);
        using var stream = new MemoryStream();

        GameStateSerializer.Serialize(state, stream);
        stream.Position = 0;
        var loaded = GameStateSerializer.Deserialize(stream);

        Assert.Equal(
            GameStateSerializer.SerializeToUtf8Bytes(state),
            GameStateSerializer.SerializeToUtf8Bytes(loaded));
    }

    [Fact]
    public void Deserialize_NullJson_Throws()
    {
        Assert.Throws<JsonException>(() => GameStateSerializer.Deserialize("null"u8));
    }

    [Fact]
    public void Deserialize_MissingRng_Throws()
    {
        byte[] json = Encoding.UTF8.GetBytes("""{ "seed": 1, "tick": 0, "provinces": [] }""");

        Assert.Throws<ArgumentNullException>(() => GameStateSerializer.Deserialize(json));
    }

    private static GameState CreateAdvancedState(ulong seed, int ticks)
    {
        var data = TestData.Load();
        var state = GameStateFactory.CreateNew(data, seed);
        for (int i = 0; i < ticks; i++)
        {
            Simulation.Step(state, data);
            state.Rng.NextUInt();
        }

        return state;
    }
}
