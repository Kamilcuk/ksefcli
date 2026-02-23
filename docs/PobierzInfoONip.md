[Powrót do strony głównej](../README.md)

# Polecenie: `PobierzInfoONip`

Wyszukuje informacje z rządowego interfejsu API Krajowej Administracji Skarbowej, w celu weryfikacji szczegółów (nazwa, adres i REGON) zarejestrowanych podatników, bazując na określonym numerze identyfikacji podatkowej (NIP).

Zwraca na standardowe wyjście pełny JSON z danymi o podmiocie powiązanym z podanym numerem NIP w danym dniu.

**Użycie:**
```bash
kcksefcli PobierzInfoONip <numer-nip> [--data <data>]
```

**Argumenty:**

| Argument        | Opis                                    | Wymagane |
|-----------------|-----------------------------------------|----------|
| `numer-nip`     | Numer NIP do wyszukania w rejestrze KAS.| Tak      |

**Opcje:**

| Opcja           | Opis                                                                                                          | Domyślnie    |
|-----------------|---------------------------------------------------------------------------------------------------------------|--------------|
| `--data`        | Wyszukiwanie danych z rejestru z wybranego dnia w przeszłości w formacie (YYYY-MM-DD). Jeśli nie podano, użyje dzisiejszej daty. | Dziś         |
