[Powrót do strony głównej](../README.md)

# Polecenie: `PrzeslijFaktury`

Wysyła faktury w formacie XML do KSeF.

**Użycie:**
```bash
kcksefcli PrzeslijFaktury faktura1.xml faktura2.xml --upodir /tmp/upo --upopdf
```

**Argumenty:**

| Argument      | Opis                                  | Wymagane |
|---------------|---------------------------------------|----------|
| `pliki`       | Ścieżki do plików XML z fakturami.    | Tak      |

**Opcje:**

> **Zalecenie:** Zawsze zaleca się podawanie flag `--upodir` oraz `--upopdf` przy wysyłaniu faktur. Dzięki temu narzędzie natychmiast zapisze Urzędowe Poświadczenie Odbioru (UPO) wraz z czytelnym wygenerowanym dokumentem PDF potwierdzającym poprawność wysyłki i nadanie numeru KSeF.

| Opcja              | Opis                                                | Wymagane |
|--------------------|-----------------------------------------------------|----------|
| `-u`, `--upodir`   | Katalog do zapisu plików UPO.                       | Nie      |
| `--upopdf`         | Konwertuje UPO od razu na format PDF.               | Nie      |
| `--uposesji`       | Zapisuje UPO sesji (zbiorcze UPO).                  | Nie      |
| `--offlinemode`    | Ustawia tryb offline dla sesji.                     | Nie      |

---


## Konfiguracja i Uwierzytelnianie

To polecenie łączy się z serwerami KSeF i w pełni obsługuje system profili, opcje konfiguracji (`kcksefcli.yaml`) oraz automatycznej pamięci podręcznej (cache) tokenów sesyjnych.
Szczegółowe informacje o zarządzaniu sesją, przełączaniu profili i środowisk znajdują się w pliku: [**Konfiguracja**](Configuration.md).
