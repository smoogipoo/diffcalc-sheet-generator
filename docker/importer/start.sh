#!/bin/bash

source ../common.sh

### Imports highscores in a database.
###
### Usage: import_scores <db_name> <workdir> <processor_repo>
function import_scores() {
    local db_name=$1
    local processor_dir=$2

    if [[ $(get_db_step ${db_name}) -ge 2 ]]; then
        echo "High scores have already been imported."
        return
    fi

    wait_for_step $db_name 1

    echo "Importing high scores. This will take a while..."

    # Ensure we have a fresh slate to work with...
    mysql "${MYSQL_ARGS[@]}" \
        --database="$db_name" \
        --execute="TRUNCATE TABLE solo_scores; TRUNCATE TABLE solo_scores_legacy_id_map; TRUNCATE TABLE solo_scores_performance; TRUNCATE TABLE solo_scores_process_history;"

    cd "${processor_dir}"
    ./UseLocalOsu.sh

    DB_NAME=$db_name \
    dotnet run \
        -c:Release \
        --project osu.Server.Queues.ScoreStatisticsProcessor \
        -- \
        queue \
        import-high-scores \
        --ruleset-id ${RULESET_ID} \
        --skip-indexing \
        --exit-on-completion

    set_db_step $db_name 2
}

PROCESSOR_A_DIR="${WORKDIR_A}/osu-queue-score-statistics"
PROCESSOR_B_DIR="${WORKDIR_B}/osu-queue-score-statistics"

echo "[PROCESSOR_A] => ${PROCESSOR_A_DIR}"
echo "[PROCESSOR_B] => ${PROCESSOR_B_DIR}"

clone_repo "${SCORE_PROCESSOR_A}" "${PROCESSOR_A_DIR}"
clone_repo "${SCORE_PROCESSOR_B}" "${PROCESSOR_B_DIR}"

import_scores "${OSU_A_HASH}" "${PROCESSOR_A_DIR}"
import_scores "${OSU_B_HASH}" "${PROCESSOR_B_DIR}"