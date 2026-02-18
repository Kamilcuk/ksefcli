#!/bin/bash
set -euo pipefail

# Sprawdź, czy podano argument
if (($# != 1)) || [[ -z "$1" ]]; then
  echo "Użycie: $0 <plik_wyjściowy_faktury.xml>"
  echo ""
  echo "Tworzy nowy plik XML faktury na podstawie szablonu FA_3_Przykład_1.xml,"
  echo "automatycznie aktualizując pole P_2 (data wystawienia) na aktualny timestamp."
  exit 1
fi

output=$1
DIR="$(dirname "$(readlink -f "${BASH_SOURCE[0]}")")"
sed "s/<P_2>.*</<P_2>$(date +%s.%N)</" "$DIR"/FA_3_Przykład_1.xml > "$1"
