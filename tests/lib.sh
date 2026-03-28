#!/usr/bin/env bash
set -euo pipefail

if [[ -v testlib_sourced ]]; then
	return
fi
testlib_sourced=1

DIR="$(dirname "$(readlink -f "${BASH_SOURCE[0]}")")"
GITDIR=$(readlink -f "$(dirname "$(readlink -f "${BASH_SOURCE[0]}")")"/..)

cli() {
	L_logrun "${opt_exe[@]}" "$@"
}

fatal() {
	echo "$@" >&2
	exit 123
}

pull_L_lib() {
	local url=https://github.com/Kamilcuk/L_lib/releases/download/v1.1.0/L_lib.sh
	local cachef="$DIR"/L_lib.sh
	if [[ ! -v L_LIB_VERSION ]]; then
		# Download L_lib.sh library
		if [[ -s "$cachef" ]]; then
			echo "Using preexisting $DIR/L_lib.sh"
		elif hash L_lib.sh 2>/dev/null; then
			echo "Using L_lib.sh from PATH"
			. L_lib.sh -s
			return
		elif hash curl 2>/dev/null; then
			echo "Downloading L_lib.sh from $url with curl"
			local args=()
			if [[ -z "$cachef" ]]; then
				args+=(-z "$cachef")
			fi
			curl -sSL -o "$cachef" "${args[@]}" "$url"
		elif hash wget 2>/dev/null; then
			echo "Downloading L_lib.sh from $url with wget"
			wget -O "$cachef" "$url"
		else
			fatal "Could not download or find L_lib.sh"
		fi
		if [[ -s "$cachef" ]]; then
			. "$cachef" -s
		else
			fatal "Downloading L_lib.sh has failed"
		fi
	fi
}

testlib_main() {
	pull_L_lib

	# Disable core dumps
	ulimit -c 0

	local args=()
	# Parse command line arguments
	L_argparse dest_prefix=opt_ \
		-- -r help="Filter tests with this regex" nargs=1 eval='args+=(-k "$1")' \
		-- -k help="Filter tests with this regex" nargs=1 eval='args+=(-k "$1")' \
		-- -l nargs=0 eval='args+=(-l)' \
		-- -s nargs=0 eval='args+=(-s)' \
		-- exe nargs=remainder help="Path to the command to test" \
		---- "$@"

	if [[ -z "${opt_exe:-}" ]]; then
		if L_hash make; then
			L_logrun make -C "$DIR"/.. build
		else
			L_logrun dotnet build "$DIR"/../src/KCKSeFCli
		fi
		opt_exe=("$(readlink -f "$DIR"/../cli)")
	fi

	if [[ "$(type "${opt_exe[0]}")" == *"function"* ]]; then
		L_fatal "First argument is the executabl to test. Use -r <regex> to filter tests to execute"
	fi
	opt_exe=$(readlink -f "${opt_exe[0]}") || exit 234

	local cmd=( L_unittest_main -p clitest_ "${args[@]}" )

	# Create a global temporary directory.
	L_with_tmpdir_to TMPD
	export TMPD

	if [[ -v KCLLM ]]; then
		# When running from GEMINI, we do not need to print everything all at once every line.
		# Just give GEMINI enouhg context to work with.
		tmp=$( "${cmd[@]}" 2>&1 )
		tail -n 100 <<<"$tmp"
	else
		"${cmd[@]}"
	fi
}

testlib_setup_integration_config() {
	pull_L_lib
	if [[ -v KCLLM ]]; then
		L_fatal "Integration tests have to executed by a human"
	fi
	if [[ -z "${KCKSEFCLI_CONFIG:-}" ]]; then
		local i
		for i in \
			"$GITDIR/.git/KSEF/kcksefcli.yaml" \
			"$GITDIR/.git/kcksefcli.yaml" \
			"$GITDIR/.git/secrets/kcksefcli.yaml" \
			"$GITDIR/.git/secret/kcksefcli.yaml" \
			"$GITDIR/secrets/kcksefcli.yaml" \
		; do
			if [[ -r "$i" ]]; then
				export KCKSEFCLI_CONFIG="$(readlink -f "$i")"
				# echo "export KCKSEFCLI_CONFIG=$KCKSEFCLI_CONFIG" >&2
				break
			fi
		done
	fi
	L_assert "Could not find KCKSEFCLI for integration tests. Integration tests have to execute by a human" \
		test -n "${KCKSEFCLI_CONFIG:-}"
	L_log "Using KCKSEFCLI_CONFIG=$KCKSEFCLI_CONFIG for integration tests"
	return 0
}

