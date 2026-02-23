[Powrót do strony głównej](../README.md)

# Polecenie: `GetFaktura`

Pobiera pojedynczą fakturę w formacie XML.

**Użycie:**
```bash
kcksefcli GetFaktura <ksef-numer>
```

**Argumenty:**

| Argument      | Opis                  | Wymagane |
|---------------|-----------------------|----------|
| `ksef-numer`  | Numer KSeF faktury.   | Tak      |

---


## Konfiguracja i Uwierzytelnianie

To polecenie łączy się z serwerami KSeF i w pełni obsługuje system profili, opcje konfiguracji (`kcksefcli.yaml`) oraz automatycznej pamięci podręcznej (cache) tokenów sesyjnych.
Szczegółowe informacje o zarządzaniu sesją, przełączaniu profili i środowisk znajdują się w pliku: [**Konfiguracja**](Configuration.md).
