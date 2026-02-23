[Powrót do strony głównej](../README.md)

# Polecenie: `QRDoFaktury`

Generuje kod QR dla linku weryfikacyjnego faktury i zapisuje go do pliku.

**Użycie:**
```bash
kcksefcli QRDoFaktury <ksef-numer> faktura-qr.png
```

**Argumenty:**

| Argument        | Opis                                      | Wymagane |
|-----------------|-------------------------------------------|----------|
| `ksef-numer`    | Numer KSeF faktury.                       | Tak      |
| `output-path`   | Ścieżka pliku wyjściowego dla kodu QR.    | Tak      |

**Opcje:**

| Opcja            | Opis                                 | Domyślnie |
|------------------|--------------------------------------|-----------|
| `-p`, `--pixels` | Piksele na moduł dla kodu QR.        | `5`       |

---


## Konfiguracja i Uwierzytelnianie

To polecenie łączy się z serwerami KSeF i w pełni obsługuje system profili, opcje konfiguracji (`kcksefcli.yaml`) oraz automatycznej pamięci podręcznej (cache) tokenów sesyjnych.
Szczegółowe informacje o zarządzaniu sesją, przełączaniu profili i środowisk znajdują się w pliku: [**Konfiguracja**](Configuration.md).
