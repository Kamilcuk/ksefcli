#!/usr/bin/env bash
set -euo pipefail
DIR="$(dirname "$(readlink -f "${BASH_SOURCE[0]}")")"
. "$DIR"/lib.sh
. "$DIR"/unit.sh
. "$DIR"/cmdauth.sh
. "$DIR"/integration.sh
testlib_main "$@"
