[Powrót do strony głównej](../README.md)

# Polecenie: `QRWeryfikacjiFaktury`

Generuje kod QR weryfikacyjny (KOD II) dla faktury i zapisuje go do pliku.

**Użycie:**
```bash
kcksefcli QRWeryfikacjiFaktury faktura.xml kod.png
```

**Argumenty:**

| Argument      | Opis                                   | Wymagane |
|---------------|----------------------------------------|----------|
| `InputFile`   | Ścieżka do pliku XML z fakturą.        | Tak      |
| `OutputPath`  | Ścieżka wyjściowa dla pliku QR (np. jpg). | Tak      |

**Opcje:**

| Opcja            | Opis                           | Domyślnie |
|------------------|--------------------------------|-----------|
| `-p`, `--pixels` | Piksele na moduł dla kodu QR.  | `5`       |

---


## Konfiguracja i Uwierzytelnianie

To polecenie łączy się z serwerami KSeF i w pełni obsługuje system profili, opcje konfiguracji (`kcksefcli.yaml`) oraz automatycznej pamięci podręcznej (cache) tokenów sesyjnych.
Szczegółowe informacje o zarządzaniu sesją, przełączaniu profili i środowisk znajdują się w pliku: [**Konfiguracja**](Configuration.md).
