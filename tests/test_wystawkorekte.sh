#!/bin/bash
. "$(dirname "$0")"/lib.sh

clitest_wystawkorekte() {
    INPUT_FILE="tests/FA_3_Przykład_1_korekta_input.xml"
    OUTPUT_FILE="/tmp/korekta_output.xml"
    EXPECTED_FILE="tests/expected_korekta.xml"

    # Generate the correction file
    L_unittest_cmd "$opt_exe" WystawKorekte \
        "$INPUT_FILE" \
        "$OUTPUT_FILE" \
        1 5 \
        --PrzyczynaKorekty "Testowa korekta" \
        --no-validate
    
    # Compare the generated file with the expected one
    L_unittest_cmd diff -u "$EXPECTED_FILE" "$OUTPUT_FILE"
    
    rm "$OUTPUT_FILE"
}

testlib_main "$@"
