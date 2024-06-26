#!/bin/bash

source ../common.sh

### Runs the score statistics processor to recalculate all scores.
###
### Usage: process_scores <db_name> <processor_dir>
function process_scores() {
    local db_name=$1
    local processor_dir=$2

    wait_for_step "${db_name}" "${STEP_PPCALC_SCORE}" \
        || { echo "Score PP values are up to date."; return; }

    cd $processor_dir
    ./UseLocalOsu.sh

    DB_NAME=$db_name \
    REALTIME_DIFFICULTY="0" \
    dotnet run \
        -c:Release \
        --project osu.Server.Queues.ScoreStatisticsProcessor \
        -- \
        performance \
        scores \
        all \
        --ruleset ${RULESET_ID} \
        --threads ${THREADS}

    next_step "${db_name}"
}

### Runs the score statistics processor to recalculate all user totals.
###
### Usage: process_totals <db_name> <processor_dir>
function process_totals() {
    local db_name=$1
    local processor_dir=$2

    wait_for_step "${db_name}" "${STEP_PPCALC_USER}" \
        || { echo "User PP values are up to date."; return; }

    cd $processor_dir
    ./UseLocalOsu.sh

    DB_NAME=$db_name \
    PROCESS_USER_TOTALS="1" \
    REALTIME_DIFFICULTY="0" \
    dotnet run \
        -c:Release \
        --project osu.Server.Queues.ScoreStatisticsProcessor \
        -- \
        performance \
        user-totals \
        all \
        --ruleset ${RULESET_ID} \
        --threads ${THREADS}

    next_step "${db_name}"
}

PROCESSOR_A_DIR="${WORKDIR_A}/osu-queue-score-statistics"
PROCESSOR_B_DIR="${WORKDIR_B}/osu-queue-score-statistics"

echo "[PROCESSOR_A] => ${PROCESSOR_A_DIR}"
echo "[PROCESSOR_B] => ${PROCESSOR_B_DIR}"

clone_repo "${SCORE_PROCESSOR_A}" "${PROCESSOR_A_DIR}"
clone_repo "${SCORE_PROCESSOR_B}" "${PROCESSOR_B_DIR}"

process_scores "${OSU_A_HASH}" "${PROCESSOR_A_DIR}"
process_totals "${OSU_A_HASH}" "${PROCESSOR_A_DIR}"
process_scores "${OSU_B_HASH}" "${PROCESSOR_B_DIR}"
process_totals "${OSU_B_HASH}" "${PROCESSOR_B_DIR}"
