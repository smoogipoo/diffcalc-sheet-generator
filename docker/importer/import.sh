#!/bin/bash

source ../common.sh

### Creates a database and ensures it's populated for the given ruleset.
###
### Usage: setup_database <db_name>
function setup_database() {
    local db_name=$1

    mysql "${MYSQL_ARGS[@]}" -e "CREATE DATABASE IF NOT EXISTS \`${db_name}\`"

    if [[ $(get_db_step ${db_name}) -ge 0 ]]; then
        echo "${db_name} is up to date!"
        return
    fi

    wait_for_step $db_name -1

    echo "Importing data in ${db_name}. This will take a while..."
    (pv --force -p $(find "/sql/${RULESET}" -type f -name "*.sql") | mysql "${MYSQL_ARGS[@]}" --database="$db_name") 2>&1 | stdbuf -o0 tr '\r' '\n'

    echo "Applying fixes..."
    (pv --force -p $(find "/app/importer/fixes" -type f -name "*.sql") | mysql "${MYSQL_ARGS[@]}" --database="$db_name") 2>&1 | stdbuf -o0 tr '\r' '\n'

    echo "Adding difficulty attributes..."
    (pv --force -p "/sql/attributes.sql" | mysql "${MYSQL_ARGS[@]}" --database="$db_name") 2>&1 | stdbuf -o0 tr '\r' '\n'

    set_db_step $db_name 0
}

### Imports highscores in a database.
###
### Usage: import_scores <db_name> <workdir> <processor_repo>
function import_scores() {
    local db_name=$1
    local workdir=$2
    local processor_repo=$3

    if [[ $(get_db_step ${db_name}) -ge 1 ]]; then
        echo "High scores have already been imported."
        return
    fi

    wait_for_step $db_name 0

    echo "Importing high scores. This will take a while..."

    # Ensure we have a fresh slate to work with...
    mysql "${MYSQL_ARGS[@]}" \
        --database="$db_name" \
        --execute="TRUNCATE TABLE solo_scores; TRUNCATE TABLE solo_scores_legacy_id_map; TRUNCATE TABLE solo_scores_performance; TRUNCATE TABLE solo_scores_process_history;"

    local processor_dir="${workdir}/osu-queue-score-statistics"
    clone_repo "${processor_repo}" "${processor_dir}"
    cd "${processor_dir}"
    ./UseLocalOsu.sh

    DB_NAME=$db_name \
    dotnet run \
        -c:Release \
        --project osu.Server.Queues.ScoreStatisticsProcessor \
        -- \
        queue \
        import-high-scores \
        -r ${RULESET_ID} \
        --skip-indexing \
        --exit-on-completion

    set_db_step $db_name 1
}

echo "Creating databases..."

setup_database "${OSU_A_HASH}"
setup_database "${OSU_B_HASH}"

import_scores "${OSU_A_HASH}" "${WORKDIR_A}" "${SCORE_PROCESSOR_A}"
import_scores "${OSU_B_HASH}" "${WORKDIR_B}" "${SCORE_PROCESSOR_B}"