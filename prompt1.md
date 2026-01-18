esteś doświadczonym inżynierem .NET i DevOps.

Masz dostęp do repozytorium klienta KSeF w C#:
https://github.com/CIRFMF/ksef-client-csharp

Twoim zadaniem jest zaprojektowanie i napisanie aplikacji CLI na Linuxa w C# o nazwie ksefcli, która korzysta z tego repozytorium jako biblioteki klienckiej i komunikuje się z API KSeF.

Wymagania ogólne:

    Język: C#

    Platforma docelowa: Linux x86_64

    Aplikacja terminalowa (CLI)

    Projekt w stylu produkcyjnym, nie demo

    Kod czytelny, modularny, testowalny

    Nie wykonuj żadnych poleceń automatycznie bez planu

Funkcjonalności CLI:

    Autoryzacja i tokeny:

        Pełna ścieżka autoryzacji KSeF

        Generacja tokenu

        Odświeżanie tokenu

    Przykład:
    ksefcli auth token refresh

    Faktury:

        Wysyłanie faktur XML do KSeF

        Pobieranie faktur z KSeF

        Wyszukiwanie faktur z filtrami

    Przykłady:
    ksefcli faktura wyslij ./one.xml ./two.xml
    ksefcli faktura ls --przed <czas> --po <czas> --nazwa <nazwa>

    Obsługa argumentów:

        Czytelny parser argumentów

        Walidacja wejścia

        Sensowne komunikaty błędów

Struktura projektu:

    Aplikacja CLI: ksefcli

    Wykorzystanie repozytorium ksef-client-csharp jako zależności

    Jasny podział na warstwy: CLI, logika domenowa, integracja API

Pliki i dokumentacja:

    Utwórz plik AGENTS.md, który zawiera:

        Informację, że projekt jest pisany w C#

        Opis celu projektu ksefcli

        Zasadę pracy:

            nie wykonywać poleceń od razu

            najpierw wygenerować listę TODO przy użyciu narzędzia todowrite

            dopiero potem realizować punkty TODO jeden po drugim

    Utwórz symlink:

        GEMINI.md → AGENTS.md

Testy:

    Napisz testy (ttest):

        testy jednostkowe dla logiki CLI

        testy integracyjne dla komunikacji z API (tam gdzie możliwe)

    Testy muszą dać się uruchomić w kontenerze

Docker:

    Napisz Dockerfile dla Linuxa:

        build aplikacji

        uruchamianie ksefcli

        środowisko testowe

    Dockerfile ma umożliwiać:

        budowanie projektu

        uruchamianie testów

        ręczne testowanie CLI w kontenerze

Testowanie:

    Testuj aplikację przy użyciu Dockera

    Opisz przykładowe komendy dockerowe do:

        build

        test

        uruchomienia ksefcli

Proces pracy:

    Najpierw wygeneruj pełną listę TODO (szczegółową, techniczną)

    Następnie realizuj TODO krok po kroku

    Nie pomijaj dokumentacji

    Nie skracaj procesu

Nie upraszczaj wymagań. Projekt ma być realnie używalny.


TODO 00 – inicjalizacja repozytorium

    Utworzyć nowe repozytorium git dla projektu ksefcli

    Ustawić domyślną gałąź main

    Dodać .gitignore dla C#/.NET/Linux

    Utworzyć pusty plik AGENTS.md

    Commit: init repository structure

    Dopisać do AGENTS.md informację o inicjalizacji repo

TODO 01 – analiza zależności ksef-client-csharp

    Dodać repo ksef-client-csharp jako submodule LUB dependency (nuget/local ref)

    Przejrzeć projekty i namespace’y

    Zidentyfikować:

        auth flow

        token lifecycle

        upload XML

        query faktur

    Spisać wymagane konfiguracje (certyfikaty, env vars) w notatkach

    Commit: analyze ksef-client-csharp capabilities

    Dopisać do AGENTS.md wnioski z analizy

