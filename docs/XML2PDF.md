[Powrót do strony głównej](../README.md)

# Polecenie: `XML2PDF`

Konwertuje poprawną fakturę KSeF w formacie XML (lub plik UPO - Urzędowego Poświadczenia Odbioru) na czytelny dla człowieka plik PDF.

Silnik generujący i renderujący widoki faktur do formatu PDF korzysta w dużej mierze z rozwiązań i szablonów open-source z zewnętrznego projektu `ksef-pdf-converter`. 

Zależnie od udostępnionych flag, do faktury można dołączyć kody QR i numer KSeF.

**Użycie:**
```bash
kcksefcli XML2PDF <plik-xml> [<plik-wyjsciowy-pdf>]
```

**Argumenty:**

| Argument               | Opis                                                      | Wymagane |
|------------------------|-----------------------------------------------------------|----------|
| `plik-xml`             | Ścieżka do istniejącego pliku wejściowego XML faktury.    | Tak      |
| `plik-wyjsciowy-pdf`   | Opcjonalna ścieżka dla docelowego pliku `.pdf`.           | Nie      |

**Opcje:**

| Opcja       | Opis                                                 |
|-------------|------------------------------------------------------|
| `--upo`     | Informuje konwerter, aby użył szablonu UPO zamiast FA. |
| `--nrKSeF`  | Numer nadany przez KSeF fakturze, dołączany na wydruku w PDF. |
| `--qrCode`  | Bezpośredni URL z kodem QR do osadzenia w wydruku.   |
| `--qrCode2` | Drugi URL z kodem QR do osadzenia w dokumencie PDF.   |
