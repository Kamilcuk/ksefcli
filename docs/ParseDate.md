[Powrót do strony głównej](../README.md)

# Polecenie: `ParseDate`

Parsuje ciąg znaków daty i wypisuje go w formacie ISO 8601 lub jako liczbę sekund od epoki Uniksa.

Kolejność parsowania:
1.  Standardowe parsowanie C# (`DateTime.TryParse`, `DateTime.TryParseExact`).
2.  Względne daty w formacie `-<liczba>(day|days|dzien|dzień|dni|week|weeks|tydzień|tygodni)`.
3.  Biblioteka `HumanDateParser` (np. `1 month ago`).
4.  Narzędzie systemowe GNU `date` (fallback).

**Użycie:**
```bash
kcksefcli ParseDate "<ciąg-daty>" [--seconds]
```

**Przykłady użycia:**
```bash
$ kcksefcli ParseDate "2024-01-02"
2024-01-02T00:00:00.000000
$ kcksefcli ParseDate "-1week"
2024-02-11T10:30:00.000000
$ kcksefcli ParseDate "yesterday" --seconds
1708137600.000000
```

**Argumenty:**

| Argument        | Opis                               | Wymagane |
|-----------------|------------------------------------|----------|
| `dateString`    | Ciąg znaków daty do sparsowania.   | Tak      |

**Opcje:**

| Opcja       | Opis                                                 |
|-------------|------------------------------------------------------|
| `--seconds` | Wypisuje zmiennoprzecinkową liczbę sekund od epoki Uniksa. |

---
