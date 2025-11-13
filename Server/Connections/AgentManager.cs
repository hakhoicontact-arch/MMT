// Connections/AgentManager.cs
public class AgentManager
{
    private readonly Dictionary<string, string> _agents = new();

    public void RegisterAgent(string connectionId, string agentId)
    {
        _agents[agentId] = connectionId;
    }

    public string GetAgentConnectionId(string agentId)
    {
        return _agents.GetValueOrDefault(agentId);
    }

    public void RemoveAgent(string agentId)
    {
        _agents.Remove(agentId);
    }

    public IEnumerable<string> GetOnlineAgents() => _agents.Keys;
}