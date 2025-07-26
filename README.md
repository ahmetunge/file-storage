# File Storage - Dağıtık Dosya Parçalama ve Saklama Sistemi

## Proje Hakkında

Bu proje, büyük dosyaların otomatik olarak küçük parçalara (chunk) ayrılması, bu parçaların farklı depolama sağlayıcılarına dağıtılması ve gerektiğinde birleştirilerek dosya bütünlüğünün korunmasını sağlayan bir .NET Console Application’dır. Sistem, yedekleme ve dağıtık depolama gibi senaryolarda temel yapı taşı olarak kullanılabilecek şekilde, genişletilebilir ve test edilebilir olarak tasarlanmıştır.

## Özellikler

- **Tekli ve çoklu dosya chunk’lama:** Bir veya birden fazla dosya, otomatik olarak optimal boyutlarda parçalara ayrılır.
- **Dinamik chunk algoritması:** Dosya boyutuna göre en uygun chunk boyutu `AppConstants` ve `ChunkingService` üzerinden hesaplanır.
- **Farklı storage provider’lara dağıtım:** Her bir chunk, farklı `IStorageProvider` implementasyonlarına (ör. FileSystem, Database) Strategy Pattern ile dağıtılır.
- **Kalıcı metadata saklama:** Chunk ve dosya metadata’ları SQLite veritabanında saklanır.
- **Birleştirme ve bütünlük kontrolü:** Chunk’lar birleştirilerek orijinal dosya oluşturulur ve SHA256 checksum ile doğrulama yapılır.
- **Tam loglama:** Tüm işlemler (chunk’lama, saklama, birleştirme, hata vb.) loglanır.
- **SOLID ve OOP prensipleri:** Tüm bileşenler arayüzler ile soyutlanmış, IoC container ile dışarıdan enjekte edilebilir yapıdadır.
- **Kapsamlı unit testler:** Tüm ana fonksiyonellikler için testler mevcuttur.
- **Parçalanmış dosylar. "\src\FileStorage.ConsoleApp\bin\Debug\net9.0\Chunks" altında tutulacaktır.

## Kurulum ve Çalıştırma

### Gereksinimler

- .NET 8 veya üzeri
- (Opsiyonel) Visual Studio 2022+ veya Rider

### Çalıştırma

1. **Projeyi klonlayın:**

   ```sh
   git clone <repo-link>
   cd file-storage
   ```

2. **Bağımlılıkları yükleyin:**

   ```sh
   dotnet restore
   ```

3. **Uygulamayı başlatın:**

   - Visual Studio ile: `FileStorage.ConsoleApp` projesini başlatın (F5).
   - Konsoldan:
     ```sh
     cd src/FileStorage.ConsoleApp
     dotnet run
     ```

4. **Test dosyaları:**  
   `TestFiles` klasöründe örnek PDF dosyaları mevcuttur. Uygulama çalışırken bu dosyalar ile test edebilirsiniz.
> **Not:** Uygulamanın test edilmesini kolaylaştırmak amacıyla, dosya parçalama (chunking) boyutu `AppConstants` içerisinde bilinçli olarak **küçük bir değerde** ayarlanmıştır. Bu sayede, `TestFiles` klasöründeki küçük dosyalarla bile sistemin bir dosyayı birden çok parçaya ayırma ve farklı sağlayıcılara dağıtma mantığını rahatça gözlemleyebilirsiniz.
   
> **Not:** Uygulama ilk çalıştırıldığında, SQLite veritabanı ve gerekli tablolar otomatik olarak oluşturulacaktır. Ekstra bir kurulum adımı gerektirmez.

### Testler

Tüm ana fonksiyonellikler için unit testler yazılmıştır. Testleri çalıştırmak için:

```sh
dotnet test
```

## Mimari ve Tasarım Tercihleri

- **ChunkingService & AppConstants:**  
  Chunk boyutu, dosya boyutuna göre dinamik olarak belirlenir. Detaylar için `ChunkingService` ve `AppConstants` dosyalarına bakınız.

- **Provider Strategy Pattern:**  
  Farklı depolama sağlayıcıları (ör. FileSystem, Database) için Strategy Pattern kullanılmıştır. Yeni bir provider eklemek için `IStorageProvider` arayüzünü implemente etmek yeterlidir.

- **IoC & Dependency Injection:**  
  Tüm bağımlılıklar, `Microsoft.Extensions.DependencyInjection` ile IoC container üzerinden yönetilir.

- **Veritabanı:**  
  Hızlı test edilebilmesi için SQLite kullanılmıştır. Dilerseniz farklı bir veritabanı ile kolayca değiştirebilirsiniz.

- **Loglama:**  
  Tüm işlemler, uygulama içinde loglanır. (ILogger veya Serilog ile genişletilebilir.)

- **Test Edilebilirlik:**  
  Tüm ana bileşenler interface tabanlıdır ve kolayca test edilebilir.

## Ekstra Özellikler

- Çoklu dosya desteği
- Kapsamlı unit testler
- Kolayca yeni storage provider eklenebilir mimari
- Hızlı kurulum ve test için SQLite kullanımı

## Katkı ve Geliştirme

Yeni storage provider’lar eklemek veya mevcut fonksiyonelliği genişletmek için:

- `IStorageProvider` arayüzünü implemente edin.
- Gerekli DI (Dependency Injection) ayarlarını yapın.
- Testlerinizi ekleyin.

## Lisans

Bu proje, iş başvurusu case çalışması olarak geliştirilmiştir.
