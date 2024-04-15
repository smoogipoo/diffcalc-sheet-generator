#!/bin/bash

source ../common.sh

### Checks that there's enough space for the database.
###
### Usage: ensure_space_available
function ensure_space_available() {
    if [[ ${MAX_DATABASE_SIZE} -lt 0 ]]; then
        return
    fi

    if [[ $(get_db_size) -lt ${MAX_DATABASE_SIZE} ]]; then
        return
    fi

    echo "Database has exceeded the maximum allowable size (db: $(get_db_size)GB, max: ${MAX_DATABASE_SIZE}GB). Purging oldest databases..."

    while [[ $(get_db_size) -gt ${MAX_DATABASE_SIZE} ]]; do
        local oldest=$(mysql "${MYSQL_ARGS[@]}" -e "SELECT TABLE_SCHEMA FROM information_schema.TABLES WHERE TABLE_SCHEMA NOT IN ('mysql', 'information_schema', 'performance_schema', 'sys', '${OSU_A_HASH}', '${OSU_B_HASH}') ORDER BY CREATE_TIME LIMIT 1");

        if [[ -z "${oldest}" ]]; then
            echo "No more databases left to drop. Database size will exceed the maximum allowable size. Increase MAX_DATABASE_SIZE to prevent this warning."
            return
        fi

        echo "Purging database '${oldest}'..."
        echo "DROP DATABASE ${oldest}" | mysql "${MYSQL_ARGS[@]}"
    done
}

function truncate_sql() {
    local file="/sql/${RULESET}/$1.sql"

    if [ ! -e "$file" ]; then
        return
    fi

    # Indices of LOCK TABLES/UNLOCK TABLES lines.
    lines=($(grep -n "LOCK" "${file}" | awk -F: '{ print $1 }'))

    # Up to the first LOCK TABLES line
    result=$(cat "${file}" | head -n ${lines[0]})

    result+="
    "

    # From the UNLOCK TABLES line onwards
    result+=$(cat "${file}" | tail -n +${lines[1]})

    echo "${result}" > "${file}"
}

### Creates a database and ensures it's populated for the given ruleset.
###
### Usage: setup_database <db_name>
function setup_database() {
    local db_name=$1

    ensure_space_available

    mysql "${MYSQL_ARGS[@]}" -e "CREATE DATABASE IF NOT EXISTS \`${db_name}\`"

    if [[ $(get_db_step ${db_name}) -ge 0 ]]; then
        echo "${db_name} is up to date!"
        return
    fi

    wait_for_step $db_name -1

    echo "Importing data in ${db_name}. This will take a while..."
    (pv --force -p $(find "/sql/${RULESET}" -type f -name "*.sql") | mysql "${MYSQL_ARGS[@]}" --database="$db_name") 2>&1 | stdbuf -o0 tr '\r' '\n'

    echo "Applying fixes..."
    (pv --force -p $(find "/app/setup/fixes" -type f -name "*.sql" | sort -n) | mysql "${MYSQL_ARGS[@]}" --database="$db_name") 2>&1 | stdbuf -o0 tr '\r' '\n'

    set_db_step $db_name 0
}

echo "Preparing files..."
truncate_sql "osu_beatmap_difficulty_attribs"
truncate_sql "osu_beatmap_difficulty"
truncate_sql "osu_beatmap_failtimes"
truncate_sql "osu_scores_high"
truncate_sql "osu_scores_taiko_high"
truncate_sql "osu_scores_fruits_high"
truncate_sql "osu_scores_mania_high"
truncate_sql "osu_user_beatmap_playcount"

echo "Creating databases..."

setup_database "${OSU_A_HASH}"
setup_database "${OSU_B_HASH}"
