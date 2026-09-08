[Powrót do strony głównej](../README.md)

# Workflow: Mój proces pracy z KSeF

Ten dokument opisuje mój typowy miesięczny proces pracy z fakturami KSeF przy użyciu `kcksefcli`.

## 1. Pobieranie faktur z poprzedniego miesiąca

Pobieram wszystkie faktury (zakupowe i sprzedażowe) z ostatnich 2 miesięcy dla wszystkich typów podmiotów:

```bash
for i in 1 2 3; do
    kcksefcli PobierzFaktury --from -2month --to today -s $i
done
```

- `-s 1` (Subject1) - sprzedawca
- `-s 2` (Subject2) - nabywca  
- `-s 3` (Subject3) - podmiot trzeci
- Domyślnie zapisuje w bieżącym katalogu (`-o .` jest opcjonalne)

To tworzy w bieżącym katalogu pliki:
- `*.xml` - oryginalny XML z KSeF
- `*_summary.json` - metadane faktury (z `InvoiceSummary`)

Konwersję do PDF i pełny JSON faktury wykonuję osobno, jeśli są potrzebne:
```bash
# PDF z kodem QR
kcksefcli XML2PDF faktura.xml faktura.pdf

# Pełny JSON (surowe dane z KSeF)
kcksefcli XML2JSON faktura.xml -o faktura.json
```

## 2. Segregacja faktur

Używam `SortFaktury` do uporządkowania plików w strukturę katalogów:

```bash
kcksefcli SortFaktury
```

To tworzy strukturę:
```
sprzedawca/
  202609/
    5223217667-20260718-02AECE400000-96 KamCuk SQUAREPOINT 1230.00.xml
    5223217667-20260718-02AECE400000-96 KamCuk SQUAREPOINT 1230.00_summary.json
    5223217667-20260718-02AECE400000-96 KamCuk SQUAREPOINT 1230.00.json
    5223217667-20260718-02AECE400000-96 KamCuk SQUAREPOINT 1230.00.pdf
  202608/
    ...
nabywca/
  202609/
    5223268877-20260713-49A7F9800004-DE AccounTable KamCuk 500.00.xml
    ...
```

- Katalogi: `sprzedawca/` lub `nabywca/` (wykrywane automatycznie na podstawie NIP w profilu)
- Podkatalogi: `YYYYMM` (miesiąc z numeru KSeF)
- Nazwy plików: `<KsefNumber> <SprzedawcaPierwszeSłowo> <NabywcaPierwszeSłowo> <KwotaBrutto>.<ext>`

## 3. Wyciąganie numerów KSeF dla księgowej

Skrypt do wysłania listy faktur do księgowej:

```bash
#!/bin/bash
# Pobiera numery KSeF z faktur zakupowych (nabywca) z poprzedniego miesiąca

PREV_MONTH=$(date -d "last month" +%Y%m)
find nabywca/$PREV_MONTH -name "*.xml" -printf "%f\n" | \
    sed 's/\.xml$//' | \
    sort
```

Przykład użycia:
```bash
./lista_dla_ksiegowej.sh > faktury_zakupowe_$(date -d "last month" +%Y-%m).txt
```

Lub wersja one-liner:
```bash
ls nabywca/$(date -d "last month" +%Y%m)/*.xml 2>/dev/null | xargs -n1 basename | sed 's/\.xml$//' | sort
```

## 4. Wystawianie cyklicznej faktury (sprzedaż)

Mam stałego klienta, co miesiąc wystawiam podobną fakturę:

```bash
# Znajdź ostatnią fakturę dla tego klienta
LAST_INVOICE=$(ls sprzedawca/$(date -d "last month" +%Y%m)/*SQUAREPOINT*.xml | head -1)

# Utwórz nową fakturę z datą wykonania na koniec bieżącego miesiąca
END_OF_MONTH=$(date -d "$(date +%Y-%m-01) +1 month -1 day" +%Y-%m-%d)

kcksefcli WystawPodobnaFakture "$LAST_INVOICE" nowa_faktura.xml \
    --data-wykonania "$END_OF_MONTH"
```

To:
- Biera ostatnią fakturę sprzedażową do klienta SQUAREPOINT
- Tworzy nowy XML z zaktualizowaną datą wykonania na ostatni dzień miesiąca
- Automatycznie aktualizuje datę wystawienia (P_1), datę wykonania (P_6) i numer faktury (P_2)

