// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Diagnostics.CodeAnalysis;
using System.Text;
using Dapper;
using osu.Game.Beatmaps.Legacy;
using osu.Game.Online.API;

namespace Generator.Generators
{
    public class StarRatingDiffsGenerator : IGenerator
    {
        private readonly Order order;

        public StarRatingDiffsGenerator(bool withMods, Order order)
        {
            this.order = order;

            StringBuilder sb = new StringBuilder("SR");

            sb.Append($" {order.ToString()}");
            sb.Append(withMods ? " (All)" : " (NM)");

            Name = sb.ToString();
            WithMods = withMods;
        }

        public string Name { get; }

        public ColumnDefinition[] Columns { get; } =
        {
            new ColumnDefinition("mods"),
            new ColumnDefinition("beatmap", Width: 720),
            new ColumnDefinition("sr_master"),
            new ColumnDefinition("sr_pr"),
            new ColumnDefinition("diff"),
            new ColumnDefinition("diff%", ColumnType.Percentage),
        };

        public bool WithMods { get; }

        public async Task<object[][]> Query()
        {
            Console.WriteLine($"Querying SR diffs (mods: {WithMods}, type: {order})...");

            List<object[]> rows = new List<object[]>();

            using (var db = await Database.GetConnection())
            {
                string comparer = order == Order.Gains ? "> 0.1" : "< -0.1";

                IAsyncEnumerable<BeatmapDiff> diffs = db.QueryUnbufferedAsync<BeatmapDiff>(
                    "SELECT "
                    + $"     `a`.`beatmap_id` AS `{nameof(BeatmapDiff.id)}`, "
                    + $"     `bm`.`playmode` AS `{nameof(BeatmapDiff.playmode)}`, "
                    + $"     `bm`.`filename` AS `{nameof(BeatmapDiff.filename)}`, "
                    + $"     `a`.`mods` AS `{nameof(BeatmapDiff.mods)}`, "
                    + $"     `a`.`diff_unified` AS `{nameof(BeatmapDiff.a_sr)}`, "
                    + $"     `b`.`diff_unified` AS `{nameof(BeatmapDiff.b_sr)}` "
                    // Select beatmap difficulties from each database...
                    + $"FROM `{Env.DB_A}`.`osu_beatmap_difficulty` `a` "
                    + $"JOIN `{Env.DB_B}`.`osu_beatmap_difficulty` `b` "
                    + "     ON `a`.`beatmap_id` = `b`.`beatmap_id` "
                    + "     AND `a`.`mods` = `b`.`mods` "
                    + "     AND `a`.`mode` = `b`.`mode` "
                    // And the beatmap for additional filtering.
                    + $"JOIN `{Env.DB_A}`.`osu_beatmaps` `bm` "
                    + "     ON `bm`.`beatmap_id` = `a`.`beatmap_id` "
                    + "WHERE `a`.`mode` = @RulesetId "
                    + $"    AND {IGenerator.GenerateBeatmapFilter("bm")} "
                    + $"    AND `b`.`diff_unified` - `a`.`diff_unified` {comparer} "
                    + $"    AND `a`.`mods` {(WithMods ? ">= 0 " : "= 0 ")}"
                    + "ORDER BY `b`.`diff_unified` - `a`.`diff_unified` "
                    + (order == Order.Gains ? "DESC " : "ASC "), new
                    {
                        RulesetId = Env.RULESET_ID
                    }, commandTimeout: int.MaxValue);

                await foreach (var d in diffs)
                {
                    APIMod[] mods = LegacyRulesetHelper.GetRulesetFromLegacyId(d.playmode)
                                                       .ConvertFromLegacyMods((LegacyMods)d.mods)
                                                       .Select(m => new APIMod(m))
                                                       .ToArray();

                    if (!this.ModsMatchFilter(mods, null))
                        continue;

                    rows.Add([
                        this.FormatMods(mods),
                        $"=HYPERLINK(\"https://osu.ppy.sh/b/{d.id}\", \"{d.filename}\")",
                        d.a_sr,
                        d.b_sr,
                        d.b_sr - d.a_sr,
                        d.a_sr == 0 ? 1.0f : d.b_sr / d.a_sr - 1
                    ]);

                    if (rows.Count == IGenerator.MAX_ROWS)
                        break;
                }
            }

            Console.WriteLine($"Finished querying SR diffs (mods: {WithMods}, type: {order})...");

            return rows.ToArray();
        }

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        [Serializable]
        private struct BeatmapDiff
        {
            public uint id { get; set; }
            public byte playmode { get; set; }
            public string filename { get; set; }

            public int mods { get; set; }

            public float a_sr { get; set; }
            public float b_sr { get; set; }
        }
    }
}
