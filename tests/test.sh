#!/usr/bin/env bash
set -euo pipefail

DIR="$(dirname "$(readlink -f "$0")")"

# Download L_lib.sh library
if hash L_lib.sh 2>/dev/null; then
	echo "Using L_lib.sh from PATH"
	. L_lib.sh -s
else
	echo "Downloading L_lib.sh"
	curl -sS -o "$DIR"/L_lib.sh -z "$DIR"/L_lib.sh https://raw.githubusercontent.com/Kamilcuk/L_lib/refs/heads/v1/bin/L_lib.sh
	. "$DIR"/L_lib.sh -s
fi

# Parse command line arguments
L_argparse dest_prefix=opt_ \
	-- -r help="Filter tests with this regex" \
	-- exe nargs=REMAINDER help="Path to the command to test" \
	---- "$@"

if [[ -z "${opt_exe:-}" ]]; then
	if L_hash make; then
		L_logrun make -C "$DIR"/.. build
	else
		L_logrun dotnet build "$DIR"/../src/KCKSeFCli
	fi
	opt_exe=("$(readlink -f "$DIR"/../cli)")
fi

cli() {
	L_logrun "${opt_exe[@]}" "$@"
}

. "$DIR"/unit.sh
. "$DIR"/integration.sh

L_unittest_main -P clitest_ ${opt_r:+-r"$opt_r"}
