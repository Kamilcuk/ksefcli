[Powrót do strony głównej](../README.md)

# Polecenie: `PrintConfig`

Wypisuje aktywną konfigurację w formacie YAML (domyślnie) lub JSON (z opcją `--json`).

**Użycie:**
```bash
kcksefcli PrintConfig [--json]
```

**Opcje:**

| Opcja       | Opis                                | Domyślnie |
|-------------|-------------------------------------|-----------|
| `--json`    | Wypisuje konfigurację w formacie JSON. | `false`   |

---


## Konfiguracja i Uwierzytelnianie

To polecenie łączy się z serwerami KSeF i w pełni obsługuje system profili, opcje konfiguracji (`kcksefcli.yaml`) oraz automatycznej pamięci podręcznej (cache) tokenów sesyjnych.
Szczegółowe informacje o zarządzaniu sesją, przełączaniu profili i środowisk znajdują się w pliku: [**Konfiguracja**](Configuration.md).
