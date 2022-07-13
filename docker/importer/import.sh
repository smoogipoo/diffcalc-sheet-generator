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
    (pv --force -p $(find /sql -type f -name "*.sql") | mysql "${MYSQL_ARGS[@]}" --database="$db_name") 2>&1 | stdbuf -o0 tr '\r' '\n'

    echo "Applying fixes..."
    (pv --force -p $(find /app/importer/fixes -type f -name "*.sql") | mysql "${MYSQL_ARGS[@]}" --database="$db_name") 2>&1 | stdbuf -o0 tr '\r' '\n'

    set_db_step $db_name 0
}

### Imports highscores in a database.
###
### Usage: import_scores <db_name>
function import_scores() {
    local db_name=$1

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

    clone_repo "https://github.com/ppy/osu-queue-score-statistics" "/osu-queue-score-statistics"

    DB_NAME=$db_name \
    dotnet run \
        --project /osu-queue-score-statistics/osu.Server.Queues.ScorePump \
        -- \
        queue \
        import-high-scores \
        -r ${RULESET_ID}

    set_db_step $db_name 1
}

echo "Creating databases..."

setup_database "${OSU_A_HASH}"
setup_database "${OSU_B_HASH}"

import_scores "${OSU_A_HASH}"
import_scores "${OSU_B_HASH}"

wait
