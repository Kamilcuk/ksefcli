# ksefcli

`ksefcli` to narzędzie wiersza poleceń (CLI) dla systemu Linux, napisane w języku C#, które ułatwia interakcję z Krajowym Systemem e-Faktur (KSeF) w Polsce. Aplikacja wykorzystuje bibliotekę kliencką `ksef-client-csharp` do komunikacji z usługami KSeF.

## Instalacja

Pobierz najnowszą wersję `ksefcli` z sekcji [Releases](https://github.com/your-repo/ksefcli/releases) i umieść ją w katalogu znajdującym się w `PATH`, na przykład `/usr/local/bin`.

## Konfiguracja

Przed rozpoczęciem pracy z `ksefcli`, należy skonfigurować aplikację, tworząc plik `ksefcli.yaml` w jednym z następujących miejsc:
- W katalogu bieżącym: `./ksefcli.yaml`
- W katalogu konfiguracyjnym użytkownika: `$HOME/.config/ksefcli/ksefcli.yaml`

Plik ten zawiera profile, które umożliwiają zarządzanie różnymi poświadczeniami i środowiskami KSeF.

### Struktura pliku `ksefcli.yaml`

```yaml
active_profile: <nazwa_aktywnego_profilu>
profiles:
  <nazwa_profilu_1>:
    environment: <srodowisko>
    nip: <nip_podmiotu>
    token: <token_autoryzacyjny>
    certificate:
      private_key: <sciezka_do_klucza_prywatnego>
      certificate: <sciezka_do_certyfikatu_publicznego>
      password_env: <zmienna_srodowiskowa_z_haslem>
  <nazwa_profilu_2>:
    # ...
```

### Opcje Konfiguracyjne

*   `active_profile`: (Opcjonalnie) Nazwa profilu, który będzie używany domyślnie, jeśli nie zostanie podany za pomocą opcji `--profile`. Jeśli zdefiniowany jest tylko jeden profil, `active_profile` jest ignorowane.
*   `profiles`: Mapa profili konfiguracyjnych.
    *   `<nazwa_profilu>`: Dowolna nazwa identyfikująca profil (np. `dyzio`, `firma_xyz_test`).
        *   `environment`: Środowisko KSeF (`test`, `demo`, `prod`).
        *   `nip`: Numer Identyfikacji Podatkowej (NIP) podmiotu, którego dotyczy profil.
        *   Należy zdefiniować **jedną** z poniższych metod uwierzytelniania:
            *   `token`: Token autoryzacyjny sesji.
            *   `certificate`: Dane certyfikatu kwalifikowanego.
                *   `private_key`: Ścieżka do klucza prywatnego (plik `.pem` lub `.pfx`). Można użyć `~` jako skrótu do katalogu domowego.
                *   `certificate`: Ścieżka do certyfikatu publicznego. Można użyć `~` jako skrótu do katalogu domowego.
                *   `password_env`: Nazwa zmiennej środowiskowej, która przechowuje hasło do klucza prywatnego.

### Przykład Konfiguracji

Poniższy przykład demonstruje konfigurację z wieloma profilami dla różnych podmiotów i środowisk.

```yaml
---
active_profile: dyzio
profiles:
  dyzio:
    environment: test
    nip: '5260215591'
    token: fdsafa
  dyzio2:
    environment: test
    nip: '5260215591'
    token: fdsfa
  kamcuk:
    environment: test
    nip: '5223217667'
    token: fdasfa
  cert_auth_example:
    environment: prod
    nip: '1234567890'
    certificate:
      private_key: '~/certs/my_private_key.pem'
      certificate: '~/certs/my_certificate.pem'
      password_env: 'KSEF_CERT_PASSWORD'

```

W tym przykładzie:
- Domyślnym profilem jest `dyzio`.
- Zdefiniowano trzy profile (`dyzio`, `dyzio2`, `kamcuk`) używające uwierzytelniania tokenem na środowisku testowym dla dwóch różnych NIP-ów.
- Profil `cert_auth_example` używa uwierzytelniania certyfikatem na środowisku produkcyjnym. Hasło do certyfikatu zostanie odczytane ze zmiennej środowiskowej `KSEF_CERT_PASSWORD`.

## Użycie

Ogólna składnia poleceń `ksefcli` jest następująca:

```bash
ksefcli <polecenie> [opcje]
```

### Dostępne Polecenia

#### `Auth`

Uwierzytelnia użytkownika przy użyciu skonfigurowanej metody (token lub certyfikat).

```bash
ksefcli Auth
```

#### `TokenAuth`

Uwierzytelnia za pomocą tokena.

*   `--token`: Token autoryzacyjny.

#### `CertAuth`

Uwierzytelnianie za pomocą certyfikatu.

*   `--private-key`: Ścieżka do klucza prywatnego.
*   `--certificate`: Ścieżka do certyfikatu.
*   `--password-env`: Zmienna środowiskowa z hasłem.

#### `TokenRefresh`

Odświeża token autoryzacyjny.

#### `SzukajFaktur`

Wyszukuje metadane faktur.

*   `-s`, `--subject-type`: Typ podmiotu (`Subject1`, `Subject2`, etc.).
*   `--from`: Data początkowa w formacie ISO-8601.
*   `--to`: Data końcowa w formacie ISO-8601.
*   `--date-type`: Typ daty (`Issue`, `Invoicing`, `PermanentStorage`). Domyślnie `Issue`.
*   `--page-offset`: Przesunięcie strony dla paginacji. Domyślnie `0`.
*   `--page-size`: Rozmiar strony dla paginacji. Domyślnie `10`.

#### `ExportInvoices`

Inicjuje asynchroniczny eksport faktur.

*   `--from`: Data początkowa w formacie ISO-8601.
*   `--to`: Data końcowa w formacie ISO-8601.
*   `--date-type`: Typ daty (`Issue`, `Invoicing`, `PermanentStorage`). Domyślnie `Issue`.
*   `-s`, `--subject-type`: Typ podmiotu faktury.

#### `GetExportStatusCommand`

Sprawdza status eksportu faktur.

*   `--reference-number`: Numer referencyjny eksportu.

#### `GetFaktura`

Pobiera pojedynczą fakturę.

*   `--ksef-id`: Numer KSeF faktury.

## Cache Tokenów

`ksefcli` przechowuje tokeny autoryzacyjne w pamięci podręcznej, aby uniknąć konieczności wielokrotnego uwierzytelniania. Domyślna lokalizacja pliku z tokenami to `~/.cache/ksefcli/tokenstore.json`.

## Rozwój

Aby skonfigurować środowisko deweloperskie, wykonaj następujące kroki:

1.  Sklonuj repozytorium:
    ```bash
    git clone https://github.com/your-repo/ksefcli.git
    ```
2.  Zainstaluj zależności .NET:
    ```bash
    dotnet restore
    ```
3.  Zbuduj projekt:
    ```bash
    dotnet build
    ```
4.  Uruchom aplikację:
    ```bash
    dotnet run -- <polecenie> [opcje]
    ```

## Uwierzytelnianie w KSeF

Szczegółowe informacje na temat mechanizmów uwierzytelniania w Krajowym Systemie e-Faktur można znaleźć w oficjalnej dokumentacji: [Uwierzytelnianie w KSeF](https://github.com/CIRFMF/ksef-docs/blob/main/uwierzytelnianie.md).
