[Powrót do strony głównej](../README.md)

# Polecenie: `PobierzFaktury`

Pobiera wiele faktur na podstawie kryteriów wyszukiwania. Rozszerza polecenie `SzukajFaktur` o opcje zapisywania plików.

**Użycie:**
```bash
kcksefcli PobierzFaktury --from "-7days" --subjectType Subject2 -o /tmp/faktury
```

**Opcje:**
To polecenie akceptuje wszystkie opcje z `SzukajFaktur` oraz dodatkowo:

| Opcja                  | Opis                                                            | Wymagane | Domyślnie |
|------------------------|-----------------------------------------------------------------|----------|-----------|
| `-o`, `--outputdir`    | Katalog wyjściowy do zapisania faktur.                          | Nie      | Bieżący katalog |
| `--useInvoiceNumber`   | Użyj `InvoiceNumber` zamiast `KsefNumber` jako nazwy pliku.     | Nie      |           |
| `--no-summary`         | Nie zapisuj metadanych faktury w plikach _summary.json.         | Nie      |           |
| `--retry-attempts`     | Liczba ponownych prób przy limicie zapytań.                     | Nie      | 5         |
| `--no-local-rate-limit`| Wyłącza lokalny limit zapytań.                                  | Nie      |           |

---

### Pliki wyjściowe

Domyślnie dla każdej faktury tworzone są:
- `<ksefNumber>.xml` - oryginalny XML z KSeF
- `<ksefNumber>_summary.json` - metadane faktury (z `InvoiceSummary`)

Konwersję do PDF i pełny JSON faktury należy wykonać osobno:
- PDF: `kcksefcli XML2PDF faktura.xml faktura.pdf`
- Pełny JSON: `kcksefcli XML2JSON faktura.xml -o faktura.json`

---

## Konfiguracja i Uwierzytelnianie

To polecenie łączy się z serwerami KSeF i w pełni obsługuje system profili, opcje konfiguracji (`kcksefcli.yaml`) oraz automatyczną pamięć podręczną (cache) tokenów sesyjnych.
Szczegółowe informacje o zarządzaniu sesją, przełączaniu profili i środowisk znajdują się w pliku: [**Konfiguracja**](Configuration.md).
