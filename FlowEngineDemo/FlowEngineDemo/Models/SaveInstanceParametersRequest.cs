using System.Text.Json;

namespace FlowEngineDemo.Models
{
    public class SaveInstanceParametersRequest
    {
        public string FlowInstanceId { get; set; } = string.Empty;

        public Dictionary<string, JsonElement> Parameters { get; set; }
            = new();
    }
}