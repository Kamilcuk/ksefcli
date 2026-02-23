#!/bin/bash
set -xeuo pipefail
./tests/integration.sh -r 'clitest_xml2pdf_qrcodes|clitest_z_integration_WystawFaktureOffline' "$@"
