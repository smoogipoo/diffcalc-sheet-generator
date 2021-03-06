#!/bin/bash

source ../common.sh

### Runs the score statistics processor to recalculate all scores.
###
### Usage: process_scores <db_name> <ss_dir>
function process_scores() {
    local db_name=$1
    local ss_dir=$2

    if [[ $(get_db_step ${db_name}) -ge 3 ]]; then
        echo "PP calculations are up to date."
        return
    fi

    wait_for_step $db_name 2

    cd $ss_dir
    ./UseLocalOsu.sh

    DB_NAME=$db_name \
    dotnet run \
        -c:Release \
        --project osu.Server.Queues.ScorePump \
        batch \
        scores \
        all \
        --ruleset ${RULESET_ID} \
        --threads ${THREADS}

    set_db_step $db_name 3
}

### Runs the score statistics processor to recalculate all user totals.
###
### Usage: process_totals <db_name> <ss_dir>
function process_totals() {
    local db_name=$1
    local ss_dir=$2

    if [[ $(get_db_step ${db_name}) -ge 4 ]]; then
        echo "PP calculations are up to date."
        return
    fi

    wait_for_step $db_name 3

    cd $ss_dir
    ./UseLocalOsu.sh

    DB_NAME=$db_name \
    PROCESS_USER_TOTALS="1" \
    dotnet run \
        -c:Release \
        --project osu.Server.Queues.ScorePump \
        -- \
        batch \
        user-totals \
        all \
        --ruleset ${RULESET_ID} \
        --threads ${THREADS}

    set_db_step $db_name 4
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