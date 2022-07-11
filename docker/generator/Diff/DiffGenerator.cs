// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Text;
using Dapper;
using Generator.Models;

namespace Generator.Diff;

public class DiffGenerator
{
    private const int max_rows = 10000;

    private readonly string dbA;
    private readonly string dbB;
    private readonly int rulesetId;
    private readonly bool converts;
    private readonly bool rankedOnly;
    private readonly bool withMods;
    private readonly DiffType type;

    public DiffGenerator(string dbA, string dbB, bool withMods, DiffType type)
    {
        this.dbA = dbA;
        this.dbB = dbB;
        this.withMods = withMods;
        this.type = type;

        rulesetId = int.Parse(Environment.GetEnvironmentVariable("RULESET_ID") ?? "0");
        converts = Environment.GetEnvironmentVariable("NO_CONVERTS") != "1";
        rankedOnly = Environment.GetEnvironmentVariable("RANKED_ONLY") == "1";
    }

    public async Task<List<ProcessedScoreDiff>> QueryPpDiffs()
    {
        List<ProcessedScoreDiff> scores = new List<ProcessedScoreDiff>(max_rows);

        using (var db = await Database.GetConnection())
        {
            Console.WriteLine($"Querying PP diffs (mods: {withMods}, type: {type})...");

            var dbInfo = LegacyDatabaseHelper.GetRulesetSpecifics(rulesetId);

            StringBuilder beatmapQuery = new StringBuilder();
            if (!converts)
                beatmapQuery.AppendLine("AND `b`.`playmode` = @RulesetId ");
            if (rankedOnly)
                beatmapQuery.AppendLine("AND `b`.`approved` IN (1, 2) ");

            string modComparison = withMods ? "IS NOT NULL " : "IS NULL ";
            string order = type == DiffType.Gains ? "DESC " : "ASC ";

            IEnumerable<ScoreDiff> diffs = await db.QueryAsync<ScoreDiff>(
                "SELECT "
                + $"     `h`.`score_id` AS `{nameof(ScoreDiff.highscore_id)}`, "
                + $"     `a`.`score_id` AS `{nameof(ScoreDiff.score_id)}`, "
                + $"     `b`.`beatmap_id` AS `{nameof(ScoreDiff.beatmap_id)}`, "
                + $"     `a`.`pp` AS `{nameof(ScoreDiff.a_pp)}`, "
                + $"     `b`.`pp` AS `{nameof(ScoreDiff.b_pp)}` "
                // Select as highscores and map for each database... These tables are only used for solo score lookups.
                // Todo: This should go away once data.ppy.sh provides solo_scores.
                + $"FROM `{dbA}`.`{dbInfo.HighScoreTable}` `h` "
                + $"JOIN `{dbA}`.`{SoloScoreLegacyIDMap.TABLE_NAME}` `ma` "
                + "     ON `ma`.`old_score_id` = `h`.`score_id` "
                + $"JOIN `{dbB}`.`{SoloScoreLegacyIDMap.TABLE_NAME}` `mb` "
                + "     ON `mb`.`old_score_id` = `h`.`score_id` "
                // Select the solo score performance from each database...
                + $"JOIN `{dbA}`.`{SoloScorePerformance.TABLE_NAME}` `a` "
                + "     ON `a`.`score_id` = `ma`.`score_id` "
                + $"JOIN `{dbB}`.`{SoloScorePerformance.TABLE_NAME}` `b` "
                + "     ON `b`.`score_id` = `mb`.`score_id` "
                // And also include the solo score itself for filtering.
                + $"JOIN `{dbA}`.`{SoloScore.TABLE_NAME}` `s` "
                + "     ON `s`.`id` = `a`.`score_id` "
                // And the beatmap for additional filtering.
                + $"JOIN `{dbA}`.`{Beatmap.TABLE_NAME}` `b` "
                + "     ON `b`.`beatmap_id` = `s`.`beatmap_id` "
                + "WHERE `s`.`ruleset_id` = @RulesetId "
                + beatmapQuery
                + "     AND ABS(`a`.`pp` - `b`.`pp` > 0.1) "
                + $"    AND JSON_EXTRACT(`s`.`data`, \"$.mods[0]\") {modComparison}"
                + "ORDER BY `b`.`pp` - `a`.`pp` "
                + order
                + "LIMIT 10000", new
                {
                    RulesetId = rulesetId
                }, commandTimeout: int.MaxValue);

            Console.WriteLine($"Got PP diffs (mods: {withMods}, type: {type}), processing scores...");

            foreach (var d in diffs)
            {
                SoloScore scoreTask = await db.QuerySingleAsync<SoloScore>(
                    $"SELECT * FROM `{dbA}`.`{SoloScore.TABLE_NAME}` WHERE `id` = @ScoreId",
                    new
                    {
                        ScoreId = d.score_id
                    });

                Beatmap beatmapTask = await db.QuerySingleAsync<Beatmap>(
                    $"SELECT * FROM `{dbA}`.`{Beatmap.TABLE_NAME}` WHERE `beatmap_id` = @BeatmapId",
                    new
                    {
                        BeatmapId = d.beatmap_id
                    });

                scores.Add(new ProcessedScoreDiff(scoreTask, d, beatmapTask));
            }

            Console.WriteLine($"Finished processing scores (mods: {withMods}, type: {type}).");
        }

        return scores;
    }

