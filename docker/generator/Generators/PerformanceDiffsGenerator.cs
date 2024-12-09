// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Diagnostics.CodeAnalysis;
using System.Text;
using Dapper;
using Generator.Models;
using Newtonsoft.Json;

namespace Generator.Generators
{
    public class PerformanceDiffsGenerator : IGenerator
    {
        private readonly Order order;

        public PerformanceDiffsGenerator(bool withMods, Order order)
        {
            this.order = order;

            StringBuilder sb = new StringBuilder("PP");

            sb.Append($" {order.ToString()}");
            sb.Append(withMods ? " (All)" : " (NM)");

            Name = sb.ToString();
            WithMods = withMods;
        }

        public string Name { get; }

        public ColumnDefinition[] Columns { get; } =
        {
            new ColumnDefinition("score_id"),
            new ColumnDefinition("enabled_mods"),
            new ColumnDefinition("beatmap", Width: 720),
            new ColumnDefinition("pp_master"),
            new ColumnDefinition("pp_pr"),
            new ColumnDefinition("diff"),
            new ColumnDefinition("diff%", ColumnType.Percentage),
        };

        public bool WithMods { get; }

        public async Task<object[][]> Query()
        {
            Console.WriteLine($"Querying PP diffs (mods: {WithMods}, type: {order})...");

            List<object[]> rows = new List<object[]>();

            using (var db = await Database.GetConnection())
            {
                string comparer = order == Order.Gains ? $"> {Env.TOLERANCE}" : $"< -{Env.TOLERANCE}";

                IAsyncEnumerable<ScoreDiff> diffs = db.QueryUnbufferedAsync<ScoreDiff>(
                    "SELECT "
                    + $"     `a`.`id` AS `{nameof(ScoreDiff.score_id)}`, "
                    + $"     `a`.`legacy_score_id` AS `{nameof(ScoreDiff.legacy_score_id)}`, "
                    + $"     `bm`.`beatmap_id` AS `{nameof(ScoreDiff.beatmap_id)}`, "
                    + $"     `bm`.`filename` AS `{nameof(ScoreDiff.beatmap_filename)}`, "
                    + $"     `a`.`pp` AS `{nameof(ScoreDiff.a_pp)}`, "
                    + $"     `b`.`pp` AS `{nameof(ScoreDiff.b_pp)}`, "
                    + $"     `a`.`data` AS '{nameof(ScoreDiff.data)}' "
                    + $"FROM `{Env.DB_A}`.`scores` `a` "
                    + $"JOIN `{Env.DB_B}`.`scores` `b` "
                    + "     ON `b`.`id` = `a`.`id` "
                    + $"JOIN `{Env.DB_A}`.`osu_beatmaps` `bm` "
                    + "     ON `bm`.`beatmap_id` = `a`.`beatmap_id` "
                    + $"WHERE {IGenerator.GenerateBeatmapFilter("bm")} "
                    + $"    AND `b`.`pp` - `a`.`pp` {comparer} "
                    + "ORDER BY `b`.`pp` - `a`.`pp` "
                    + (order == Order.Gains ? "DESC " : "ASC "), new
                    {
                        RulesetId = Env.RULESET_ID
                    }, commandTimeout: int.MaxValue);

                await foreach (var d in diffs)
                {
                    SoloScoreData scoreData = d.GetScoreData();

                    if (!this.ModsMatchFilter(scoreData.Mods, d.legacy_score_id))
                        continue;

                    rows.Add([
                        $"=HYPERLINK(\"https://osu.ppy.sh/scores/{d.score_id}\", \"{d.score_id}\")",
                        this.FormatMods(scoreData.Mods),
                        $"=HYPERLINK(\"https://osu.ppy.sh/b/{d.beatmap_id}\", \"{d.beatmap_filename}\")",
                        d.a_pp,
                        d.b_pp,
                        d.b_pp - d.a_pp,
                        d.a_pp == 0 ? 1.0f : d.b_pp / d.a_pp - 1
                    ]);

                    if (rows.Count == IGenerator.MAX_ROWS)
                        break;
                }

                Console.WriteLine($"Finished querying PP diffs (mods: {WithMods}, type: {order})...");

                return rows.ToArray();
            }
        }

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        [Serializable]
        private struct ScoreDiff
        {
            public ulong score_id { get; set; }
            public ulong? legacy_score_id { get; set; }

            public uint beatmap_id { get; set; }
            public string beatmap_filename { get; set; }

            public float a_pp { get; set; }
            public float b_pp { get; set; }

            public string data { get; set; }

            public SoloScoreData GetScoreData() => JsonConvert.DeserializeObject<SoloScoreData>(data) ?? new SoloScoreData();
        }
    }
}
