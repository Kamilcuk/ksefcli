#!/bin/bash
set -xeuo pipefail
./tests/integration.sh -r ' ! clitest_z_integration_PobierzFaktury_prod ' "$@"
