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
            if (Env.NO_ASPIRE)
                add(builder, $"`{selector}`.`id` NOT IN {GetAspireBlacklistDatabaseArray()}");
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

        private static string GetAspireBlacklistDatabaseArray()
        {
            // technically there are some non-aspire maps here but the idea is still the same
            int[] aspireMapBlacklist =
            [
                1258033, // (MinG3012) The Solace of Oblivion
                1257904, // (ProfessionalBox) The Solace of Oblivion
                1529760, // (Rucker) Der Wald [NiNo's Aspire]
                1529757, // (Rucker) Der Wald [Hey jia]
                1408449, // (fanzhen0019) GHOST
                2055234, // (DTM9 Nowa) Acid Rain
                2087153, // (seselis1) Acid Rain
                2571609, // (Meow Mix) Space Invaders
                2571016, // (ScubDomino) cherry blossoms explode across the dying horizon [wabi � sabi]
                2573886, // (ScubDomino) cherry blossoms explode across the dying horizon [�debug]
                2571139, // (ShotgunApe) THE SKIES OPEN [SHADOW OF TOMORROW]
                2572147, // (ShotgunApe) THE SKIES OPEN [Debug]
                2571858, // (mantasu) Unshakable
                2628991, // (fanzhen0019) XNOR XNOR XNOR [Moon]
                2573161, // (fanzhen0019) XNOR XNOR XNOR [Earth (atm)]
                2619200, // (fanzhen0019) XNOR XNOR XNOR [Earth]
                2571051, // (fanzhen0019) XNOR XNOR XNOR [.-- .-. --- -. --. .-- .- -.--]
                2573164, // (fanzhen0019) XNOR XNOR XNOR [Beloved Exclusive]
                1029976, // (MinG3012) Grenade
                2536330, // (Acylica) Flashbacklog
                1314546, // (rustbell) HYENA [:thinking:]
                1314545, // (rustbell) HYENA [caonimenquanbu]
                4553451, // (pw384) Nhelv [Distortion fix (no autofail debug)]
                4679228 // (pw384) Nhelv [KATASTROPHE]
            ];

            return $"({string.Join(',', aspireMapBlacklist)})";
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
