namespace EchoServiceApi;

public class DiagnosticInfo
{
    public Dictionary<string, object?> LoggingScopeState { get; internal set; } = new Dictionary<string, object?>();
}
