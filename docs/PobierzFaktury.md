[Powrót do strony głównej](../README.md)

# Polecenie: `PobierzFaktury`

Pobiera wiele faktur na podstawie kryteriów wyszukiwania. Rozszerza polecenie `SzukajFaktur` o opcje zapisywania plików.

**Użycie:**
```bash
kcksefcli PobierzFaktury --from "-7days" --subjectType Subject2 -o /tmp/faktury --pdf
```

**Opcje:**
To polecenie akceptuje wszystkie opcje z `SzukajFaktur` oraz dodatkowo:

| Opcja                  | Opis                                                            | Wymagane | Domyślnie |
|------------------------|-----------------------------------------------------------------|----------|-----------|
| `-o`, `--outputdir`    | Katalog wyjściowy do zapisania faktur.                          | Tak      |           |
| `-p`, `--pdf`          | Zapisz również wersję PDF faktury.                              | Nie      |           |
| `--useInvoiceNumber`   | Użyj `InvoiceNumber` zamiast `KsefNumber` jako nazwy pliku.     | Nie      |           |
| `--zapiszjson`         | Zapisz metadane faktury w plik .json.                           | Nie      |           |
| `--retry-attempts`     | Liczba ponownych prób przy limicie zapytań.                     | Nie      | 5         |
| `--no-local-rate-limit`| Wyłącza lokalny limit zapytań.                                  | Nie      |           |

---


## Konfiguracja i Uwierzytelnianie

To polecenie łączy się z serwerami KSeF i w pełni obsługuje system profili, opcje konfiguracji (`kcksefcli.yaml`) oraz automatycznej pamięci podręcznej (cache) tokenów sesyjnych.
Szczegółowe informacje o zarządzaniu sesją, przełączaniu profili i środowisk znajdują się w pliku: [**Konfiguracja**](Configuration.md).
