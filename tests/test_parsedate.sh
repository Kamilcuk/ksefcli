#!/usr/bin/env bash
set -euo pipefail

parse_date_check() {
    local output=$1
    local expected=$2
    awk -v output="$output" -v expected="$expected" '
        BEGIN {
            diff = output - expected;
            if (diff < 0) diff = -diff;
            if (diff <= 1) {
                exit 0;
            } else {
                print "Timestamp difference too large: |" output " - " expected "| = " diff " > 1";
                exit 1;
            }
        }
    '
}

clitest_ParseDate_simple_seconds() {
    local output
    L_unittest_cmd -v output cli ParseDate --seconds "2024-01-02 10:20:30.123"
    local expected
    L_unittest_cmd -v expected date -d "2024-01-02 10:20:30.123" "+%s.%N"
    L_unittest_cmd parse_date_check "$output" "$expected"
}

clitest_ParseDate_relative_days_seconds() {
    local output
    L_unittest_cmd -v output cli ParseDate --seconds "-1day"
    local expected
    L_unittest_cmd -v expected date -d "1 day ago" "+%s.%N"
    L_unittest_cmd parse_date_check "$output" "$expected"
}

clitest_ParseDate_relative_weeks_seconds() {
    local output
    L_unittest_cmd -v output cli ParseDate --seconds "-2weeks"
    local expected
    L_unittest_cmd -v expected date -d "2 weeks ago" "+%s.%N"
    L_unittest_cmd parse_date_check "$output" "$expected"
}

clitest_ParseDate_human_readable_seconds() {
    local output
    L_unittest_cmd -v output cli ParseDate --seconds "yesterday"
    local expected
    # HumanDateParser.Parse("yesterday") likely returns yesterday at midnight in local time.
    L_unittest_cmd -v expected date -d "yesterday 00:00:00" "+%s.%N"
    L_unittest_cmd parse_date_check "$output" "$expected"
}

clitest_ParseDate_simple_iso() {
    local output
    L_unittest_cmd -v output cli ParseDate "2024-01-02 10:20:30.123"
    L_unittest_vareq output "2024-01-02T10:20:30.123000"
}

clitest_ParseDate_seconds_output_comma() {
    local output
    L_unittest_cmd -v output cli ParseDate --seconds "2024-01-02 10:20:30,123"
    local expected
    L_unittest_cmd -v expected date -d "2024-01-02 10:20:30.123" "+%s.%N"
    L_unittest_cmd parse_date_check "$output" "$expected"
}
