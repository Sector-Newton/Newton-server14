using Robust.Shared.Serialization;
using Content.Shared.Newton.Research; // Newton

namespace Content.Shared.Research.Components
{
    [NetSerializable, Serializable]
    public enum ResearchConsoleUiKey : byte
    {
        Key,
    }

    [Serializable, NetSerializable]
    public sealed class ConsoleUnlockTechnologyMessage : BoundUserInterfaceMessage
    {
        public string Id;

        public ConsoleUnlockTechnologyMessage(string id)
        {
            Id = id;
        }
    }

    [Serializable, NetSerializable]
    public sealed class ConsoleServerSelectionMessage : BoundUserInterfaceMessage
    {

    }

    [Serializable, NetSerializable]
    public sealed class ResearchConsoleBoundInterfaceState : BoundUserInterfaceState
    {
        public int Points;
        public Dictionary<string, ResearchStatus> ResearchStatus; // Newton
        public ResearchConsoleBoundInterfaceState(int points, Dictionary<string, ResearchStatus> researchStatus) // Newton
        {
            Points = points;
            ResearchStatus = researchStatus; // Newton
        }
    }
}
