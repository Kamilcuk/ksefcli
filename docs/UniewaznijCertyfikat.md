[Powrót do strony głównej](../README.md)

# Polecenie: `UniewaznijCertyfikat`

Unieważnia certyfikat KSeF.

**Użycie:**
```bash
kcksefcli UniewaznijCertyfikat <numer-seryjny>
```

**Argumenty:**

| Argument        | Opis                                      | Wymagane |
|-----------------|-------------------------------------------|----------|
| `numer-seryjny` | Numer seryjny certyfikatu.                | Tak      |

**Opcje:**

| Opcja      | Opis                                                                                                          | Domyślnie |
|------------|---------------------------------------------------------------------------------------------------------------|-----------|
| `--reason` | Powód unieważnienia: `KeyCompromise`, `AffiliationChanged`, `Superseded`, `CessationOfOperation`, `Other`.    | `Other`   |

---


## Konfiguracja i Uwierzytelnianie

To polecenie łączy się z serwerami KSeF i w pełni obsługuje system profili, opcje konfiguracji (`kcksefcli.yaml`) oraz automatycznej pamięci podręcznej (cache) tokenów sesyjnych.
Szczegółowe informacje o zarządzaniu sesją, przełączaniu profili i środowisk znajdują się w pliku: [**Konfiguracja**](Configuration.md).
