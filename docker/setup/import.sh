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
    (pv --force -p $(find "/app/setup/fixes" -type f -name "*.sql") | mysql "${MYSQL_ARGS[@]}" --database="$db_name") 2>&1 | stdbuf -o0 tr '\r' '\n'

    for i in {1..1000..2}; do
        mysql "${MYSQL_ARGS[@]}" --database="$db_name" -e "INSERT IGNORE INTO osu_difficulty_attribs (attrib_id, name) VALUES (${i}, 'Attrib ID ${i}')"
    done

    set_db_step $db_name 0
}

echo "Creating databases..."

setup_database "${OSU_A_HASH}"
setup_database "${OSU_B_HASH}"