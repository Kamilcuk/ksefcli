[Powrót do strony głównej](../README.md)

# Polecenie: `PobierzCertyfikat`

Pobiera treść certyfikatu KSeF na podstawie numeru seryjnego.

**Użycie:**
```bash
kcksefcli PobierzCertyfikat <numer-seryjny>
```

**Argumenty:**

| Argument        | Opis                                      | Wymagane |
|-----------------|-------------------------------------------|----------|
| `numer-seryjny` | Numer seryjny certyfikatu.                | Tak      |

**Opcje:**

| Opcja               | Opis                                 |
|---------------------|--------------------------------------|
| `-o`, `--outputFile`| Ścieżka zapisu certyfikatu.          |

---


## Konfiguracja i Uwierzytelnianie

To polecenie łączy się z serwerami KSeF i w pełni obsługuje system profili, opcje konfiguracji (`kcksefcli.yaml`) oraz automatycznej pamięci podręcznej (cache) tokenów sesyjnych.
Szczegółowe informacje o zarządzaniu sesją, przełączaniu profili i środowisk znajdują się w pliku: [**Konfiguracja**](Configuration.md).
