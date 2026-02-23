[Powrót do strony głównej](../README.md)

# Polecenie: `QRWeryfikacjiFaktury`

> [!NOTE]
> To polecenie działa w trybie **offline** i **nie łączy się** bezpośrednio z serwerami KSeF. Wykorzystuje dane klucza prywatnego certyfikatu zdefiniowane w profilu (`kcksefcli.yaml`), aby złożyć odpowiedni podpis bez wywoływania zapytań sieciowych.


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

To polecenie **nie łączy się** z serwerami KSeF, działa w pełni lokalnie. System profili i obsługa plików (`kcksefcli.yaml`) jest jednak potrzebna po to, aby uzyskać dostęp do zdefiniowanego w profilu klucza prywatnego, by poprawnie podpisać wystawiony element offline.
Szczegółowe informacje o zarządzaniu sesją, przełączaniu profili i środowisk znajdują się w pliku: [**Konfiguracja**](Configuration.md).
