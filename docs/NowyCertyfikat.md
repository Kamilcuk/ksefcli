[Powrót do strony głównej](../README.md)

# Polecenie: `NowyCertyfikat`

Tworzy wniosek o generowanie nowego certyfikatu KSeF poprzez wywołanie endpointu API z żądaniem utworzenia kluczy, opcjonalnie dając możliwość zapisania lokalnego pliku CSR (żądania certyfikatu) i samego wystawionego certyfikatu oraz odpowiadającego mu klucza prywatnego (zakodowanego w Base64).

**Użycie:**
```bash
kcksefcli NowyCertyfikat --certificateName "NowyCert2026"
```

**Opcje specyficzne:**

| Opcja                     | Opis                                                                                                                      | Domyślnie        |
|---------------------------|---------------------------------------------------------------------------------------------------------------------------|------------------|
| `--certificateName`       | Wymagane. Nazwa dla wydawanego certyfikatu.                                                                               |                  |
| `--certificateType`       | Typ certyfikatu (`Authentication` lub `Offline`).                                                                         | `Authentication` |
| `--csrOutputPath`         | Ścieżka pliku wyjściowego, w którym zostanie zapisany wygenerowany na platformie podpis CSR (zakodowany w Base64).        |                  |
| `--privateKeyOutputPath`  | Ścieżka pliku wyjściowego, w którym zostanie zapisany klucz prywatny wygenerowany lokalnie przez narzędzie (w Base64).    |                  |
| `--certificateOutputPath` | Ścieżka pliku wyjściowego dla wystawionego i uzyskanego certyfikatu (zakodowanego w Base64).                              |                  |
| `--validFrom`             | Data początkowa ważności certyfikatu (np. `2026-01-01`). Jeśli nie podano, wniosek zadziała od daty bieżącej serwera.    | Data dzisiejsza  |

*Polecenie obsługuje również wszystkie ogólne opcje konfiguracyjne np. do podawania poświadczeń.*


## Konfiguracja i Uwierzytelnianie

To polecenie łączy się z serwerami KSeF i w pełni obsługuje system profili, opcje konfiguracji (`kcksefcli.yaml`) oraz automatycznej pamięci podręcznej (cache) tokenów sesyjnych.
Szczegółowe informacje o zarządzaniu sesją, przełączaniu profili i środowisk znajdują się w pliku: [**Konfiguracja**](Configuration.md).
