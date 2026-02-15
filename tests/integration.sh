#!/bin/bash

setup_integration_config() {
	local maybe="$DIR/../.git/KSEF/kcksefcli.yaml"
	if [[ -r "$maybe" ]]; then
		export KCKSEFCLI_CONFIG="$maybe"
		return 0
	fi
	echo "skipping integration tests: $maybe missing" >&2
	return 1
}

clitest_z_integration_limit_json() {
	setup_integration_config || return 0
	local output
	output=$(cli SprawdzLimitCertyfikatow -a token) || return 1
	jq -es . <<<"$output" >/dev/null || return 1
}

clitest_z_integration_szukaj_faktur_loop() {
	setup_integration_config || return 0
	local output len
	for i in 1 2; do
		output=$(cli SzukajFaktur -a token -v --from 2026-01-21T00:00:00+01:00 --to 2026-01-22T00:00:00+01:00) || return 1
		L_unittest_cmd -I -r '[12]' jq length <<<"$output"
	done
}

