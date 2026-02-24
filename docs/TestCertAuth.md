[Powrót do strony głównej](../README.md)

# Polecenie: `TestCertAuth`

> [!IMPORTANT]
> **Komenda testowa:** To polecenie służy wyłącznie do ręcznego testowania procesu uwierzytelniania. W normalnej pracy z narzędziem nie ma potrzeby wywoływania go jawnie. Aplikacja `kcksefcli` automatycznie zarządza procesem logowania, pobieraniem tokenów sesyjnych oraz ich odświeżaniem w tle przed wykonaniem jakiejkolwiek innej operacji (np. wysyłki lub szukania faktur).
>
> Ta komenda może zostać usunięta w przyszłych wersjach narzędzia.
> Więcej o automatycznym zarządzaniu sesją znajdziesz w dokumencie: [**Konfiguracja**](Configuration.md).

Wymusza uwierzytelnienie za pomocą certyfikatu kwalifikowanego z aktywnego profilu. Profil musi zawierać sekcję `certificate`.

**Użycie:**
```bash
kcksefcli -a profil_z_certyfikatem TestCertAuth
```

---


## Konfiguracja i Uwierzytelnianie

To polecenie łączy się z serwerami KSeF i w pełni obsługuje system profili, opcje konfiguracji (`kcksefcli.yaml`) oraz automatycznej pamięci podręcznej (cache) tokenów sesyjnych.
Szczegółowe informacje o zarządzaniu sesją, przełączaniu profili i środowisk znajdują się w pliku: [**Konfiguracja**](Configuration.md).
