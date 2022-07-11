// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Generator.Models;

namespace Generator.Diff;

public record struct ProcessedScoreDiff(SoloScore Score, ScoreDiff Diff, Beatmap Beatmap);