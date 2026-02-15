#!/bin/bash

# Source the test utilities
. "$(dirname -- "$0")"/test-utils.sh

# Test case: Command-line environment and NIP
clitest_cmd_env_nip() {
    local output
    output=$(cli PrintConfig --environment test --nip 1234567890 --json)
    L_unittest_cmd -I jq -e '.active_profile == "cmd"' <<<"$output"
    L_unittest_cmd -I jq -e '.profile.Environment == "test"' <<<"$output"
    L_unittest_cmd -I jq -e '.profile.Nip == "1234567890"' <<<"$output"
}

# Test case: Command-line token authentication
clitest_cmd_token_auth() {
    local output
    output=$(cli PrintConfig --environment demo --nip 0987654321 --token mytesttoken --json)
    L_unittest_cmd -I jq -e '.active_profile == "cmd"' <<<"$output"
    L_unittest_cmd -I jq -e '.profile.Environment == "demo"' <<<"$output"
    L_unittest_cmd -I jq -e '.profile.Nip == "0987654321"' <<<"$output"
    L_unittest_cmd -I jq -e '.profile.Token == "mytesttoken"' <<<"$output"
    L_unittest_cmd -I jq -e '.profile.AuthMethod == "KsefToken"' <<<"$output"
}

# Test case: Command-line certificate authentication
clitest_cmd_cert_auth() {
    local output
    local cert_file="$DIR/my_certificate.pem"
    local key_file="$DIR/my_private_key.pem"
    local password_env_var="KSEF_TEST_PASSWORD"
    export KSEF_TEST_PASSWORD="testpassword"

    output=$(cli PrintConfig --environment prod --nip 1122334455 --certificate-file "$cert_file" --private-key-file "$key_file" --password-env "$password_env_var" --json)
    L_unittest_cmd -I jq -e '.active_profile == "cmd"' <<<"$output"
    L_unittest_cmd -I jq -e '.profile.Environment == "prod"' <<<"$output"
    L_unittest_cmd -I jq -e '.profile.Nip == "1122334455"' <<<"$output"
    L_unittest_cmd -I jq -e '.profile.Certificate.Password == "testpassword"' <<<"$output"
    L_unittest_cmd -I jq -e '.profile.AuthMethod == "Xades"' <<<"$output"
    # Verify certificate and private key content is loaded (truncated for brevity in actual JSON)
    L_unittest_cmd -I grep -q "BEGIN CERTIFICATE" <<<"$output"
    L_unittest_cmd -I grep -q "BEGIN PRIVATE KEY" <<<"$output"
    unset KSEF_TEST_PASSWORD
}

# Test case: Conflict between --config and command-line profile options
clitest_cmd_config_conflict() {
    local output
    output=$(KCKSEFCLI_CONFIG="$DIR/test_kcksefcli.yaml" cli PrintConfig --environment test --json 2>&1)
    L_unittest_cmd -I grep -q "Cannot use --config or --active with command-line profile options." <<<"$output"
}

# Test case: Conflict between --active and command-line profile options
clitest_cmd_active_conflict() {
    local output
    output=$(cli PrintConfig --active cert_test --environment test --json 2>&1)
    L_unittest_cmd -I grep -q "Cannot use --config or --active with command-line profile options." <<<"$output"
}
