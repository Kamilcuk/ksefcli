[Powrót do strony głównej](../README.md)

# Polecenie: `SelfUpdate`

Aktualizuje narzędzie `kcksefcli` do najnowszej stabilnej wersji, pobierając binarkę z repozytorium GitLab CI/CD.

**Użycie:**
```bash
kcksefcli SelfUpdate [--url <adres-url-binarki>]
```

**Opcje:**

| Opcja            | Opis                                                                                   | Domyślnie |
|------------------|----------------------------------------------------------------------------------------|-----------|
| `-d`, `--destination` | Zapisuje nową wersję do określonej ścieżki zamiast zastępować bieżący plik wykonywalny. | Bieżący plik wykonywalny |
| `--url`          | Określa niestandardowy adres URL do pobrania binarnego pliku aktualizacji.              | Automatycznie wykrywany na podstawie platformy |

---
