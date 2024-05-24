#!/bin/bash

source ../common.sh

wait_for_step "${OSU_A_HASH}" "${STEP_GENERATE}"
wait_for_step "${OSU_B_HASH}" "${STEP_GENERATE}"

# See Dockerfile
/out/Generator
