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
        --threads 16

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
    dotnet run \
        -c:Release \
        --project osu.Server.Queues.ScorePump \
        -- \
        batch \
        user-totals \
        all \
        --ruleset ${RULESET_ID} \
        --threads 16

    set_db_step $db_name 4
}

SS_A_DIR="${WORKDIR_A}/osu-queue-score-statistics"
SS_B_DIR="${WORKDIR_B}/osu-queue-score-statistics"

echo "[SS_A] => ${SS_A_DIR}"
echo "[SS_B] => ${SS_B_DIR}"

clone_repo "https://github.com/ppy/osu-queue-score-statistics" "${SS_A_DIR}"
cd "${SS_A_DIR}"
./UseLocalOsu.sh

clone_repo "https://github.com/ppy/osu-queue-score-statistics" "${SS_B_DIR}"
cd "${SS_B_DIR}"
./UseLocalOsu.sh

process_scores "${OSU_A_HASH}" "${SS_A_DIR}"
process_totals "${OSU_A_HASH}" "${SS_A_DIR}"
process_scores "${OSU_B_HASH}" "${SS_B_DIR}"
process_totals "${OSU_B_HASH}" "${SS_B_DIR}"
