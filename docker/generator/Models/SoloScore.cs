// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Diagnostics.CodeAnalysis;
using Dapper.Contrib.Extensions;
using Newtonsoft.Json;
using osu.Game.Scoring;

namespace Generator.Models;

[SuppressMessage("ReSharper", "InconsistentNaming")]
[Serializable]
[Table(TABLE_NAME)]
public class SoloScore
{
    public const string TABLE_NAME = "solo_scores";

    [ExplicitKey]
    public ulong id { get; set; }

    public uint user_id { get; set; }

    public uint beatmap_id { get; set; }

    public ushort ruleset_id { get; set; }

    public bool has_replay { get; set; }
    public bool preserve { get; set; }
    public bool ranked { get; set; } = true;

    public ScoreRank rank { get; set; }

    public bool passed { get; set; } = true;

    public float accuracy { get; set; }

    public uint max_combo { get; set; }

    public uint total_score { get; set; }

    public SoloScoreData ScoreData = new SoloScoreData();

    public string data
    {
        get => JsonConvert.SerializeObject(ScoreData);
        set
        {
            var soloScoreData = JsonConvert.DeserializeObject<SoloScoreData>(value);
            if (soloScoreData != null)
                ScoreData = soloScoreData;
        }
    }

    public double? pp { get; set; }

    public ulong? legacy_score_id { get; set; }
    public uint? legacy_total_score { get; set; }

    public DateTimeOffset? started_at { get; set; }
    public DateTimeOffset ended_at { get; set; }

    public override string ToString() => $"score_id: {id} user_id: {user_id}";

    public ushort? build_id { get; set; }
}