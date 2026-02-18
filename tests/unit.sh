#!/usr/bin/env bash
set -euo pipefail

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
	L_unittest_cmd -v output cli UniewaznijCertyfikat --help
	L_unittest_cmd -I grep -q "Certificate serial number to revoke" <<<"$output"
}

clitest_help_wylistuj() {
	local output
	L_unittest_cmd -v output cli WylistujCertyfikaty --help
	L_unittest_cmd -I grep -q "Filter by certificate name" <<<"$output"
}

clitest_help_pobierz() {
	local output
	L_unittest_cmd -v output cli PobierzCertyfikat --help
	L_unittest_cmd -I grep -q "Certificate serial number to retrieve" <<<"$output"
}

clitest_help_nowy() {
	local output
	L_unittest_cmd -v output cli NowyCertyfikat --help
	L_unittest_cmd -I grep -q "Name for the new certificate" <<<"$output"
}

clitest_cmd_token_test() {
    local output
	KCKSEFCLI_CONFIG="$DIR/test_kcksefcli.yaml" L_unittest_cmd -v output \
		cli PrintConfig -a token_test
	KCKSEFCLI_CONFIG="$DIR/test_kcksefcli.yaml" L_unittest_cmd -v output \
		cli PrintConfig -a token_no_nip_test
}


DIR="$(dirname "$(readlink -f "${BASH_SOURCE[0]}")")"
. "$DIR"/cmdauth.sh
. "$DIR"/lib.sh "$@"
. "$DIR"/test_parsedate.sh
testlib_main "$@"
