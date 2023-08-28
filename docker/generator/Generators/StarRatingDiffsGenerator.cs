// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Diagnostics.CodeAnalysis;
using System.Text;
using Dapper;
using Generator.Models;
using osu.Game.Beatmaps.Legacy;
using osu.Game.Rulesets.Mods;

namespace Generator.Generators
{
    public class StarRatingDiffsGenerator : IGenerator
    {
        private const int max_rows = 10000;

        private readonly bool withMods;
        private readonly Order order;

        public StarRatingDiffsGenerator(bool withMods, Order order)
        {
            this.withMods = withMods;
            this.order = order;

            StringBuilder sb = new StringBuilder("SR");

            sb.Append($" {order.ToString()}");
            sb.Append(withMods ? " (All)" : " (NM)");

            Name = sb.ToString();
        }

        public string Name { get; }

        public ColumnDefinition[] Columns { get; } =
        {
            new ColumnDefinition("beatmap_id"),
            new ColumnDefinition("mods"),
            new ColumnDefinition("filename", Width: 720),
            new ColumnDefinition("sr_master"),
            new ColumnDefinition("sr_pr"),
            new ColumnDefinition("diff"),
            new ColumnDefinition("diff%", ColumnType.Percentage),
        };

        public async Task<object[][]> Query()
        {
            Console.WriteLine($"Querying SR diffs (mods: {withMods}, type: {order})...");

            List<object[]> rows = new List<object[]>();

            using (var db = await Database.GetConnection())
            {
                StringBuilder beatmapQuery = new StringBuilder();

                if (Env.NO_CONVERTS)
                    beatmapQuery.AppendLine("AND `m`.`playmode` = @RulesetId ");
                if (Env.RANKED_ONLY)
                    beatmapQuery.AppendLine("AND `m`.`approved` IN (1, 2) ");

                IEnumerable<BeatmapDiff> diffs = await db.QueryAsync<BeatmapDiff>(
                    "SELECT "
                    + $"     `a`.`beatmap_id` AS `{nameof(BeatmapDiff.beatmap_id)}`, "
                    + $"     `a`.`mods` AS `{nameof(BeatmapDiff.mods)}`, "
                    + $"     `a`.`diff_unified` AS `{nameof(BeatmapDiff.a_sr)}`, "
                    + $"     `b`.`diff_unified` AS `{nameof(BeatmapDiff.b_sr)}` "
                    // Select beatmap difficulties from each database...
                    + $"FROM `{Env.DB_A}`.`{BeatmapDifficulty.TABLE_NAME}` `a` "
                    + $"JOIN `{Env.DB_B}`.`{BeatmapDifficulty.TABLE_NAME}` `b` "
                    + "     ON `a`.`beatmap_id` = `b`.`beatmap_id` "
                    + "     AND `a`.`mods` = `b`.`mods` "
                    + "     AND `a`.`mode` = `b`.`mode` "
                    // And the beatmap for additional filtering.
                    + $"JOIN `{Env.DB_A}`.`{Beatmap.TABLE_NAME}` `m` "
                    + "     ON `m`.`beatmap_id` = `a`.`beatmap_id` "
                    + "WHERE `a`.`mode` = @RulesetId "
                    + beatmapQuery
                    + "     AND ABS(`a`.`diff_unified` - `b`.`diff_unified`) > 0.1 "
                    + $"    AND `a`.`mods` {(withMods ? ">= 0 " : "= 0 ")}"
                    + "ORDER BY `b`.`diff_unified` - `a`.`diff_unified` "
                    + (order == Order.Gains ? "DESC " : "ASC ")
                    + $"LIMIT {max_rows}", new
                    {
                        RulesetId = Env.RULESET_ID
                    }, commandTimeout: int.MaxValue);

                foreach (var d in diffs)
                {
                    Beatmap beatmapTask = await db.QuerySingleAsync<Beatmap>($"SELECT * FROM `{Env.DB_A}`.`{Beatmap.TABLE_NAME}` WHERE `beatmap_id` = @BeatmapId", new
                    {
                        BeatmapId = d.beatmap_id
                    });

                    rows.Add(new object[]
                    {
                        d.beatmap_id,
                        getModString(LegacyRulesetHelper.GetRulesetFromLegacyId(beatmapTask.playmode).ConvertFromLegacyMods((LegacyMods)d.mods).ToArray()),
                        beatmapTask.filename,
                        d.a_sr,
                        d.b_sr,
                        d.b_sr - d.a_sr,
                        d.a_sr == 0 ? 1.0f : d.b_sr / d.a_sr - 1
                    });
                }
            }

            Console.WriteLine($"Finished querying SR diffs (mods: {withMods}, type: {order})...");

            return rows.ToArray();
        }

        private static string getModString(Mod[] mods) => mods.Any() ? string.Join(", ", mods.Select(m => m.Acronym.ToUpper())) : "NM";

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        [Serializable]
        private struct BeatmapDiff
        {
            public uint beatmap_id;
            public int mods;
            public float a_sr;
            public float b_sr;
        }
    }
}
