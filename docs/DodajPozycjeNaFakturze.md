[Powrót do strony głównej](../README.md)

# Polecenie: `DodajPozycjeNaFakturze`

Służy do dodawania nowej pozycji (towaru lub usługi) do istniejącej faktury KSeF w formacie XML.

Polecenie parsowania faktury w formacie XML, po czym wstrzykuje nową pozycję do sekcji `FaWiersz`, ponownie wyliczając sumy podatków oraz wartości brutto i podmieniając je w pliku. Waliduje też zaktualizowaną fakturę na zgodność ze schematem XML.

**Użycie:**
```bash
kcksefcli DodajPozycjeNaFakturze <plik-wejsciowy-xml> [<plik-wyjsciowy-xml>] --nazwa "Usługa" --miara "szt" --ilosc 1 --cena-netto 100 --stawka-vat 23
```

**Argumenty:**

| Argument             | Opis                                                                                                    | Wymagane |
|----------------------|---------------------------------------------------------------------------------------------------------|----------|
| `plik-wejsciowy-xml` | Ścieżka do istniejącego pliku XML z fakturą KSeF.                                                       | Tak      |
| `plik-wyjsciowy-xml` | Ścieżka wyjściowa dla pliku XML. Jeśli nie zostanie podany, plik wejściowy zostanie nadpisany.          | Nie      |

**Opcje:**

| Opcja                 | Opis                                             | Wymagane |
|-----------------------|--------------------------------------------------|----------|
| `--nazwa`             | Nazwa towaru lub usługi (pole P_7).              | Tak      |
| `--miara`             | Jednostka miary (pole P_8A).                     | Tak      |
| `--ilosc`             | Ilość (pole P_8B).                               | Tak      |
| `--cena-netto`        | Cena jednostkowa netto (pole P_9A).              | Tak      |
| `--stawka-vat`        | Stawka podatku VAT (pole P_12), np. 23, 8, 5, 0. | Tak      |
| `--bez-walidacji`     | Pomija walidację XML po modyfikacji pliku.       | Nie      |
