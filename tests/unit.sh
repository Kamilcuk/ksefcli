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

clitest_weryfikuj_xml() {
	L_unittest_cmd cli WeryfikujXML "$DIR"/FA_3_Przykład_1.xml
}

clitest_dodaj_pozycje() {
    L_with_cd_tmpdir
    cp "$DIR"/FA_3_Przykład_1.xml test_invoice.xml
    L_unittest_cmd cli DodajPozycjeNaFakturze test_invoice.xml test_invoice_out.xml \
        --nazwa "Nowa Pozycja" \
        --miara "szt" \
        --ilosc 10 \
        --cena-netto 100.00 \
        --stawka-vat 23

    local p13_1 p14_1 p15
    L_unittest_cmd -v p13_1 cli XMLExtract test_invoice_out.xml "/ns:Faktura/ns:Fa/ns:P_13_1" --namespaces "ns=http://crd.gov.pl/wzor/2025/06/25/13775/"
    L_unittest_cmd -v p14_1 cli XMLExtract test_invoice_out.xml "/ns:Faktura/ns:Fa/ns:P_14_1" --namespaces "ns=http://crd.gov.pl/wzor/2025/06/25/13775/"
    L_unittest_cmd -v p15 cli XMLExtract test_invoice_out.xml "/ns:Faktura/ns:Fa/ns:P_15" --namespaces "ns=http://crd.gov.pl/wzor/2025/06/25/13775/"

    L_unittest_vareq p13_1 "2666.66"
    L_unittest_vareq p14_1 "613.33"
    L_unittest_vareq p15 "3281.00"
}

clitest_nowa_faktura() {
    L_with_cd_tmpdir
    L_unittest_cmd cli NowaFaktura "$DIR"/test_invoice.yaml invoice.xml
    L_unittest_cmd ls -la invoice.xml
}

clitest_pobierz_info_o_nip() {
    local output
    L_unittest_cmd -v output cli PobierzInfoONip "5260202588" --data "$(date +%Y-%m-%d)"
    L_unittest_cmd -I grep -q "subject" <<<"$output"
}

clitest_xml_extract() {
    L_with_cd_tmpdir
    cat <<EOF > test.xml
<Root>
    <Element1>Value1</Element1>
    <Element2>
        <NestedElement>NestedValue</NestedElement>
    </Element2>
</Root>
EOF
    local output
    L_unittest_cmd -v output cli XMLExtract test.xml "/Root/Element1"
    L_unittest_vareq output "Value1"

    L_unittest_cmd -v output cli XMLExtract test.xml "/Root/Element2/NestedElement"
    L_unittest_vareq output "NestedValue"
}






DIR="$(dirname "$(readlink -f "${BASH_SOURCE[0]}")")"
. "$DIR"/cmdauth.sh
. "$DIR"/lib.sh "$@"
. "$DIR"/test_parsedate.sh
testlib_main "$@"
