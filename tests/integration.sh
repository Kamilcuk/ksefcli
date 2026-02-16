#!/usr/bin/env bash
set -euo pipefail

clitest_z_integration_SprawdzLimitCertyfikatow() {
	local output
	L_unittest_cmd -v output cli SprawdzLimitCertyfikatow -a mytoken
	"$DIR"/jq_sed.sh - check <<<"$output" >/dev/null || return 1
}

clitest_z_integration_PobierzFaktury() {
	L_unittest_cmd -v output cli SzukajFaktur -a token2 -v --from 2026-01-21T00:00:00+01:00 --to 2026-01-22T00:00:00+01:00
	L_unittest_cmd -I -r '[12]' "$DIR"/jq_sed.sh - length <<<"$output"
	#
	L_with_cd_tmpdir
	L_unittest_cmd cli PobierzFaktury -a token2 -v --from 2026-01-21T00:00:00+01:00 --to 2026-01-22T00:00:00+01:00 -o . --pdf
	L_unittest_cmd ls -lah 5260215591-20260124-01006068A46A-59.{json,pdf,xml}
	L_unittest_cmd -v _ "$DIR"/jq_sed.sh 5260215591-20260124-01006068A46A-59.json check
}

clitest_z_integration_PrzeslijFaktury() {
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

clitest_z_integration_PobierzFaktury_prod() {
	L_with_cd_tmpdir
	L_unittest_cmd -v output cli PobierzFaktury -a dyzio-prod --from 2026-02-05 --to 2026-02-05 -s Subject2 -o /tmp --pdf
}

DIR="$(dirname "$(readlink -f "${BASH_SOURCE[0]}")")"
. "$DIR"/lib.sh "$@"
testlib_setup_integration_config
testlib_main "$@"
