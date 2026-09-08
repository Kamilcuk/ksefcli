[Powrót do strony głównej](../README.md)

# Polecenie: `SortFaktury`

Organizuje pliki faktur (XML, JSON, PDF) w strukturę katalogów według roli (sprzedawca/nabywca) i daty.

**Użycie:**
```bash
kcksefcli SortFaktury -o /tmp/sortowane --no-move
```

**Opcje:**

| Opcja                | Opis                                                              | Wymagane | Domyślnie                    |
|----------------------|-------------------------------------------------------------------|----------|------------------------------|
| `-o`, `--output-dir` | Katalog wyjściowy dla uporządkowanych plików                      | Nie      | Bieżący katalog roboczy      |
| `-n`, `--dryrun`     | Pokaż co zostanie zrobione bez wprowadzania zmian                 | Nie      | false                        |
| `-p`, `--pdf`        | Dołącz pliki PDF do sortowania                                    | Nie      | true                         |
| `-j`, `--json`       | Dołącz pliki JSON (_summary.json) do sortowania                   | Nie      | true                         |
| `--xml`              | Dołącz pliki XML do sortowania                                    | Nie      | true                         |
| `--no-move`          | Kopiuj pliki zamiast przenosić (domyślnie: PRZENOŚ)               | Nie      | false (czyli domyślnie move) |
| (pozycyjne)          | Pliki XML do posortowania. Jeśli pominięte, skanuje bieżący katalog. | Nie      | Wszystkie pliki w CWD        |

**Struktura katalogów wyjściowych:**
```
<output-dir>/
  sprzedawca/
    202501/
      <KsefNumber> <SprzedawcaPierwszeSłowo> <NabywcaPierwszeSłowo> <KwotaBrutto>.xml
      <KsefNumber> <SprzedawcaPierwszeSłowo> <NabywcaPierwszeSłowo> <KwotaBrutto>_summary.json
      <KsefNumber> <SprzedawcaPierwszeSłowo> <NabywcaPierwszeSłowo> <KwotaBrutto>.pdf
    202502/
      ...
  nabywca/
    202501/
      ...
```

**Pobieranie danych:**
1. Najpierw próbuje odczytać dane z pliku `_summary.json` (tworzonego przez `PobierzFaktury`)
2. Jeśli nie ma JSON, parsuje plik XML (szuka `Podmiot1/Nazwa`, `Podmiot2/Nazwa`, `P_13`)
3. Datę pobiera z numeru KSeF (format: `XXXX-YYYY-MM-...`)

**Przykłady:**

Sortowanie wszystkich plików w bieżącym katalogu (przenosi):
```bash
kcksefcli SortFaktury -o /tmp/faktury_posortowane
```

Sortowanie z kopiowaniem (bez usuwania oryginałów):
```bash
kcksefcli SortFaktury -o /tmp/faktury_posortowane --no-move
```

Tylko podgląd (dry run):
```bash
kcksefcli SortFaktury -o /tmp/faktury_posortowane -n
```

Sortowanie tylko wybranych plików:
```bash
kcksefcli SortFaktury -o /tmp/out faktura1.xml faktura2.xml
```

Tylko XML i JSON (bez PDF):
```bash
kcksefcli SortFaktury -o /tmp/out --pdf false
```

**Obsługa duplikatów:**
Jeśli plik o tej samej nazwie już istnieje w katalogu docelowym, dopisywany jest sufiks `_1`, `_2`, itd.

**Nazwy plików:**
Format: `<KsefNumber> <SprzedawcaFirst> <NabywcaFirst> <GrossAmount:F2>.<ext>`
Przykład: `123456789-2025-01-15-ABCDEF123456 Jan Anna 1230.00.xml`