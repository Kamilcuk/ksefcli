[Powrót do strony głównej](../README.md)

# Polecenie: `XML2JSON`

Konwertuje plik XML faktury na format JSON.

**Użycie:**
```bash
kcksefcli XML2JSON faktura.xml -o faktura.json
```

**Opcje:**

| Opcja           | Opis                                                         | Wymagane | Domyślnie |
|-----------------|--------------------------------------------------------------|----------|-----------|
| (pozycyjna)     | Ścieżka do pliku XML wejściowego                             | Tak      |           |
| `-o`, `--output`| Ścieżka do pliku JSON wyjściowego. Jeśli nie podano, wypisuje na stdout. | Nie      |           |
| `--indent`      | Wypisz JSON z wcięciami (ładny format).                      | Nie      | true      |

**Przykłady:**

Konwersja z zapisem do pliku:
```bash
kcksefcli XML2JSON faktura.xml -o faktura.json
```

Konwersja z wypisaniem na stdout:
```bash
kcksefcli XML2JSON faktura.xml
```

Konwersja bez wcięć (kompaktowy format):
```bash
kcksefcli XML2JSON faktura.xml --indent false
```

**Format wyjściowy:**
JSON zachowuje strukturę XML:
- Atrybuty XML → klucze z prefiksem `@` (np. `@attr`)
- Treść elementu → klucz `#text` (gdy element ma atrybuty lub dzieci)
- Elementy z tą samą nazwą → tablica
- Elementy zagnieżdżone → obiekty zagnieżdżone