## 5. Wysyłka faktury do KSeF

```bash
kcksefcli PrzeslijFaktury -u upo/ --upopdf --uposesji nowa_faktura.xml
```

Opcje:
- `-u upo/` - katalog na UPO (potwierdzenia odbioru)
- `--upopdf` - pobiera też PDF UPO
- `--uposesji` - pobiera UPO sesji

## 6. Pobieranie potwierdzonej faktury

Po wysłance pobieram fakturę z nadanym numerem KSeF, by miała pełne metadane:

```bash
kcksefcli PobierzFaktury --from today --to today -s 1 -o . --pdf --json
```

Lub pobieram konkretną po numerze KSeF (jeśli znam):
```bash
kcksefcli GetFaktura <KSEF_NUMBER> -o . --pdf --json
```

## Inne przydatne komendy

### Sprawdzanie limity certyfikatów
```bash
kcksefcli SprawdzLimitCertyfikatow
```

### Generowanie kodu QR do weryfikacji
```bash
kcksefcli QRWeryfikacjiFaktury <KSEF_NUMBER> -o qr.png
```

### Weryfikacja XML przed wysłką
```bash
kcksefcli WeryfikujXML faktura.xml
```

### Konwersja XML do PDF (offline z kodem QR)
```bash
kcksefcli WystawFaktureOffline faktura.xml faktura.pdf
```

### Ekstrakcja danych z XML
```bash
kcksefcli XMLExtract faktura.xml "/Faktura/Fa/P_13_1"  # kwota brutto
```

### Usuwanie namespace'ów z XML
```bash
kcksefcli XMLRemoveNamespace faktura_z_ns.xml faktura_bez_ns.xml
```

### Konwersja XML na JSON
```bash
kcksefcli XML2JSON faktura.xml -o faktura.json
```

## Aliasy w .bashrc / .zshrc

```bash
# Szybkie pobieranie miesięczne (tylko XML + _summary.json)
alias ksef-pobierz-miesiac='for i in 1 2 3; do kcksefcli PobierzFaktury --from -2month --to today -s $i; done'

# Segregacja
alias ksef-sortuj='kcksefcli SortFaktury'

# Konwersja do PDF (dla pojedynczego pliku)
alias ksef-xml2pdf='kcksefcli XML2PDF'

# Konwersja do JSON (dla pojedynczego pliku)
alias ksef-xml2json='kcksefcli XML2JSON'

# Lista dla księgowej (zakupy z zeszłego miesiąca)
alias ksef-lista-ksiegowa='ls nabywca/$(date -d "last month" +%Y%m)/*.xml 2>/dev/null | xargs -n1 basename | sed "s/\.xml$//" | sort'

# Ostatnia faktura sprzedażowa do danego klienta
ksef-ostatnia-sprzedaz() {
    local client=$1
    ls sprzedawca/$(date -d "last month" +%Y%m)/*${client}*.xml 2>/dev/null | head -1
}

# Nowa faktura cykliczna
ksef-nowa-cykliczna() {
    local last_invoice=$1
    local end_of_month=$(date -d "$(date +%Y-%m-01) +1 month -1 day" +%Y-%m-%d)
    kcksefcli WystawPodobnaFakture "$last_invoice" nowa_faktura.xml --data-wykonania "$end_of_month"
}
```

## Struktura katalogów po kilku miesiącach

```
~/ksef/
├── sprzedawca/
│   ├── 202607/
│   ├── 202608/
│   └── 202609/
├── nabywca/
│   ├── 202607/
│   ├── 202608/
│   └── 202609/
├── upo/
│   ├── 202609/
│   └── ...
├── nowa_faktura.xml
└── nowa_faktura.pdf
```

## Wskazówki

1. **Profile** - Używaj różnych profili (`-a profil`) dla różnych firm/NIP-ów
2. **Token cache** - Tokeny są cachowane w `~/.cache/kcksefcli/tokenstore.json`
3. **Konfiguracja** - Zobacz [Configuration.md](Configuration.md) dla szczegółów pliku `kcksefcli.yaml`
4. **Automatyzacja** - Możesz dodać powyższe skrypty do crona lub GitHub Actions/GitLab CI
5. **Backup** - Regularnie backupuj katalog z fakturami (XML to źródło prawdy)