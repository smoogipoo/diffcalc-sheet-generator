ALTER INSTANCE DISABLE INNODB REDO_LOG;

CREATE TABLE IF NOT EXISTS `score_process_history` (
  `score_id` bigint NOT NULL,
  `processed_version` tinyint NOT NULL,
  `processed_at` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`score_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS `score_process_queue` (
  `queue_id` int unsigned NOT NULL AUTO_INCREMENT,
  `score_id` bigint unsigned NOT NULL,
  `mode` tinyint unsigned NOT NULL,
  `start_time` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `update_time` timestamp NULL DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,
  `status` tinyint(1) NOT NULL DEFAULT '0',
  `is_deletion` tinyint(1) NOT NULL DEFAULT '0',
  PRIMARY KEY (`queue_id`,`start_time`),
  KEY `status` (`status`),
  KEY `lookup_v3` (`mode`,`status`,`queue_id`),
  KEY `temp_pp_processor` (`score_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS `osu_builds` (
  `build_id` mediumint unsigned NOT NULL AUTO_INCREMENT,
  `version` varchar(40) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `hash` binary(16) DEFAULT NULL,
  `last_hash` binary(16) DEFAULT NULL,
  `date` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `allow_ranking` tinyint(1) NOT NULL DEFAULT '1',
  `allow_bancho` tinyint(1) NOT NULL DEFAULT '1',
  `allow_performance` tinyint(1) NOT NULL DEFAULT '0',
  `test_build` tinyint(1) NOT NULL DEFAULT '0',
  `users` mediumint unsigned NOT NULL DEFAULT '0',
  `stream_id` tinyint unsigned DEFAULT NULL,
  PRIMARY KEY (`build_id`),
  UNIQUE KEY `stream_id` (`stream_id`,`version`),
  KEY `hash` (`hash`),
  KEY `version` (`version`),
  KEY `allow_bancho` (`allow_bancho`),
  KEY `osu_builds_stream_id_index` (`stream_id`),
  KEY `osu_builds_allow_performance_index` (`allow_performance`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS `osu_user_month_playcount` (
  `user_id` int unsigned NOT NULL DEFAULT '0',
  `year_month` char(4) NOT NULL,
  `playcount` smallint unsigned NOT NULL,
  PRIMARY KEY (`user_id`,`year_month`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb3 ROW_FORMAT=COMPRESSED;

CREATE TABLE IF NOT EXISTS `osu_beatmap_scoring_attribs` (
  `beatmap_id` mediumint unsigned NOT NULL,
  `mode` tinyint unsigned NOT NULL,
  `legacy_accuracy_score` int NOT NULL DEFAULT 0,
  `legacy_combo_score` bigint NOT NULL DEFAULT 0,
  `legacy_bonus_score_ratio` float NOT NULL DEFAULT 0,
  `legacy_bonus_score` int NOT NULL DEFAULT 0,
  `max_combo` int NOT NULL DEFAULT 0,
  PRIMARY KEY (`beatmap_id`, `mode`)
);

CREATE TABLE `score_pins` (
  `user_id` int unsigned NOT NULL,
  `score_id` bigint unsigned NOT NULL,
  `ruleset_id` smallint unsigned NOT NULL,
  `display_order` double(8,2) NOT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL,
  PRIMARY KEY (`score_id`),
  KEY `user_id` (`user_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

DELETE FROM `osu_counts` WHERE `name` = 'slave_latency';

-- May be temporarily required as components are updated to the new table terminology. See: https://github.com/ppy/osu-infrastructure/issues/24
CREATE VIEW solo_scores AS SELECT * FROM scores;
CREATE VIEW solo_scores_process_history AS SELECT * FROM score_process_history;
