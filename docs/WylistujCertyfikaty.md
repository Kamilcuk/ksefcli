[Powrót do strony głównej](../README.md)

# Polecenie: `WylistujCertyfikaty`

Listuje metadane certyfikatów KSeF.

**Użycie:**
```bash
kcksefcli WylistujCertyfikaty
```

**Opcje:**

| Opcja            | Opis                                      |
|------------------|-------------------------------------------|
| `--name`         | Filtrowanie po nazwie certyfikatu.        |
| `--serialNumber` | Filtrowanie po numerze seryjnym.          |

---


## Konfiguracja i Uwierzytelnianie

To polecenie łączy się z serwerami KSeF i w pełni obsługuje system profili, opcje konfiguracji (`kcksefcli.yaml`) oraz automatycznej pamięci podręcznej (cache) tokenów sesyjnych.
Szczegółowe informacje o zarządzaniu sesją, przełączaniu profili i środowisk znajdują się w pliku: [**Konfiguracja**](Configuration.md).
