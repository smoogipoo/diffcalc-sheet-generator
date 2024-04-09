#!/bin/bash

source ../common.sh

wait_for_step "${OSU_A_HASH}" 3
wait_for_step "${OSU_B_HASH}" 3

# See Dockerfile
/out/Generator