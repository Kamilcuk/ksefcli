#!/usr/bin/env bash
set -euo pipefail

DIR="$(dirname "$(readlink -f "$0")")"

# Download L_lib.sh library
if hash L_lib.sh 2>/dev/null; then
  . L_lib.sh -s
else
  if [[ ! -e L_lib.sh ]]; then
	  curl -sS -o "$DIR"/L_lib.sh -z "$DIR"/L_lib.sh https://raw.githubusercontent.com/Kamilcuk/L_lib/refs/heads/v1/bin/L_lib.sh
  fi
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

clitest_version() {
	cli --version
}

clitest_help() {
	cli --help
}

clitest_profile_cert() {
	KCKSEFCLI_CONFIG="$DIR/test_kcksefcli.yaml" cli PrintConfig --active cert_test >/dev/null
}

clitest_profile_token() {
	KCKSEFCLI_CONFIG="$DIR/test_kcksefcli.yaml" cli PrintConfig --active token_test >/dev/null
}

clitest_profile_env_pw() {
	TEST_PASSWORD_ENV="env_password" KCKSEFCLI_CONFIG="$DIR/test_kcksefcli.yaml" \
		cli PrintConfig --active cert_env_password_test >/dev/null
}

clitest_profile_inline() {
	KCKSEFCLI_CONFIG="$DIR/test_kcksefcli.yaml" cli PrintConfig --active cert_inline_test >/dev/null
}

clitest_help_uniewaznij() {
	local output
	output=$(cli UniewaznijCertyfikat --help)
	grep -q "Certificate serial number to revoke" <<<"$output"
}

clitest_help_wylistuj() {
	local output
	output=$(cli WylistujCertyfikaty --help)
	grep -q "Filter by certificate status" <<<"$output"
}

clitest_help_pobierz() {
	local output
	output=$(cli PobierzCertyfikat --help)
	grep -q "Certificate serial number to retrieve" <<<"$output"
}

clitest_help_nowy() {
	local output
	output=$(cli NowyCertyfikat --help)
	grep -q "Name for the new certificate" <<<"$output"
}

# integration tests

setup_integration_config() {
    local maybe="$PWD/.git/KSEF/kcksefcli.yaml"
    if [[ ! ( -r "$maybe" && ! -v KCKSEFCLI_CONFIG ) ]]; then
        echo "skipping config-dependent tests: $maybe missing" >&2
        return 1
    fi
    export KCKSEFCLI_CONFIG="$maybe"
}

clitest_integration_limit_json() {
	setup_integration_config || return 0
	local output
	output=$(cli SprawdzLimitCertyfikatow -a token)
	[[ $? -eq 0 ]] && jq -e . >/dev/null <<<"$output"
}

clitest_integration_szukaj_faktur_loop() {
	setup_integration_config || return 0
	local output len
	for i in 1 2; do
		output=$(cli SzukajFaktur -a token -v --from 2026-01-21T00:00:00+01:00 --to 2026-01-22T00:00:00+01:00)
		[[ $? -ne 0 ]] && return 1
		len=$(jq length <<<"$output")
		[[ "$len" -ne 1 ]] && return 1
	done
}

L_unittest_main -P clitest_ ${opt_r:+-r"$opt_r"}
