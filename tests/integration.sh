#!/bin/bash

setup_integration_config() {
	local maybe="$DIR/../.git/KSEF/kcksefcli.yaml"
	if [[ -r "$maybe" ]]; then
		export KCKSEFCLI_CONFIG="$(readlink -f "$maybe")"
		L_log "Using KCKSEFCLI_CONFIG=$KCKSEFCLI_CONFIG"
		return 0
	fi
	echo "skipping integration tests: $maybe missing" >&2
	return 1
}

clitest_z_integration_SprawdzLimitCertyfikatow() {
	setup_integration_config || return 0
	local output
	L_unittest_cmd -v output cli SprawdzLimitCertyfikatow -a mytoken
	jq -es . <<<"$output" >/dev/null || return 1
}

clitest_z_integration_PobierzFaktury() {
	setup_integration_config || return 0
	#
	L_unittest_cmd -v output cli SzukajFaktur -a token2 -v --from 2026-01-21T00:00:00+01:00 --to 2026-01-22T00:00:00+01:00
	L_unittest_cmd -I -r '[12]' jq length <<<"$output"
	#
	L_with_cd_tmpdir
	L_unittest_cmd cli PobierzFaktury -a token2 -v --from 2026-01-21T00:00:00+01:00 --to 2026-01-22T00:00:00+01:00 -o . --pdf
	L_unittest_cmd ls -lah 5260215591-20260124-01006068A46A-59.{json,pdf,xml}
	L_unittest_cmd -v _ jq . 5260215591-20260124-01006068A46A-59.json
}

clitest_z_integration_PrzeslijFaktury() {
	setup_integration_config || return 0
	#
	L_with_cd_tmpdir
	sed "s/<P_2>.*</<P_2>$(date +%s.%N)</" "$DIR"/FA_3_Przykład_1.xml > faktura1.xml
	sed "s/<P_2>.*</<P_2>$(date +%s.%N)</" "$DIR"/FA_3_Przykład_1.xml > faktura2.xml
	L_unittest_cmd \
		cli PrzeslijFaktury -a mytoken --upodir . --upopdf faktura1.xml faktura2.xml
	rm faktura1.xml faktura2.xml
	L_unittest_cmd ls -lah
	local xmls pdfs
	pdfs="$(find . -maxdepth 1 -name "*.pdf" | wc -l)"
	xmls="$(find . -maxdepth 1 -name "*.xml" | wc -l)"
	L_unittest_vareq xmls 2
	L_unittest_vareq pdfs 2
}
