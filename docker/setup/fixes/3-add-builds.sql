INSERT INTO `osu_builds` (build_id, allow_performance)
(
    SELECT DISTINCT(build_id), true FROM `scores` WHERE build_id IS NOT NULL AND pp IS NOT NULL
);
