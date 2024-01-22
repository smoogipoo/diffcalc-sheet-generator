using Newtonsoft.Json;
using osu.Game.Online.API;
using osu.Game.Rulesets.Scoring;

namespace Generator.Models;

[Serializable]
public class SoloScoreData
{
    [JsonProperty("mods")]
    public APIMod[] Mods { get; set; } = Array.Empty<APIMod>();

    [JsonProperty("statistics")]
    public Dictionary<HitResult, int> Statistics { get; set; } = new Dictionary<HitResult, int>();

    [JsonProperty("maximum_statistics")]
    public Dictionary<HitResult, int> MaximumStatistics { get; set; } = new Dictionary<HitResult, int>();
}
