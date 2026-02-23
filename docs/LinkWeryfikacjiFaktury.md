[Powrót do strony głównej](../README.md)

# Polecenie: `LinkWeryfikacjiFaktury`

> [!NOTE]
> To polecenie w pełni działa w trybie **offline** i **nie łączy się** z serwerami KSeF. Opiera się na kluczu prywatnym i certyfikacie wskazanym w profilu (w sekcji `certificate`). Mechanizm ten nie weryfikuje tokenów ani nie wykonuje żadnych zapytów sieciowych w kierunku infrastruktury Ministerstwa Finansów. Generowany link służy jako KOD II do weryfikacji.


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

To polecenie **nie łączy się** z serwerami KSeF, działa w pełni lokalnie. Jednak wymaga systemu profili i opcji konfiguracji (`kcksefcli.yaml`), aby uzyskać dostęp do zdefiniowanego w profilu klucza prywatnego certyfikatu, który jest niezbędny do wygenerowania ważnego weryfikacyjnie skrótu kryptograficznego (KOD II).
Szczegółowe informacje o zarządzaniu sesją, przełączaniu profili i środowisk znajdują się w pliku: [**Konfiguracja**](Configuration.md).
