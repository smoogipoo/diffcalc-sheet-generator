// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Text;
using osu.Game.Online.API;

namespace Generator.Generators
{
    public interface IGenerator
    {
        const int MAX_ROWS = 10000;

        string Name { get; }

        ColumnDefinition[] Columns { get; }

        bool WithMods { get; }

        Task<object[][]> Query();

        static string GenerateBeatmapFilter(string selector)
        {
            StringBuilder builder = new StringBuilder();

            if (Env.NO_CONVERTS)
                add(builder, $"`{selector}`.`playmode` = @RulesetId");
            if (Env.RANKED_ONLY)
                add(builder, $"`{selector}`.`approved` IN (1, 2)");

            return builder.Length == 0 ? "(1)" : $"({builder})";

            static void add(StringBuilder builder, string query)
            {
                if (builder.Length > 0)
                    builder.Append(" AND ");
                builder.Append(query);
            }
        }
    }

    public static class GeneratorExtensions
    {
        /// <summary>
        /// Whether the given mods should be filtered out.
        /// </summary>
        public static bool ModsMatchFilter(this IGenerator generator, APIMod[] mods, ulong? legacyScoreId)
        {
            if (legacyScoreId != null)
                mods = mods.Where(m => m.Acronym != "CL").ToArray();

            if (!generator.WithMods && mods.Length > 0)
                return false;

            return Env.MOD_FILTERS.Length == 0
                   || Env.MOD_FILTERS.Any(f => f.Matches(mods));
        }

        /// <summary>
        /// Formats mods for the spreadsheet.
        /// </summary>
        public static string FormatMods(this IGenerator generator, IEnumerable<APIMod> mods)
            => mods.Any() ? string.Join(", ", mods.Select(m => m.Acronym.ToUpper())) : "NM";
    }
}