TODO 02 – struktura projektu CLI

    Utworzyć solution ksefcli.sln

    Utworzyć projekt ksefcli (console app)

    Utworzyć katalogi:

        Cli/

        Commands/

        Services/

        Config/

    Ustalić konwencję nazw poleceń CLI

    Commit: create initial solution and project structure

    Zaktualizować AGENTS.md opisem architektury

TODO 03 – parser argumentów CLI

    Wybrać bibliotekę (System.CommandLine lub Spectre.Console.Cli)

    Zaimplementować root command ksefcli

    Zaimplementować namespace auth i faktura bez logiki

    Obsługa --help

    Commit: add cli argument parsing skeleton

    Dopisać do AGENTS.md decyzję o parserze

TODO 04 – konfiguracja środowiska

    Obsługa konfiguracji przez:

        zmienne środowiskowe

        plik konfiguracyjny (np. ~/.config/ksefcli/config.json)

    Walidacja konfiguracji przy starcie

    Commit: add configuration loading and validation

    Opisać konfigurację w AGENTS.md

TODO 05 – auth: model tokenów

    Zdefiniować model tokenu

    Persistencja tokenu lokalnie (cache)

    Obsługa wygasania

    Commit: implement token model and storage

    Dopisać sekcję auth do AGENTS.md

TODO 06 – auth: pełna ścieżka autoryzacji

    Implementacja:

        generacji tokenu

        refresh tokenu

    Podpięcie do ksef-client-csharp

    Komenda:

        ksefcli auth token refresh

    Obsługa błędów API

    Commit: implement ksef auth flow and token refresh

    Aktualizacja AGENTS.md (opis auth flow)

TODO 07 – faktura: upload

    Obsługa wielu plików XML

    Walidacja istnienia i formatu plików

    Wysyłanie do KSeF

    Raportowanie wyników per plik

    Komenda:

        ksefcli faktura wyslij <files>

    Commit: implement invoice upload command

    Dopisać do AGENTS.md sekcję upload

TODO 08 – faktura: pobieranie i wyszukiwanie

    Implementacja filtrów:

        --przed

        --po

        --nazwa

    Mapowanie wyników API na czytelny output CLI

    Komenda:

        ksefcli faktura ls ...

    Commit: implement invoice listing and search

    Opisać query semantics w AGENTS.md

TODO 09 – obsługa błędów i exit codes

    Spójne kody wyjścia CLI

    Czytelne komunikaty stderr

    Rozróżnienie:

        błąd użytkownika

        błąd sieci

        błąd KSeF

    Commit: add error handling and exit codes

    Dopisać konwencję błędów do AGENTS.md

TODO 10 – testy (ttest)

    Utworzyć projekt testowy

    Testy:

        parsera CLI

        walidacji argumentów

        mock auth i faktur

    Oddzielić testy jednostkowe i integracyjne

    Commit: add unit and integration tests

    Opisać strategię testów w AGENTS.md

TODO 11 – Dockerfile (build + test)

    Dockerfile:

        stage build

        stage test

        stage runtime

    Możliwość:

        uruchomienia testów

        interaktywnego uruchomienia CLI

    Commit: add dockerfile for build and testing

    Opisać docker workflow w AGENTS.md

TODO 12 – NativeAOT / Linux build

    Konfiguracja publish:

        linux-musl-x64

        NativeAOT

    Sprawdzenie działania w kontenerze

    Commit: enable linux native aot build

    Dopisać ograniczenia AOT do AGENTS.md

TODO 13 – symlinki i porządek

    Utworzyć GEMINI.md jako symlink do AGENTS.md

    Sprawdzić repo na czystość

    Commit: add GEMINI.md symlink and cleanup

    Ostatnia aktualizacja AGENTS.md


Dockerfile ma być wcześniej, byśmy mogli od razu testować

dodaj .gitlab-ci.yml tak by wygenerował statycznie zlinkowany binarkę do pobrania dla x86

