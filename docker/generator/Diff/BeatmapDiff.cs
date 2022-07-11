// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Diagnostics.CodeAnalysis;

namespace Generator.Diff;

[SuppressMessage("ReSharper", "InconsistentNaming")]
[Serializable]
public class BeatmapDiff
{
    public uint beatmap_id;
    public int mods;
    public float a_sr;
    public float b_sr;
}
