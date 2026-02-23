[Powrót do strony głównej](../README.md)

# Polecenie: `XMLExtract`

Ekstrahuje konkretną wartość z pliku XML na podstawie wyrażenia XPath. Przydatne do automatyzacji skryptów wokół plików faktur w KSeF.

Opcja `-s` / `--strip-namespaces` usuwa wszystkie przestrzenie nazw z dokumentu przed ewaluacją XPath, co znacznie upraszcza zapytania na dokumentach korzystających z przestrzeni nazw, pozwalając na ich całkowite pominięcie.

**Użycie:**
```bash
kcksefcli XMLExtract <plik-xml> <wyrazenie-xpath>
```

**Przykłady użycia:**
```bash
# Klasyczne użycie (jeśli XML nie używa przestrzeni nazw lub korzystasz z opcji -s)
$ kcksefcli XMLExtract -s faktura.xml /Faktura/Fa/P_13_1
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
| `-s, --strip-namespaces` | Oczyszcza dokument ze wszystkich zadeklarowanych przestrzeni nazw przed wykonaniem wyszukiwania XPath, upraszczając ścieżkę. |           |
