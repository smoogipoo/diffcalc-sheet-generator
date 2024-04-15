INSERT IGNORE INTO `osu_difficulty_attribs` (attrib_id, name)
(
    SELECT i, 'Attrib ID ' + i FROM
    (
        WITH RECURSIVE seq AS (
            SELECT 1 AS i UNION ALL SELECT i + 2 FROM seq WHERE i <= 1000
        ) SELECT i FROM seq
    ) gen
);
