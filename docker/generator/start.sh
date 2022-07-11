#!/bin/bash

source ../common.sh

wait_for_step "${OSU_A_HASH}" 4
wait_for_step "${OSU_B_HASH}" 4

dotnet run -c:Release