    public async Task<List<ProcessedBeatmapDiff>> QuerySrDiffs()
    {
        List<ProcessedBeatmapDiff> beatmaps = new List<ProcessedBeatmapDiff>(max_rows);

        using (var db = await Database.GetConnection())
        {
            Console.WriteLine($"Querying SR diffs (mods: {withMods}, type: {type})...");

            StringBuilder beatmapQuery = new StringBuilder();
            if (!converts)
                beatmapQuery.AppendLine("AND `m`.`playmode` = @RulesetId ");
            if (rankedOnly)
                beatmapQuery.AppendLine("AND `m`.`approved` IN (1, 2) ");

            string modComparison = withMods ? ">= 0 " : "= 0 ";
            string order = type == DiffType.Gains ? "DESC " : "ASC ";

            IEnumerable<BeatmapDiff> diffs = await db.QueryAsync<BeatmapDiff>(
                "SELECT "
                + $"     `a`.`beatmap_id` AS `{nameof(BeatmapDiff.beatmap_id)}`, "
                + $"     `a`.`mods` AS `{nameof(BeatmapDiff.mods)}`, "
                + $"     `a`.`diff_unified` AS `{nameof(BeatmapDiff.a_sr)}`, "
                + $"     `b`.`diff_unified` AS `{nameof(BeatmapDiff.b_sr)}` "
                // Select beatmap difficulties from each database...
                + $"FROM `{dbA}`.`{BeatmapDifficulty.TABLE_NAME}` `a` "
                + $"JOIN `{dbB}`.`{BeatmapDifficulty.TABLE_NAME}` `b` "
                + "     ON `a`.`beatmap_id` = `b`.`beatmap_id` "
                + "     AND `a`.`mods` = `b`.`mods` "
                + "     AND `a`.`mode` = `b`.`mode` "
                // And the beatmap for additional filtering.
                + $"JOIN `{dbA}`.`{Beatmap.TABLE_NAME}` `m` "
                + "     ON `m`.`beatmap_id` = `a`.`beatmap_id` "
                + "WHERE `a`.`mode` = @RulesetId "
                + beatmapQuery
                + "     AND ABS(`a`.`diff_unified` - `b`.`diff_unified` > 0.1) "
                + $"    AND `a`.`mods` {modComparison}"
                + "ORDER BY `b`.`diff_unified` - `a`.`diff_unified` "
                + order
                + "LIMIT 10000", new
                {
                    RulesetId = rulesetId
                }, commandTimeout: int.MaxValue);

            Console.WriteLine($"Got SR diffs (mods: {withMods}, type: {type}), processing beatmaps...");

            foreach (var d in diffs)
            {
                Beatmap beatmapTask = await db.QuerySingleAsync<Beatmap>(
                    $"SELECT * FROM `{dbA}`.`{Beatmap.TABLE_NAME}` WHERE `beatmap_id` = @BeatmapId",
                    new
                    {
                        BeatmapId = d.beatmap_id
                    });

                beatmaps.Add(new ProcessedBeatmapDiff(d, beatmapTask));
            }

            Console.WriteLine($"Finished processing beatmaps (mods: {withMods}, type: {type}).");
        }

        return beatmaps;
    }
}
