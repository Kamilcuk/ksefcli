[Powrót do strony głównej](../README.md)

# Polecenie: `WystawPodobnaFakture`

Tworzy nową fakturę XML KSeF na podstawie istniejącego pliku faktury i aktualizuje wybrane daty. Skutecznie ułatwia tworzenie cyklicznych faktur na podstawie gotowego szablonu z przeszłości.

Uaktualnia automatycznie datę wytworzenia faktury, datę wystawienia (`P_1`), datę dokonania lub zakończenia dostawy towarów/wykonania usług (`P_6`) oraz numer faktury (`P_2`), o ile oryginalny numer faktury był w formacie `FV/yyyyMMdd/01`.

**Użycie:**
```bash
kcksefcli WystawPodobnaFakture <plik-wejsciowy-xml> <plik-wyjsciowy-xml> [--data-wystawienia <data>] [--data-wykonania <data>]
```

**Argumenty:**

| Argument             | Opis                                                   | Wymagane |
|----------------------|--------------------------------------------------------|----------|
| `plik-wejsciowy-xml` | Ścieżka do istniejącego pliku wejściowego XML faktury. | Tak      |
| `plik-wyjsciowy-xml` | Ścieżka dla nowo wygenerowanego pliku XML.             | Tak      |

**Opcje:**

| Opcja                  | Opis                                                                                          | Domyślnie    |
|------------------------|-----------------------------------------------------------------------------------------------|--------------|
| `--data-wystawienia`   | Nowa data wystawienia faktury (pole P_1). Format `yyyy-MM-dd`. Jeśli nie podano, użyje dzisiejszej daty. | Dziś         |
| `--data-wykonania`     | Nowa data wykonania usługi/dostawy (pole P_6). Format `yyyy-MM-dd`. Jeśli nie podano, użyje dzisiejszej daty. | Dziś         |
