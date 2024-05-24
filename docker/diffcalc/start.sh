#!/bin/bash

source ../common.sh

### Runs the score statistics processor to recalculate all scores + totals.
###
### Usage: run_processor <db_name> <ss_dir>
function run_processor() {
    local db_name=$1
    local diffcalc_dir=$2

    wait_for_step "${db_name}" "${STEP_DIFFCALC}" \
        || { echo "SR values are up to date"; return; }

    cd $diffcalc_dir
    ./UseLocalOsu.sh

    echo "Running ${diffcalc_dir}..."

    time {
        DB_NAME=$db_name \
        BEATMAPS_PATH="/beatmaps" \
        ALLOW_DOWNLOAD=1 \
        SAVE_DOWNLOADED=1 \
        dotnet run \
            -c:Release \
            --project osu.Server.DifficultyCalculator \
            -- \
            all \
            --mode ${RULESET_ID} \
            --allow-converts \
            --concurrency ${THREADS}
    }

    next_step "${db_name}"
}


DIFFCALC_A_DIR="${WORKDIR_A}/osu-difficulty-calculator"
DIFFCALC_B_DIR="${WORKDIR_B}/osu-difficulty-calculator"

echo "[DIFFCALC_A] => ${DIFFCALC_A_DIR}"
echo "[DIFFCALC_B] => ${DIFFCALC_B_DIR}"

clone_repo "${DIFFICULTY_CALCULATOR_A}" "${DIFFCALC_A_DIR}"
clone_repo "${DIFFICULTY_CALCULATOR_B}" "${DIFFCALC_B_DIR}"

TIMEFORMAT="Completed in %3lR"

run_processor "${OSU_A_HASH}" "${DIFFCALC_A_DIR}"
run_processor "${OSU_B_HASH}" "${DIFFCALC_B_DIR}"
