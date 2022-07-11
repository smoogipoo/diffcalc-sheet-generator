CREATE TABLE `solo_scores` (
  `id` bigint unsigned NOT NULL AUTO_INCREMENT,
  `user_id` int unsigned NOT NULL,
  `beatmap_id` mediumint unsigned NOT NULL,
  `ruleset_id` smallint unsigned NOT NULL,
  `data` json NOT NULL,
  `preserve` tinyint(1) NOT NULL DEFAULT '0',
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL,
  `deleted_at` timestamp NULL DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `solo_scores_preserve_index` (`preserve`),
  KEY `solo_scores_user_id_index` (`user_id`)
) ENGINE=InnoDB AUTO_INCREMENT=78380399 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE `solo_scores_legacy_id_map` (
  `ruleset_id` smallint unsigned NOT NULL,
  `old_score_id` int unsigned NOT NULL,
  `score_id` bigint NOT NULL,
  PRIMARY KEY (`ruleset_id`,`old_score_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `solo_scores_performance` (
  `score_id` bigint unsigned NOT NULL,
  `pp` double(8,2) DEFAULT NULL,
  PRIMARY KEY (`score_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE `solo_scores_process_history` (
  `score_id` bigint NOT NULL,
  `processed_version` tinyint NOT NULL,
  `processed_at` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`score_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE `score_process_queue` (
  `queue_id` int unsigned NOT NULL AUTO_INCREMENT,
  `score_id` bigint unsigned NOT NULL,
  `mode` tinyint unsigned NOT NULL,
  `start_time` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `update_time` timestamp NULL DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,
  `status` tinyint(1) NOT NULL DEFAULT '0',
  PRIMARY KEY (`queue_id`,`start_time`),
  KEY `status` (`status`),
  KEY `lookup_v3` (`mode`,`status`,`queue_id`),
  KEY `temp_pp_processor` (`score_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE `osu_builds` (
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