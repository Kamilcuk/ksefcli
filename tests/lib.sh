#!/usr/bin/env bash
set -euo pipefail

if [[ -v testlib_sourced ]]; then
	return
fi
testlib_sourced=1

DIR="$(dirname "$(readlink -f "${BASH_SOURCE[0]}")")"
GITDIR=$(readlink -f "$(dirname "$(readlink -f "${BASH_SOURCE[0]}")")/..")

pull_L_lib() {
	if [[ ! -f "$DIR/L_lib.sh" ]]; then
		echo "Downloading L_lib.sh from https://github.com/Kamilcuk/L_lib/releases/download/v1.1.0/L_lib.sh with curl"
		curl -fsSL https://github.com/Kamilcuk/L_lib/releases/download/v1.1.0/L_lib.sh > "$DIR/L_lib.sh"
	fi
	# shellcheck source=tests/L_lib.sh
	. "$DIR/L_lib.sh" -s -L
}

testlib_main() {
	pull_L_lib

	local args=()
	local opt_exe=()
	local i
	local opt_k=()
	while (( $# )); do
		case "$1" in
		-k) opt_k=( -k "$2" ); shift 2 ;;
		*)
			if [[ "$1" == */* ]]; then
				opt_exe+=( "$1" )
			else
				args+=( "$1" )
			fi
			shift
			;;
		esac
	done
	args+=( "${opt_k[@]}" )

	if (( ! ${#opt_exe[@]} )); then
		if L_hash make; then
			L_logrun make -C "$DIR"/.. build
		else
			L_logrun dotnet build "$DIR"/../src/KCKSeFCli
		fi
		opt_exe=( "$DIR/../src/KCKSeFCli/bin/Debug/net10.0/linux-x64/kcksefcli" )
		if [[ ! -f "${opt_exe[0]}" ]]; then
			opt_exe=( "$DIR/../src/KCKSeFCli/bin/Debug/net6.0/linux-x64/kcksefcli" )
		fi
	fi
	opt_exe=$(readlink -f "${opt_exe[0]}") || exit 234

	local cmd=( L_unittest_main -p clitest_ "${args[@]}" )

	# Create a global temporary directory.
	L_with_tmpdir_to TMPD
	export TMPD

	"${cmd[@]}"
}

testlib_setup_integration_config() {
	pull_L_lib
	if [[ -z "${KCKSEFCLI_CONFIG:-}" ]]; then
		local i
		for i in \
			"$GITDIR/.git/KSEF/kcksefcli.yaml" \
			"$GITDIR/.git/kcksefcli.yaml" \
			"$HOME/.config/kcksefcli/kcksefcli.yaml" \
			"$HOME/.config/kcksefcli.yaml" \
			; do
			if [[ -f "$i" ]]; then
				KCKSEFCLI_CONFIG="$i"
				export KCKSEFCLI_CONFIG
				break
			fi
		done
	fi
	L_assert "Could not find KCKSEFCLI for integration tests. Integration tests have to execute by a human" \
		test -n "${KCKSEFCLI_CONFIG:-}"
	L_log "Using KCKSEFCLI_CONFIG=$KCKSEFCLI_CONFIG for integration tests"
	return 0
}
