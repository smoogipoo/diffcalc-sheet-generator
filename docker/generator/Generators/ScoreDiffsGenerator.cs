// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Diagnostics.CodeAnalysis;
using System.Text;
using Dapper;
using Generator.Models;
using osu.Game.Online.API;

namespace Generator.Generators
{
    public class ScoreDiffsGenerator : IGenerator
    {
        private const int max_rows = 10000;

        private readonly bool withMods;
        private readonly Order order;

        public ScoreDiffsGenerator(bool withMods, Order order)
        {
            this.withMods = withMods;
            this.order = order;

            StringBuilder sb = new StringBuilder("Score");

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
            new ColumnDefinition("score_master"),
            new ColumnDefinition("score_pr"),
            new ColumnDefinition("diff"),
            new ColumnDefinition("diff%", ColumnType.Percentage),
        };

        public async Task<object[][]> Query()
        {
            Console.WriteLine($"Querying Score diffs (mods: {withMods}, type: {order})...");

            List<object[]> rows = new List<object[]>();

            using (var db = await Database.GetConnection())
            {
                var dbInfo = LegacyDatabaseHelper.GetRulesetSpecifics(Env.RULESET_ID);

                string comparer = order == Order.Gains ? "> 0" : "< 0";

                IEnumerable<ScoreDiff> diffs = await db.QueryAsync<ScoreDiff>(
                    "SELECT "
                    + $"     `h`.`score_id` AS `{nameof(ScoreDiff.highscore_id)}`, "
                    + $"     `a`.`id` AS `{nameof(ScoreDiff.score_id)}`, "
                    + $"     `a`.`beatmap_id` AS `{nameof(ScoreDiff.beatmap_id)}`, "
                    + $"     JSON_EXTRACT(`a`.`data`, '$.total_score') AS '{nameof(ScoreDiff.a_score)}', "
                    + $"     JSON_EXTRACT(`b`.`data`, '$.total_score') AS '{nameof(ScoreDiff.b_score)}' "
                    + $"FROM `{Env.DB_A}`.`{dbInfo.HighScoreTable}` `h` "
                    + $"JOIN `{Env.DB_A}`.`solo_scores_legacy_id_map` `ma` "
                    + "     ON `ma`.`ruleset_id` = @RulesetId "
                    + "     AND `ma`.`old_score_id` = `h`.`score_id` "
                    + $"JOIN `{Env.DB_B}`.`solo_scores_legacy_id_map` `mb` "
                    + "     ON `mb`.`ruleset_id` = @RulesetId "
                    + "     AND `mb`.`old_score_id` = `h`.`score_id` "
                    + $"JOIN `{Env.DB_A}`.`solo_scores` `a` "
                    + "     ON `a`.`id` = `ma`.`score_id` "
                    + $"JOIN `{Env.DB_B}`.`solo_scores` `b` "
                    + "     ON `b`.`id` = `mb`.`score_id` "
                    + $"WHERE JSON_EXTRACT(`b`.`data`, '$.total_score') - JSON_EXTRACT(`a`.`data`, '$.total_score') {comparer} "
                    + $"    AND `h`.`enabled_mods` {(withMods ? ">= 0 " : "= 0 ")} "
                    + "ORDER BY JSON_EXTRACT(`b`.`data`, '$.total_score') - JSON_EXTRACT(`a`.`data`, '$.total_score') "
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
                        d.a_score,
                        d.b_score,
                        d.b_score - d.a_score,
                        d.a_score == 0 ? 1.0f : d.b_score / d.a_score - 1
                    });
                }
            }

            Console.WriteLine($"Finished querying Score diffs (mods: {withMods}, type: {order})...");

            return rows.ToArray();
        }

        private static string getModString(APIMod[] mods) => mods.Any() ? string.Join(", ", mods.Select(m => m.Acronym.ToUpper())) : "NM";

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        [Serializable]
        private struct ScoreDiff
        {
            public ulong highscore_id;
            public ulong score_id;
            public uint beatmap_id;
            public float a_score;
            public float b_score;
        }
    }
}
