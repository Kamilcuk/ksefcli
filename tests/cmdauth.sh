#!/bin/bash

# Test case: Command-line token authentication
clitest_cmd_token_auth() {
    local output
    output=$(cli PrintConfig --environment demo --token 'mytesttoken|nip-1234567890|123' --json)
    L_unittest_cmd -I jq -e '.active_profile == ".__cmd__"' <<<"$output"
    L_unittest_cmd -I jq -e '.profile.Environment == "demo"' <<<"$output"
    L_unittest_cmd -I jq -e '.profile.Nip == "1234567890"' <<<"$output"
    L_unittest_cmd -I jq -e '.profile.Token == "mytesttoken|nip-1234567890|123"' <<<"$output"
    # L_unittest_cmd -I jq -e '.profile.AuthMethod == "KsefToken"' <<<"$output"
    L_unittest_cmd -I jq -e '.profile.AuthMethod == 1' <<<"$output"
}

# Test case: Conflict between --config and command-line profile options
clitest_cmd_config_missing() {
    L_unittest_cmd -j -v ouptut -e 134 \
        cli PrintConfig --environment test --json
    L_unittest_cmd -j -v output -e 134 \
        cli PrintConfig --token 123 --json
}

# Test case: Conflict between --active and command-line profile options
clitest_cmd_active_conflict() {
    L_unittest_cmd -j -r "Cannot use --config or --active with command-line profile options." -e 134 \
        cli PrintConfig --active cert_test --environment test --json
}
