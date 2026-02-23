[Powrót do strony głównej](../README.md)

# Polecenie: `WeryfikujXML`

Wykonuje walidację lokalnego pliku faktury XML z wbudowanym schematem XSD opublikowanym przez Ministerstwo Finansów. Służy do upewniania się, że wygenerowana faktura jest zgodna z oficjalnymi strukturami logicznymi KSeF (weryfikuje wprost **schemat FA-3**, wersja 1-0E), co jest bezwzględnie wymagane przed próbą wgrania pliku na serwery.

W przypadku wystąpienia błędów walidacji ich specyfika zostanie wypisana na standardowe wyjście błędów (stderr).

**Użycie:**
```bash
kcksefcli WeryfikujXML <plik-wejsciowy-xml>
```

**Argumenty:**

| Argument             | Opis                                                 | Wymagane |
|----------------------|------------------------------------------------------|----------|
| `plik-wejsciowy-xml` | Ścieżka do utworzonego na dysku pliku faktury w XML. | Tak      |
