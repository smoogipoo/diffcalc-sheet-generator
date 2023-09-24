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
    (pv --force -p $(find "/app/setup/fixes" -type f -name "*.sql") | mysql "${MYSQL_ARGS[@]}" --database="$db_name") 2>&1 | stdbuf -o0 tr '\r' '\n'

    for i in {1..1000..2}; do
        mysql "${MYSQL_ARGS[@]}" --database="$db_name" -e "INSERT IGNORE INTO osu_difficulty_attribs (attrib_id, name) VALUES (${i}, 'Attrib ID ${i}')"
    done

    set_db_step $db_name 0
}

echo "Creating databases..."

setup_database "${OSU_A_HASH}"
setup_database "${OSU_B_HASH}"