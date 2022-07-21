#!/bin/bash

source ../common.sh

### Runs the score statistics processor to recalculate all scores + totals.
###
### Usage: run_processor <db_name> <ss_dir>
function run_processor() {
    local db_name=$1
    local diffcalc_dir=$2

    if [[ $(get_db_step ${db_name}) -ge 2 ]]; then
        echo "SR calculations are up to date."
        return
    fi

    wait_for_step $db_name 1

    cd $diffcalc_dir
    ./UseLocalOsu.sh

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

    set_db_step $db_name 2
}


DIFFCALC_A_DIR="${WORKDIR_A}/osu-difficulty-calculator"
DIFFCALC_B_DIR="${WORKDIR_B}/osu-difficulty-calculator"

echo "[DIFFCALC_A] => ${DIFFCALC_A_DIR}"
echo "[DIFFCALC_B] => ${DIFFCALC_B_DIR}"

clone_repo "https://github.com/ppy/osu-difficulty-calculator" "${DIFFCALC_A_DIR}"
clone_repo "https://github.com/ppy/osu-difficulty-calculator" "${DIFFCALC_B_DIR}"

TIMEFORMAT="Completed in %3lR"

echo "Running ${DIFFCALC_A_DIR}..."
time run_processor "${OSU_A_HASH}" "${DIFFCALC_A_DIR}"

echo "Running ${DIFFCALC_B_DIR}..."
time run_processor "${OSU_B_HASH}" "${DIFFCALC_B_DIR}"