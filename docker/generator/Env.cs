// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace Generator;

public static class Env
{
    public static readonly string RULESET;
    public static readonly int RULESET_ID;
    public static readonly string DB_A;
    public static readonly string DB_B;
    public static readonly bool NO_CONVERTS;
    public static readonly bool RANKED_ONLY;

    static Env()
    {
        RULESET = Environment.GetEnvironmentVariable("RULESET") ?? throw new InvalidOperationException("Missing RULESET environment variable.");
        RULESET_ID = int.Parse(Environment.GetEnvironmentVariable("RULESET_ID") ?? throw new InvalidOperationException("Missing RULESET_ID environment variable."));
        DB_A = Environment.GetEnvironmentVariable("OSU_A_HASH") ?? throw new InvalidOperationException("Missing OSU_A_HASH environment variable.");
        DB_B = Environment.GetEnvironmentVariable("OSU_B_HASH") ?? throw new InvalidOperationException("Missing OSU_B_HASH environment variable.");
        NO_CONVERTS = Environment.GetEnvironmentVariable("NO_CONVERTS") == "1";
        RANKED_ONLY = Environment.GetEnvironmentVariable("RANKED_ONLY") == "1";
    }
}
