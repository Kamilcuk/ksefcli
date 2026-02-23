[Powrót do strony głównej](../README.md)

# Polecenie: `NowaFaktura`

Generuje nową, prostą fakturę XML zgodną ze standardem KSeF na podstawie wejściowego pliku specyfikacji zapisanego w formacie YAML. Narzędzie automatycznie mapuje przyjazną składnię YAML na skomplikowane drzewo XML oczekiwane przez system e-Faktur. Automatycznie też weryfikuje wygenerowaną strukturę względem schemy (o ile walidacja nie zostanie wyłączona opcją).

**Użycie:**
```bash
kcksefcli NowaFaktura <plik-yaml> <plik-wyjsciowy-xml>
```

**Argumenty:**

| Argument               | Opis                                                 | Wymagane |
|------------------------|------------------------------------------------------|----------|
| `plik-yaml`            | Ścieżka do wejściowego pliku w formacie YAML.        | Tak      |
| `plik-wyjsciowy-xml`   | Ścieżka do utworzonego na dysku pliku faktury w XML. | Tak      |

**Opcje:**

| Opcja               | Opis                                             | Domyślnie |
|---------------------|--------------------------------------------------|-----------|
| `--bez-walidacji`   | Pomija walidację XML po utworzeniu pliku.        | False     |

## Format pliku YAML dla NowaFaktura

Polecenie `NowaFaktura` przyjmuje jako argument plik w formacie YAML definiujący fakturę. 

**Uwaga do sekcji Nabywcy (Kupujący):** Jeśli dla kupującego nie zostanie podany `Nip` oraz `NrID`, system uzna, że dokument jest wystawiany dla **osoby fizycznej** nieprowadzącej działalności gospodarczej, przypisując w wygenerowanym dokumencie element `<BrakID>1</BrakID>` zgodnie z wymaganiami KSeF.

Przykład struktury pliku YAML:

```yaml
Sprzedawca:
  Nip: "5260202588"
  Nazwa: "Firma Sprzedawca" # Opcjonalnie, zostanie pobrane automatycznie z rejestru NIP jeśli puste
  Adres: "ul. Prosta 1, 00-001 Warszawa" # Opcjonalnie, pobierane jw.
Kupujący:
  Nip: "5223217667" # Może być Nip lub NrID lub całkowity brak
  NrID: "1234567890" # Alternatywa dla Nip
  Nazwa: "Klient Kupujący"
DataWykonania: "2026-02-15" # Opcjonalnie, mapowane na P_6 (domyślnie data dzisiejsza)
DodatkowyOpis: # Opcjonalna sekcja dla dodatkowych opisów faktury
  - Klucz: "Klucz1"
    Wartosc: "Wartosc1"
Pozycje:
  - Nazwa: "Usługa IT"
    Jednostka: "godz" # Domyślnie "" (jeśli puste, P_8A nie pojawi się w XML)
    Ilosc: 1 # Opcjonalnie (jeśli puste, P_8B nie pojawi się w XML)
    StawkaPodatku: "23" # Opcjonalnie, domyślnie "23" (użyj "odwrotne obciążenie" lub "oo" dla oo)
    WartoscBrutto: 1230.00
```
