#!/bin/bash

: ${DB_HOST:="db"}
: ${DB_PORT:=3306}
: ${DB_USER:="root"}

export DB_HOST
export DB_PORT
export DB_USER

MYSQL_ARGS=(
    -sN
    --host="${DB_HOST}"
    --port="${DB_PORT}"
    --user="${DB_USER}"
)

WORKDIR_A=""
WORKDIR_B=""

OSU_A_DIR=""
OSU_A_HASH=""
OSU_B_DIR=""
OSU_B_HASH=""

RULESET_ID=0
HIGH_SCORES_SUFFIX=""

NC='\033[0m'
YELLOW='\033[0;33m'
RED='\033[0;31m'

### Clones a given repository URL into a target directory.
### The URL may link to a pull request, commit, or tree.
###
### Usage: clone_repo <url> <target_dir>
function clone_repo() {
    local url=$1
    local target_dir=$2
    local re="^(https:\/\/github\.com\/[^\/]+\/[^\/]+)\/?([^\/]+)?\/?([^\/]+)?$"

    echo "Cloning ${url} => ${target_dir}"

    if [[ $url =~ $re ]]; then
        local repo=${BASH_REMATCH[1]}
        local target=${BASH_REMATCH[2]}
        local target_info=${BASH_REMATCH[3]}

        git clone $repo $target_dir
        pushd $target_dir > /dev/null

        case $target in
            pull)
                gh pr checkout $target_info
                ;;
            commit | tree)
                git checkout $target_info
                ;;
            "")
                ;;
            *)
                echo -e "${RED}Invalid repository target: $target${NC}"
                exit 1
        esac

        popd > /dev/null
    else
        echo -e "${RED}Not a valid repository url: $url${NC}"
        exit 1
    fi
}

### Retrieves a unique hash for the given directory.
###
### Remarks: This is implemented via git rev-parse.
###
### Usage: get_hash <target_dir>
function get_hash() {
    local target_dir=$1

    pushd $target_dir > /dev/null
    echo $(git rev-parse HEAD)
    popd > /dev/null
}

### Retrieves the latest processed step in the database.
###
### -1 => Not initialised.
###  0 => DB imported.
###  1 => Highscores imported
###  2 => Difficulty calculation complete
###  3 => Score PP calculation complete
###  4 => User PP calculation complete
###
### Usage: get_db_step <db_name>
function get_db_step() {
    local db_name=$1

    local count=$(mysql "${MYSQL_ARGS[@]}" -e \
        "SELECT \`count\` FROM \`osu_counts\` WHERE name = '${RULESET}_db_step'" \
        "$db_name" \
        2>/dev/null)

    if [[ -z "$count" ]]; then
        echo "-1"
    else
        echo "$count"
    fi
}

### Sets the latest processed step in the database.
###
### Usage: set_db_step <db_name> <num>
function set_db_step() {
    local db_name=$1
    local num=$2

    mysql "${MYSQL_ARGS[@]}" -e \
        "INSERT INTO \`osu_counts\` (\`name\`, \`count\`) VALUES ('${RULESET}_db_step', $num) ON DUPLICATE KEY UPDATE \`count\` = $num" \
        "$db_name"
}

### Waits for the database to reach a given step.
###
### See: get_db_step()
###
### Usage: wait_for_step <db_name> <num>
function wait_for_step() {
    local db_name=$1
    local num=$2

    while [[ $(get_db_step ${db_name}) != ${num} ]]; do
        echo "Waiting for database ${db_name} to reach step ${num}. Currently at step $(get_db_step ${db_name})."
        sleep 5
    done
}

# Checkout + setup hashes/dirs/etc.

WORKDIR_A=$(mktemp -d)
WORKDIR_B=$(mktemp -d)

echo "[WORKDIR_A] => ${WORKDIR_A}"
echo "[WORKDIR_B] => ${WORKDIR_B}"

OSU_A_DIR="${WORKDIR_A}/osu"
OSU_B_DIR="${WORKDIR_B}/osu"

echo "[OSU_A] => ${OSU_A_DIR}"
echo "[OSU_B] => ${OSU_B_DIR}"

clone_repo "$OSU_A" "$OSU_A_DIR"
clone_repo "$OSU_B" "$OSU_B_DIR"

OSU_A_HASH=$(get_hash $OSU_A_DIR)
OSU_B_HASH=$(get_hash $OSU_B_DIR)

echo "[HASH_A] => ${OSU_A_HASH}"
echo "[HASH_B] => ${OSU_B_HASH}"

export OSU_A_HASH
export OSU_B_HASH
export NO_CONVERTS
export RANKED_ONLY