// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Text;

namespace Generator.Generators
{
    public interface IGenerator
    {
        string Name { get; }

        ColumnDefinition[] Columns { get; }

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
}
