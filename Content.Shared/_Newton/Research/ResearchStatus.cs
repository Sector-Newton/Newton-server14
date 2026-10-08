using Robust.Shared.Serialization;

namespace Content.Shared.Newton.Research;

[Serializable, NetSerializable]
public enum ResearchStatus : byte
{
    Researched,
    Available,
    Prerequisites,
    Unavailable
}