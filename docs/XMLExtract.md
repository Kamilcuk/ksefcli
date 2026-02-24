[Powrót do strony głównej](../README.md)

# Polecenie: `XMLExtract`

Ekstrahuje konkretną wartość z pliku XML na podstawie wyrażenia XPath. Przydatne do automatyzacji skryptów wokół plików faktur w KSeF.

Opcja `--no-strip-namespaces` wyłącza domyślne zachowanie usuwania wszystkich przestrzeni nazw z dokumentu przed ewaluacją XPath. Domyślnie przestrzenie nazw są usuwane, co znacznie upraszcza zapytania na dokumentach z ich użyciem, pozwalając na ich całkowite pominięcie.

**Użycie:**
```bash
kcksefcli XMLExtract <plik-xml> <wyrazenie-xpath>
```

**Przykłady użycia:**
```bash
# Klasyczne użycie (jeśli XML nie używa przestrzeni nazw lub polegasz na domyślnym ich usunięciu)
$ kcksefcli XMLExtract faktura.xml /Faktura/Fa/P_13_1
```

**Argumenty:**

| Argument             | Opis                                                                        | Wymagane |
|----------------------|-----------------------------------------------------------------------------|----------|
| `plik-xml`           | Ścieżka do istniejącego pliku wejściowego XML.                              | Tak      |
| `wyrazenie-xpath`    | Wyrażenie XPath wskazujące na element, z którego ma zostać pobrana wartość. | Tak      |

**Opcje:**

| Opcja                      | Opis                                                                                                                      | Domyślnie |
|----------------------------|---------------------------------------------------------------------------------------------------------------------------|-----------|
| `--namespaces`             | Lista przestrzeni nazw i prefiksów oddzielonych przecinkami, np. `ns=http://example.com,ns2=http://another.com`.         |           |
| `--no-strip-namespaces`    | Nie usuwa przestrzeni nazw z dokumentu przed ewaluacją XPath. Zachowuje oryginalne przestrzenie nazw w dokumencie. |           |
