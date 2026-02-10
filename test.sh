#!/usr/bin/env bash
set -euo pipefail

assert() {
  if ! "${@:2}"; then
    echo "ERROR: $*" >&2
    exit 2
  else
    echo "OK ${BASH_LINENO[0]}" >&2
  fi
}

setx() {
  local -
  set -x
  "$@" || exit "$?"
}

cd "$(dirname "$(readlink -f "$0")")"
if (( !$# )); then
  dotnet build src/KCKSeFCli
  set -- ./cli
fi
exe=("$@")

( setx "${exe[@]}" --version ) || :

setx "${exe[@]}" --help

# Test with cert_test profile (from file paths)
setx env KCKSEFCLI_CONFIG="tests/test_kcksefcli.yaml" "${exe[@]}" PrintConfig --active cert_test

# Test with token_test profile
setx env KCKSEFCLI_CONFIG="tests/test_kcksefcli.yaml" "${exe[@]}" PrintConfig --active token_test

# Test with cert_env_password_test profile
setx env TEST_PASSWORD_ENV="env_password" KCKSEFCLI_CONFIG="tests/test_kcksefcli.yaml" "${exe[@]}" PrintConfig --active cert_env_password_test

# Test with cert_inline_test profile
setx env KCKSEFCLI_CONFIG="tests/test_kcksefcli.yaml" "${exe[@]}" PrintConfig --active cert_inline_test

maybe=$PWD/.git/KSEF/kcksefcli.yaml
if [[ ! ( -r $maybe && ! -v KCKSEFCLI_CONFIG ) ]]; then
  echo "Skipping tests" >&2
  exit 0
fi
export KCKSEFCLI_CONFIG=$maybe
#

# Test SprawdzLimitCertyfikatow
tmp=$( setx "${exe[@]}" SprawdzLimitCertyfikatow -a token )
assert 'is a json' jq >/dev/null <<<"$tmp"

#
for i in 1 2; do
  tmp=$( setx "${exe[@]}" SzukajFaktur -a token -v --from 2026-01-21T00:00:00+01:00 --to 2026-01-22T00:00:00+01:00 )
  len=$( setx jq length <<<"$tmp" )
  assert '' test "$len" == 1
done
