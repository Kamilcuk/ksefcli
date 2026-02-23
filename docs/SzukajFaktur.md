[Powrót do strony głównej](../README.md)

# Polecenie: `SzukajFaktur`

Wyszukuje faktury na podstawie podanych kryteriów. Odpowiada endpointowi `GET /online/Query/Invoice/Sync`.

**Użycie:**
```bash
kcksefcli SzukajFaktur --from "-7days" --subjectType Subject2
```

**Opcje:**

| Opcja                                   | Opis                                                                                                                                     | Domyślnie    | Wymagane |
|-----------------------------------------|------------------------------------------------------------------------------------------------------------------------------------------|--------------|----------|
| `-s`, `--subjectType`                   | Typ podmiotu dla kryteriów filtrowania. Możliwe wartości: `Subject1` (sprzedawca), `Subject2` (nabywca), `Subject3`, `SubjectAuthorized`. | `Subject1`   | Tak      |
| `--from`                                | Data początkowa. Szczegóły formatu daty: zobacz [ParseDate](#parsedate).                                       |              | Tak      |
| `--to`                                  | Data końcowa. Szczegóły formatu daty: zobacz [ParseDate](#parsedate).                                                   |              | Nie      |
| `--dateType`                            | Typ daty używany w zakresie dat. Możliwe wartości: `Issue`, `Invoicing`, `PermanentStorage`.                                               | `Issue`      | Tak      |
| `--pageOffset`                          | Przesunięcie strony dla paginacji.                                                                                                       | `0`          | Nie      |
| `--pageSize`                            | Rozmiar strony dla paginacji.                                                                                                            | `10`         | Nie      |
| `--restrictToPermanentStorageHwmDate`   | Ogranicza filtrowanie do `PermanentStorageHwmDate`. Dotyczy tylko `dateType` = `PermanentStorage`.                                     |              | Nie      |
| `--ksefNumber`                          | Numer KSeF faktury (dokładne dopasowanie).                                                                                               |              | Nie      |
| `--invoiceNumber`                       | Numer faktury nadany przez wystawcę (dokładne dopasowanie).                                                                              |              | Nie      |
| `--amountType`                          | Typ filtru kwotowego. Możliwe wartości: `Brutto`, `Netto`, `Vat`.                                                                          |              | Nie      |
| `--amountFrom`                          | Minimalna wartość kwoty.                                                                                                                 |              | Nie      |
| `--amountTo`                            | Maksymalna wartość kwoty.                                                                                                                |              | Nie      |
| `--sellerNip`                           | NIP sprzedawcy (dokładne dopasowanie).                                                                                                   |              | Nie      |
| `--buyerIdentifierType`                 | Typ identyfikatora nabywcy. Możliwe wartości: `Nip`, `VatUe`, `Other`, `None`.                                                            |              | Nie      |
| `--buyerIdValue`                        | Wartość identyfikatora nabywcy (dokładne dopasowanie).                                                                                   |              | Nie      |
| `--currencyCodes`                       | Kody walut, oddzielone przecinkami (np. `PLN,EUR`).                                                                                       |              | Nie      |
| `--invoicingMode`                       | Tryb fakturowania: `Online` lub `Offline`.                                                                                               |              | Nie      |
| `--isSelfInvoicing`                     | Czy faktura jest samofakturowaniem.                                                                                                      |              | Nie      |
| `--formType`                            | Typ dokumentu. Możliwe wartości: `FA`, `PEF`, `RR`.                                                                                      |              | Nie      |
| `--invoiceTypes`                        | Typy faktur, oddzielone przecinkami (np. `Vat`, `Zal`, `Kor`).                                                                             |              | Nie      |
| `--hasAttachment`                       | Czy faktura posiada załącznik.                                                                                                           |              | Nie      |

---


## Konfiguracja i Uwierzytelnianie

To polecenie łączy się z serwerami KSeF i w pełni obsługuje system profili, opcje konfiguracji (`kcksefcli.yaml`) oraz automatycznej pamięci podręcznej (cache) tokenów sesyjnych.
Szczegółowe informacje o zarządzaniu sesją, przełączaniu profili i środowisk znajdują się w pliku: [**Konfiguracja**](Configuration.md).
