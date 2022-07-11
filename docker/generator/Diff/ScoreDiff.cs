// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Diagnostics.CodeAnalysis;

namespace Generator.Diff;

[SuppressMessage("ReSharper", "InconsistentNaming")]
[Serializable]
public struct ScoreDiff
{
    public ulong highscore_id;
    public ulong score_id;
    public uint beatmap_id;
    public float a_pp;
    public float b_pp;
}
