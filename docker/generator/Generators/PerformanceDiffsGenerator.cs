// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Diagnostics.CodeAnalysis;
using System.Text;
using Dapper;
using Generator.Models;
using osu.Game.Online.API;

namespace Generator.Generators
{
    public class PerformanceDiffsGenerator : IGenerator
    {
        private const int max_rows = 10000;

        private readonly bool withMods;
        private readonly Order order;

        public PerformanceDiffsGenerator(bool withMods, Order order)
        {
            this.withMods = withMods;
            this.order = order;

            StringBuilder sb = new StringBuilder("PP");

            sb.Append($" {order.ToString()}");
            sb.Append(withMods ? " (All)" : " (NM)");

            Name = sb.ToString();
        }

        public string Name { get; }

        public ColumnDefinition[] Columns { get; } =
        {
            new ColumnDefinition("score_id"),
            new ColumnDefinition("beatmap_id"),
            new ColumnDefinition("enabled_mods"),
            new ColumnDefinition("filename", Width: 720),
            new ColumnDefinition("pp_master"),
            new ColumnDefinition("pp_pr"),
            new ColumnDefinition("diff"),
            new ColumnDefinition("diff%", ColumnType.Percentage),
        };

        public async Task<object[][]> Query()
        {
            Console.WriteLine($"Querying PP diffs (mods: {withMods}, type: {order})...");

            List<object[]> rows = new List<object[]>();

            using (var db = await Database.GetConnection())
            {
                var dbInfo = LegacyDatabaseHelper.GetRulesetSpecifics(Env.RULESET_ID);

                StringBuilder beatmapQuery = new StringBuilder();

                if (Env.NO_CONVERTS)
                    beatmapQuery.AppendLine("AND `b`.`playmode` = @RulesetId ");
                if (Env.RANKED_ONLY)
                    beatmapQuery.AppendLine("AND `b`.`approved` IN (1, 2) ");

                IEnumerable<ScoreDiff> diffs = await db.QueryAsync<ScoreDiff>(
                    "SELECT "
                    + $"     `h`.`score_id` AS `{nameof(ScoreDiff.highscore_id)}`, "
                    + $"     `a`.`score_id` AS `{nameof(ScoreDiff.score_id)}`, "
                    + $"     `bm`.`beatmap_id` AS `{nameof(ScoreDiff.beatmap_id)}`, "
                    + $"     `a`.`pp` AS `{nameof(ScoreDiff.a_pp)}`, "
                    + $"     `b`.`pp` AS `{nameof(ScoreDiff.b_pp)}` "
                    // Select as highscores and map for each database... These tables are only used for solo score lookups.
                    // Todo: This should go away once data.ppy.sh provides solo_scores.
                    + $"FROM `{Env.DB_A}`.`{dbInfo.HighScoreTable}` `h` "
                    + $"JOIN `{Env.DB_A}`.`{SoloScoreLegacyIDMap.TABLE_NAME}` `ma` "
                    + "     ON `ma`.`old_score_id` = `h`.`score_id` "
                    + $"JOIN `{Env.DB_B}`.`{SoloScoreLegacyIDMap.TABLE_NAME}` `mb` "
                    + "     ON `mb`.`old_score_id` = `h`.`score_id` "
                    // Select the solo score performance from each database...
                    + $"JOIN `{Env.DB_A}`.`{SoloScorePerformance.TABLE_NAME}` `a` "
                    + "     ON `a`.`score_id` = `ma`.`score_id` "
                    + $"JOIN `{Env.DB_B}`.`{SoloScorePerformance.TABLE_NAME}` `b` "
                    + "     ON `b`.`score_id` = `mb`.`score_id` "
                    // And also include the solo score itself for filtering.
                    + $"JOIN `{Env.DB_A}`.`{SoloScore.TABLE_NAME}` `s` "
                    + "     ON `s`.`id` = `a`.`score_id` "
                    // And the beatmap for additional filtering.
                    + $"JOIN `{Env.DB_A}`.`{Beatmap.TABLE_NAME}` `bm` "
                    + "     ON `bm`.`beatmap_id` = `s`.`beatmap_id` "
                    + "WHERE `s`.`ruleset_id` = @RulesetId "
                    + beatmapQuery
                    + "     AND ABS(`a`.`pp` - `b`.`pp`) > 0.1 "
                    + $"    AND JSON_LENGTH(JSON_EXTRACT(`s`.`data`, \"$.mods\")) {(withMods ? "> 1 " : "= 1 ")}"
                    + "ORDER BY `b`.`pp` - `a`.`pp` "
                    + (order == Order.Gains ? "DESC " : "ASC ")
                    + $"LIMIT {max_rows}", new
                    {
                        RulesetId = Env.RULESET_ID
                    }, commandTimeout: int.MaxValue);

                foreach (var d in diffs)
                {
                    SoloScore scoreTask = await db.QuerySingleAsync<SoloScore>($"SELECT * FROM `{Env.DB_A}`.`{SoloScore.TABLE_NAME}` WHERE `id` = @ScoreId", new
                    {
                        ScoreId = d.score_id
                    });

                    Beatmap beatmapTask = await db.QuerySingleAsync<Beatmap>($"SELECT * FROM `{Env.DB_A}`.`{Beatmap.TABLE_NAME}` WHERE `beatmap_id` = @BeatmapId", new
                    {
                        BeatmapId = d.beatmap_id
                    });

                    rows.Add(new object[]
                    {
                        d.highscore_id,
                        beatmapTask.beatmap_id,
                        getModString(scoreTask.ScoreInfo.mods.ToArray()),
                        beatmapTask.filename,
                        d.a_pp,
                        d.b_pp,
                        d.b_pp - d.a_pp,
                        d.a_pp == 0 ? 1.0f : d.b_pp / d.a_pp - 1
                    });
                }

                Console.WriteLine($"Finished querying PP diffs (mods: {withMods}, type: {order})...");

                return rows.ToArray();
            }
        }

        private static string getModString(APIMod[] mods) => mods.Any() ? string.Join(", ", mods.Select(m => m.Acronym.ToUpper())) : "NM";

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        [Serializable]
        private struct ScoreDiff
        {
            public ulong highscore_id;
            public ulong score_id;
            public uint beatmap_id;
            public float a_pp;
            public float b_pp;
        }
    }
}
