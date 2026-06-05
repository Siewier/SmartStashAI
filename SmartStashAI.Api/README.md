# SmartStashAI.Api

SmartStashAI.Api to nowoczesny, wydajny backend napisany w środowisku **.NET 8 (ASP.NET Core Web API)**. Aplikacja stanowi serce systemu inteligentnego zarządzania domowym magazynem, komponentami elektronicznymi, narzędziami oraz automatyką. 

Projekt integruje się z lokalnymi modelami sztucznej inteligencji (LLM/VLM) za pośrednictwem serwera **Ollama**, wykorzystując moc obliczeniową karty graficznej do automatycznego rozpoznawania i kategoryzowania przedmiotów na podstawie zdjęć.

---

## 🚀 Główne Funkcjonalności

### 1. Autoryzacja i Współdzielenie (Multi-tenant / Households)
* **Architektura Rodzinna:** Przedmioty i szafki nie są przypisane do jednej osoby, lecz do gospodarstwa domowego (`Household`). 
* **System Rejestracji i Zaproszeń:** Pierwszy użytkownik tworzy przestrzeń domową, a kolejni członkowie rodziny (np. współmałżonek, dzieci) mogą do niej dołączyć za pomocą unikalnego ID domu.
* **Bezpieczeństwo:** Autentykacja oparta o tokeny **JWT (JSON Web Tokens)** z bezpiecznym haszowaniem haseł algorytmem PBKDF2 (`PasswordHasher`). ID gospodarstwa domowego jest zaszyte bezpośrednio w roszczeniach (claims) tokenu, co gwarantuje całkowitą izolację danych.

### 2. Cyfrowe Mapowanie Schowków i Kody QR
* **Struktura Drzewiasta lokalizacji:** System pozwala na rekurencyjne tworzenie struktur szafek i pojemników (np. `Garaż` -> `Regał A` -> `Szuflada 1`).
* **Integracja z QRCoder:** Każda nowo utworzona lokalizacja otrzymuje unikalny token tekstowy zabezpieczony prefiksem (np. `STASH_C8E00BDDDA1B`). Backend udostępnia endpoint generujący fizyczny kod QR w formacie PNG, gotowy do wydrukowania i naklejenia na organizer lub szufladę.
* **Skanowanie (Opcja Wyszukiwania 1):** Zeskanowanie kodu telefonem natychmiast wyciąga z bazy pełną zawartość danego pojemnika wraz z rzeczami ukrytymi w podszufladach.

### 3. Zarządzanie Przedmiotami (`Items`)
* **Katalogowanie:** Zapisywanie nazwy, kategorii, przeznaczenia oraz ścieżki do lokalnego magazynu zdjęć przedmiotu.
* **System Śledzenia Zgub (IsLost):** Jeśli przedmiotu nie ma w szafce, w której powinien się znajdować, użytkownik może jednym kliknięciem oznaczyć go flagą `IsLost`, co ułatwia zarządzanie domowymi zgubami.
* **Filtrowanie ścieżek:** Podczas wyszukiwania system automatycznie mapuje i zwraca pełną ścieżkę dostępu do przedmiotu (np. `Piwnica -> Szafa Metalowa -> Pudełko z kablami`).

### 4. Lokalna Sztuczna Inteligencja (Vision AI)
* **Integracja z Ollama:** Wykorzystanie ujednoliconego standardu `Microsoft.Extensions.AI` oraz oficjalnej biblioteki `OllamaSharp` do komunikacji z lokalnym serwerem AI.
* **Model Llama 3.2 Vision:** Przesłany z urządzenia mobilnego obraz w formacie Base64 jest konwertowany na dane binarne (`DataContent`) i przetwarzany przez model wizyjny uruchomiony na lokalnym GPU.
* **Wymuszenie Struktury JSON:** API steruje temperaturą generowania (`Temperature = 0.1f`) oraz formatem wyjściowym, zmuszając LLM do zwrotu czystego, ustrukturyzowanego obiektu JSON zawierającego dopasowaną nazwę, kategorię i przeznaczenie w języku polskim.

---

## 🛠️ Architektura i Stos Technologiczny

* **Framework:** .NET 8.0 (ASP.NET Core Web API)
* **Baza Danych:** SQLite (lekka, bezserwerowa, przechowywana lokalnie w pliku `smartstash.db`)
* **ORM:** Entity Framework Core 8.0 (podejście Code-First z pełną historią migracji)
* **AI:** Microsoft.Extensions.AI + OllamaSharp (Model: `llama3.2-vision:11b`)
* **Generowanie QR:** QRCoder (Natywne renderowanie tablic bajtów PNG przez `PngByteQRCode`)
* **Dokumentacja API:** Swagger / OpenAPI z włączoną obsługą autoryzacji Bearer JWT

---

## 🗄️ Struktura Bazy Danych (Encje)



* **`Household`**: Reprezentuje domostwo. Posiada kolekcję członków oraz lokalizacji.
* **`ApplicationUser`**: Dane użytkownika (Username, PasswordHash) powiązane relacją jeden-do-wielu z `Household`.
* **`StorageLocation`**: Schowek posiadający unikalny indeks na `QrCodeToken`. Zawiera klucz obcy do samej siebie (`ParentLocationId`), umożliwiając budowanie drzewa szafek. Usunięcie szafki kaskadowo usuwa informację o szufladach wewnątrz (`DeleteBehavior.Cascade`).
* **`Item`**: Przedmiot przypisany do konkretnego `StorageLocation`. Posiada flagę stanową `IsLost`.

---

## ⚡ Szybki Start (Uruchomienie Lokalne)

### Wymagania wstępne
1. Zainstalowane środowisko **.NET 8 SDK**.
2. Uruchomiona lokalnie **Ollama** z pobranym modelem wizyjnym:
   ```bash
   ollama run llama3.2-vision:11b

## Konfiguracja i uruchomienie
Sklonuj repozytorium projektu.

Otwórz projekt w Visual Studio 2022 lub VS Code.

Przywróć pakiety NuGet i uruchom pierwszą migrację bazy danych w Package Manager Console:

PowerShell
Update-Database
Uruchom aplikację wybierając profil https lub http (F5).

Przeglądarka automatycznie otworzy interfejs Swagger UI pod adresem:
https://localhost:7047/swagger/index.html

Testowanie autoryzacji w Swaggerze
Aby testować endpointy oznaczone jako [Authorize]:

Zarejestruj się lub zaloguj przez sekcję Auth.

Skopiuj wygenerowany token z odpowiedzi.

Kliknij przycisk Authorize na samej górze strony Swaggera.

Wpisz: Bearer <wklej_skopiowany_token> (pamiętaj o spacji po słowie Bearer) i zatwierdź.