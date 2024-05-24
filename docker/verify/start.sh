#!/bin/bash

source ../common.sh

### Runs the score statistics processor to verify imported scores.
###
### Usage: verify_scores <db_name> <processor_dir>
function verify_scores() {
    local db_name=$1
    local processor_dir=$2

    wait_for_step "${db_name}" "${STEP_VERIFY}" \
        || { echo "Scores have already been verified."; return; }

    cd $processor_dir
    ./UseLocalOsu.sh

    DB_NAME=$db_name \
    dotnet run \
        -c:Release \
        --project osu.Server.Queues.ScoreStatisticsProcessor \
        -- \
        maintenance \
        verify-imported-scores \
        --ruleset-id ${RULESET_ID}

    next_step "${db_name}"
}

PROCESSOR_A_DIR="${WORKDIR_A}/osu-queue-score-statistics"
PROCESSOR_B_DIR="${WORKDIR_B}/osu-queue-score-statistics"

echo "[PROCESSOR_A] => ${PROCESSOR_A_DIR}"
echo "[PROCESSOR_B] => ${PROCESSOR_B_DIR}"

clone_repo "${SCORE_PROCESSOR_A}" "${PROCESSOR_A_DIR}"
clone_repo "${SCORE_PROCESSOR_B}" "${PROCESSOR_B_DIR}"

verify_scores "${OSU_A_HASH}" "${PROCESSOR_A_DIR}"
verify_scores "${OSU_B_HASH}" "${PROCESSOR_B_DIR}"
