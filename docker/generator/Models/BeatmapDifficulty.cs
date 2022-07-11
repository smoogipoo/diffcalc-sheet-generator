// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Diagnostics.CodeAnalysis;
using Dapper.Contrib.Extensions;

namespace Generator.Models;

[SuppressMessage("ReSharper", "InconsistentNaming")]
[Serializable]
[Table(TABLE_NAME)]
public class BeatmapDifficulty
{
    public const string TABLE_NAME = "osu_beatmap_difficulty";
}
