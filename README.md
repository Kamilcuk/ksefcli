# kcksefcli

`kcksefcli` to narzędzie wiersza poleceń (CLI) dla systemu Linux, napisane w języku C#, które ułatwia interakcję z Krajowym Systemem e-Faktur (KSeF) w Polsce. Aplikacja wykorzystuje bibliotekę kliencką `ksef-client-csharp` do komunikacji z usługami KSeF.

## Spis Treści

- [Instalacja](#instalacja)
- [Przykłady użycia](#przykłady-użycia)
- [Konfiguracja](#konfiguracja)
  - [Struktura pliku `kcksefcli.yaml`](#struktura-pliku-kcksefcliyaml)
  - [Opcje Konfiguracyjne](#opcje-konfiguracyjne)
  - [Przykład Konfiguracji](#przykład-konfiguracji)
- [Użycie](#użycie)
  - [Opcje Globalne](#opcje-globalne)
  - [Dostępne Polecenia](#dostępne-polecenia)
- [Polecenia](#polecenia)
  - [`TestAuth`](docs/TestAuth.md)
  - [`TestCertAuth`](docs/TestCertAuth.md)
  - [`DodajPozycjeNaFakturze`](docs/DodajPozycjeNaFakturze.md)
  - [`GetFaktura`](docs/GetFaktura.md)
  - [`LinkDoFaktury`](docs/LinkDoFaktury.md)
  - [`LinkWeryfikacjiFaktury`](docs/LinkWeryfikacjiFaktury.md)
  - [`NowyCertyfikat`](docs/NowyCertyfikat.md)
  - [`NowaFaktura`](docs/NowaFaktura.md)
  - [`ParseDate`](docs/ParseDate.md)
  - [`PobierzCertyfikat`](docs/PobierzCertyfikat.md)
  - [`PobierzInfoONip`](docs/PobierzInfoONip.md)
  - [`PobierzFaktury`](docs/PobierzFaktury.md)
  - [`PokazLimity`](docs/PokazLimity.md)
  - [`PrintConfig`](docs/PrintConfig.md)
  - [`PrzeslijFaktury`](docs/PrzeslijFaktury.md)
  - [`QRDoFaktury`](docs/QRDoFaktury.md)
  - [`QRWeryfikacjiFaktury`](docs/QRWeryfikacjiFaktury.md)
  - [`SelfUpdate`](docs/SelfUpdate.md)
  - [`SprawdzLimitCertyfikatow`](docs/SprawdzLimitCertyfikatow.md)
  - [`SzukajFaktur`](docs/SzukajFaktur.md)
  - [`TestTokenAuth`](docs/TestTokenAuth.md)
  - [`TokenRefresh`](docs/TokenRefresh.md)
  - [`UniewaznijCertyfikat`](docs/UniewaznijCertyfikat.md)
  - [`WeryfikujXML`](docs/WeryfikujXML.md)
  - [`WylistujCertyfikaty`](docs/WylistujCertyfikaty.md)
  - [`WystawFaktureOffline`](docs/WystawFaktureOffline.md)
  - [`WystawPodobnaFakture`](docs/WystawPodobnaFakture.md)
  - [`WystawKorekte`](docs/WystawKorekte.md)
  - [`XMLExtract`](docs/XMLExtract.md)
  - [`XMLRemoveNamespace`](docs/XMLRemoveNamespace.md)
  - [`XML2PDF`](docs/XML2PDF.md)
- [Rozwój](#rozwój)
- [Uwierzytelnianie w KSeF](#uwierzytelnianie-w-ksef)
- [Autor i Licencja](#autor-i-licencja)

## Instalacja

Możesz pobrać statycznie linkowaną binarkę `kcksefcli` bezpośrednio z artefaktów GitLab CI/CD, a następnie umieścić ją w katalogu znajdującym się w `PATH` (np. `~/.local/bin`).

Poniższy link jest przeznaczony dla systemu Linux.

```bash
mkdir -p ~/.local/bin
curl -LsS https://gitlab.com/kamcuk/kcksefcli/builds/artifacts/main/download?job=linux_build_main | zcat > ~/.local/bin/kcksefcli
chmod +x ~/.local/bin/kcksefcli
export PATH="$HOME/.local/bin:$PATH"
```

### Bezpośrednie linki do pobrania

- [Linux x64](https://gitlab.com/kamcuk/kcksefcli/-/jobs/artifacts/main/raw/kcksefcli?job=linux_build_main)
- [Windows x64](https://gitlab.com/kamcuk/kcksefcli/-/jobs/artifacts/main/raw/kcksefcli.exe?job=windows_build_main)


## Przykłady użycia

Wyszukiwanie numeru KSeF dla faktury o konkretnym numerze:
```bash
$ kcksefcli SzukajFaktur -q -c kcksefcli.yaml --from "-1week" --to "now" --invoiceNumber '0004/26' | jq -r '.Invoices[0].KsefNumber'
12312312312-20260117-XXXXXXXXXXXX-5C
```

Pobieranie wszystkich faktur zakupowych z ostatniego miesiąca do wskazanego katalogu w formacie XML i PDF:
```bash
$ kcksefcli PobierzFaktury --from "-1month" --subjectType Subject2 --outputdir ./faktury_zakupowe --pdf
```

Przesyłanie faktury z użyciem konkretnego profilu:
```bash
$ kcksefcli PrzeslijFaktury -c kcksefcli.yaml -f d03900-001.xml  -a firma2
```

Wyszukiwanie faktur wystawionych w ostatnim tygodniu i zapisanie wyników do pliku:
```bash
$ kcksefcli SzukajFaktur -c kcksefcli.yaml --from "-1week" --to "now" > /tmp/1.json
```

## Konfiguracja
Szczegóły konfiguracji opisano w pliku [Konfiguracja](docs/Configuration.md).
**Utworzenie pliku konfiguracyjnego z poświadczeniami jest niezbędne, aby korzystać z komend łączących się bezpośrednio z serwerami KSeF.**

## Użycie

Ogólna składnia poleceń `kcksefcli` jest następująca:

```bash
kcksefcli <polecenie> [opcje]
```

Szczegółowy opis konfiguracji profili, globalnych opcji i pamięci podręcznej znajdziesz w dokumencie: [**Konfiguracja**](docs/Configuration.md).



## Polecenia

  - [`DodajPozycjeNaFakturze`](docs/DodajPozycjeNaFakturze.md)
  - [`GetFaktura`](docs/GetFaktura.md)
  - [`LinkDoFaktury`](docs/LinkDoFaktury.md)
  - [`LinkWeryfikacjiFaktury`](docs/LinkWeryfikacjiFaktury.md)
  - [`NowaFaktura`](docs/NowaFaktura.md)
  - [`NowyCertyfikat`](docs/NowyCertyfikat.md)
  - [`ParseDate`](docs/ParseDate.md)
  - [`PobierzCertyfikat`](docs/PobierzCertyfikat.md)
  - [`PobierzFaktury`](docs/PobierzFaktury.md)
  - [`PobierzInfoONip`](docs/PobierzInfoONip.md)
  - [`PokazLimity`](docs/PokazLimity.md)
  - [`PrintConfig`](docs/PrintConfig.md)
  - [`PrzeslijFaktury`](docs/PrzeslijFaktury.md)
  - [`QRDoFaktury`](docs/QRDoFaktury.md)
  - [`QRWeryfikacjiFaktury`](docs/QRWeryfikacjiFaktury.md)
  - [`SelfUpdate`](docs/SelfUpdate.md)
  - [`SprawdzLimitCertyfikatow`](docs/SprawdzLimitCertyfikatow.md)
  - [`SzukajFaktur`](docs/SzukajFaktur.md)
  - [`TestAuth`](docs/TestAuth.md)
  - [`TestCertAuth`](docs/TestCertAuth.md)
  - [`TestTokenAuth`](docs/TestTokenAuth.md)
  - [`TokenRefresh`](docs/TokenRefresh.md)
  - [`UniewaznijCertyfikat`](docs/UniewaznijCertyfikat.md)
  - [`WeryfikujXML`](docs/WeryfikujXML.md)
  - [`WylistujCertyfikaty`](docs/WylistujCertyfikaty.md)
  - [`WystawFaktureOffline`](docs/WystawFaktureOffline.md)
  - [`WystawPodobnaFakture`](docs/WystawPodobnaFakture.md)
  - [`XML2PDF`](docs/XML2PDF.md)
  - [`XMLExtract`](docs/XMLExtract.md)
  - [`XMLRemoveNamespace`](docs/XMLRemoveNamespace.md) - Usuwa przestrzenie nazw (namespaces) z faktur KSeF, czyniąc pliki XML bardziej czytelnymi dla człowieka i łatwiejszymi do przetwarzania prostymi narzędziami.

## Rozwój

Rozwój odbywa się na GitLabie.

Aby skonfigurować środowisko deweloperskie i uruchomić aplikację, wykonaj następujące kroki:

```bash
# Sklonuj repozytorium
git clone https://gitlab.com/kamcuk/kcksefcli.git
cd kcksefcli

# Inicjalizacja i pobranie zawartości niezbędnych submodułów (zależności)
git submodule update --init --recursive

# Pobranie paczek .NET i budowa projektu
dotnet build

# Uruchomienie aplikacji
dotnet run --project src/KCKSeFCli -- <polecenie> [opcje]
```

## Uwierzytelnianie w KSeF

Szczegółowe informacje na temat mechanizmów uwierzytelniania w Krajowym Systemie e-Faktur można znaleźć w oficjalnej dokumentacji: [Uwierzytelnianie w KSeF](https://github.com/CIRFMF/ksef-docs/blob/main/uwierzytelnianie.md).

Dokumentacja KSeF API: [https://api-test.ksef.mf.gov.pl/docs/v2/index.html](https://api-test.ksef.mf.gov.pl/docs/v2/index.html).

Artykuł o problemach z namespace w KSeF: [https://ksbot.pl/api/ksef-api-xml-namespace-problemy/](https://ksbot.pl/api/ksef-api-xml-namespace-problemy/).

## Autor i Licencja

Program napisany przez Kamila Cukrowskiego.
Licencja: [GPLv3](LICENSE.md).
