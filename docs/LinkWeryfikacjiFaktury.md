[Powrót do strony głównej](../README.md)

# Polecenie: `LinkWeryfikacjiFaktury`

Generuje link weryfikacji faktury (KOD II).

**Użycie:**
```bash
kcksefcli LinkWeryfikacjiFaktury faktura.xml
```

**Argumenty:**

| Argument   | Opis                             | Wymagane |
|------------|----------------------------------|----------|
| `FilePath` | Ścieżka do pliku XML z fakturą.  | Tak      |


## Konfiguracja i Uwierzytelnianie

To polecenie łączy się z serwerami KSeF i w pełni obsługuje system profili, opcje konfiguracji (`kcksefcli.yaml`) oraz automatycznej pamięci podręcznej (cache) tokenów sesyjnych.
Szczegółowe informacje o zarządzaniu sesją, przełączaniu profili i środowisk znajdują się w pliku: [**Konfiguracja**](Configuration.md).
