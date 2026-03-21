#!/bin/bash
# A simple jq wrapper using sed.
#
# Usage:
#   jq_sed.sh <source> <subcommand> [args...]
#
# Arguments:
#   <source>:
#     -              Read JSON from standard input.
#     <filepath>     Read JSON from the specified file.
#
#   <subcommand>:
#     check          Validates the JSON structure. Exits with 0 if valid, 1 otherwise.
#     length         Outputs the number of elements in a JSON array.
#     compare <key> <value>
#                    Compares the value of a key in the JSON. The <key> should be in
#                    the format '.key'. Exits with 0 if they match, 1 otherwise.
#     .key           Extracts and prints the value of the specified key.

set -euo pipefail

usage() {
    echo "Usage: $0 <source> <subcommand> [args...]" >&2
    echo "  <source>: - (stdin) or <filepath>" >&2
    echo "  <subcommand>:" >&2
    echo "    check                  - Validates JSON structure" >&2
    echo "    length                 - Outputs array length" >&2
    echo "    compare <key> <value>  - Compares a key's value" >&2
    echo "    .key                   - Extracts a key's value" >&2
    exit 1
}

if [[ "$#" -lt 2 ]]; then
    usage
fi

source=$1
subcommand=$2
shift 2 # The rest are args for the subcommand

json_string=""
if [[ "$source" == "-" ]]; then
    json_string=$(cat)
else
    if [[ ! -f "$source" ]]; then
        echo "Error: File not found: $source" >&2
        exit 1
    fi
    json_string=$(cat "$source")
fi

# Function to extract a value for a given key
extract_value() {
    local json=$1
    local key_to_extract=$2
    local result

    # Try to extract string value: "key": "value"
    result=$(echo "$json" | sed -n "s/.*\"$key_to_extract\":[ ]*\"\([^\"]*\)\".*/\1/p")
    
    # If not found, try to extract numeric or boolean value: "key": 123 or "key": true
    if [[ -z "$result" ]]; then
        result=$(echo "$json" | sed -n "s/.*\"$key_to_extract\":[ ]*\([0-9a-zA-Z.]*\).*/\1/p" | sed 's/[,}]$//')
    fi
    echo "$result"
}

case "$subcommand" in
    check)
        # Very basic JSON structure check
        if ! grep -q '[{].*[}]' <<<"${json_string//$'\n'}"; then
            echo "Error: Invalid JSON structure: $json_string" >&2
            exit 1
        fi
        ;;

    length)
        if [[ "$json_string" == "[]" ]]; then
            echo 0
        else
            # Count commas and add 1 for arrays with content
            count=$(echo "$json_string" | tr -cd ',' | wc -c)
            echo $((count + 1))
        fi
        ;;

    compare)
        if [[ "$#" -ne 2 ]]; then
            echo "Error: 'compare' requires <key> and <expected_value>" >&2
            usage
        fi
        filter_key=$1
        expected_value=$2
        key=$(echo "$filter_key" | sed 's/^\.//')

        actual_value=$(extract_value "$json_string" "$key")

        if [[ "$actual_value" == "$expected_value" ]]; then
            exit 0
        else
            echo "Error: Comparison failed for key '${key}'. Expected: '${expected_value}', Got: '${actual_value}'" >&2
            exit 1
        fi
        ;;

    *) # Default case for extraction .key
        if [[ "$subcommand" =~ ^\..* ]]; then
            key=$(echo "$subcommand" | sed 's/^\.//')
            extract_value "$json_string" "$key"
        else
            echo "Error: Unknown subcommand '$subcommand'" >&2
            usage
        fi
        ;;
esac
