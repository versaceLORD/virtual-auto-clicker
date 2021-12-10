using System.Text.Json.Serialization;

namespace VirtualAutoClicker.Models
{
    public class Configuration
    {
        [JsonPropertyName("processWindowed")]
        public bool ProcessWindowed { get; set; }
            
        [JsonPropertyName("windowTopBarOffset")]
        public int WindowTopBarOffset { get; set; }
    }
}