[Powrót do strony głównej](../README.md)

# Polecenie: `XMLRemoveNamespace`

Usuwa wszystkie przestrzenie nazw z elementów w pliku XML faktury KSeF i ustawia domyślną przestrzeń nazw dla każdego elementu. Wynikowy XML jest zapisywany do pliku wyjściowego.

**Użycie:**
```bash
kcksefcli XMLRemoveNamespace <plik_wejściowy.xml> <plik_wyjściowy.xml>
```

**Argumenty Pozycyjne:**

| Argument              | Opis                                     |
|-----------------------|------------------------------------------|
| `plik_wejściowy.xml`  | Ścieżka do wejściowego pliku XML faktury. |
| `plik_wyjściowy.xml`  | Ścieżka do wyjściowego pliku XML, gdzie zostanie zapisana faktura bez przestrzeni nazw. |

**Przykład:**

Usuwa przestrzenie nazw z `faktura_z_namespace.xml` i zapisuje wynik do `faktura_bez_namespace.xml`.

```bash
kcksefcli XMLRemoveNamespace faktura_z_namespace.xml faktura_bez_namespace.xml
```
