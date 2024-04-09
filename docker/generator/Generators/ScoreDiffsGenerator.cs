// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Diagnostics.CodeAnalysis;
using System.Text;
using Dapper;
using Generator.Models;
using Newtonsoft.Json;
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
            new ColumnDefinition("enabled_mods"),
            new ColumnDefinition("beatmap", Width: 720),
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
                string comparer = order == Order.Gains ? "> 0" : "< 0";

                IEnumerable<ScoreDiff> diffs = await db.QueryAsync<ScoreDiff>(
                    "SELECT "
                    + $"     `a`.`id` AS `{nameof(ScoreDiff.score_id)}`, "
                    + $"     `a`.`legacy_score_id` AS `{nameof(ScoreDiff.legacy_score_id)}`, "
                    + $"     `bm`.`beatmap_id` AS `{nameof(ScoreDiff.beatmap_id)}`, "
                    + $"     `bm`.`filename` AS `{nameof(ScoreDiff.beatmap_filename)}`, "
                    + $"     `a`.`total_score` AS '{nameof(ScoreDiff.a_score)}', "
                    + $"     `b`.`total_score` AS '{nameof(ScoreDiff.b_score)}', "
                    + $"     `a`.`data` AS '{nameof(ScoreDiff.data)}' "
                    + $"FROM `{Env.DB_A}`.`scores` `a` "
                    + $"JOIN `{Env.DB_B}`.`scores` `b` "
                    + "     ON `b`.`id` = `a`.`id` "
                    + $"JOIN `{Env.DB_A}`.`osu_beatmaps` `bm` "
                    + "     ON `bm`.`beatmap_id` = `a`.`beatmap_id` "
                    + $"WHERE CAST(`b`.`total_score` AS SIGNED) - CAST(`a`.`total_score` AS SIGNED) {comparer} "
                    + "ORDER BY CAST(`b`.`total_score` AS SIGNED) - CAST(`a`.`total_score` AS SIGNED) "
                    + (order == Order.Gains ? "DESC " : "ASC ")
                    + $"LIMIT {max_rows}", new
                    {
                        RulesetId = Env.RULESET_ID
                    }, commandTimeout: int.MaxValue);

                foreach (var d in diffs)
                {
                    SoloScoreData scoreData = d.GetScoreData();

                    if (!withMods && scoreData.Mods.Length > (d.legacy_score_id > 0 ? 1 : 0))
                        continue;

                    rows.Add([
                        $"=HYPERLINK(\"https://osu.ppy.sh/scores/{d.score_id}\", \"{d.score_id}\")",
                        getModString(scoreData.Mods.ToArray()),
                        $"=HYPERLINK(\"https://osu.ppy.sh/b/{d.beatmap_id}\", \"{d.beatmap_filename}\")",
                        d.a_score,
                        d.b_score,
                        d.b_score - d.a_score,
                        d.a_score == 0 ? 1.0f : d.b_score / d.a_score - 1
                    ]);
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
            public ulong score_id;
            public ulong? legacy_score_id;

            public uint beatmap_id;
            public string beatmap_filename;

            public float a_score;
            public float b_score;

            public string data;

            public SoloScoreData GetScoreData() => JsonConvert.DeserializeObject<SoloScoreData>(data) ?? new SoloScoreData();
        }
    }
}
