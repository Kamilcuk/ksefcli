[Powrót do strony głównej](../README.md)

# Polecenie: `LinkDoFaktury`

Generuje link weryfikacyjny dla pojedynczej faktury.

**Użycie:**
```bash
kcksefcli LinkDoFaktury <ksef-numer>
```

**Argumenty:**

| Argument      | Opis                  | Wymagane |
|---------------|-----------------------|----------|
| `ksef-numer`  | Numer KSeF faktury.   | Tak      |

---


## Konfiguracja i Uwierzytelnianie

To polecenie łączy się z serwerami KSeF i w pełni obsługuje system profili, opcje konfiguracji (`kcksefcli.yaml`) oraz automatycznej pamięci podręcznej (cache) tokenów sesyjnych.
Szczegółowe informacje o zarządzaniu sesją, przełączaniu profili i środowisk znajdują się w pliku: [**Konfiguracja**](Configuration.md).
