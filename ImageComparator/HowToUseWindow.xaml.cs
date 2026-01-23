using System.Windows;

namespace ImageComparator
{
    public partial class HowToUseWindow : Window
    {
        public HowToUseWindow(bool isEnglish)
        {
            InitializeComponent();

            if (isEnglish)
            {
                Title = "How To Use";
                closeButton.Content = "Close";
                contentTextBlock.Text = GetEnglishContent();
            }
            else
            {
                Title = "Nasıl Kullanılır";
                closeButton.Content = "Kapat";
                contentTextBlock.Text = GetTurkishContent();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private string GetEnglishContent()
        {
            return @"IMAGE COMPARATOR - USER GUIDE

Welcome to Image Comparator! This application helps you find and manage duplicate images in your folders.

═══════════════════════════════════════════════════════════

1. ADDING FOLDERS
   • Click the ""Add Folder"" button or drag-and-drop folders into the output list at the bottom
   • The application will scan for image files in the selected folder
   • You can add multiple folders to compare images across different locations

2. FINDING DUPLICATES
   • Click ""Find Duplicates"" to start the comparison process
   • The application will analyze images and find similar or duplicate files
   • Progress will be shown in the progress bar
   • You can pause or stop the process at any time using the respective buttons

3. UNDERSTANDING RESULTS
   • Duplicate pairs are shown in two side-by-side lists
   • Color-coded indicators show confidence levels:
     - Green: Exact duplicates (100% match)
     - Yellow: Very similar (high confidence)
     - Red: Similar (medium confidence)
     - Blue: False positive marked by user
   • Click on any file to preview it in the center panel

4. SEARCH FORMATS
   • Go to Options > Search Formats to select which image types to scan
   • Supported formats: JPEG, BMP, PNG, GIF, TIFF, ICO
   • All formats are enabled by default

5. DELETION OPTIONS
   • Select files to delete using the checkboxes
   • ""Delete Selected"": Removes checked files from your system
   • ""Mark For Deletion"": Highlights files for later deletion (shown in red)
   • ""Remove From List"": Removes items from results without deleting files

6. DELETION METHOD
   • Go to Options > Deletion Method to choose:
     - ""Send To Recycle Bin"": Safer option, allows file recovery (default)
     - ""Delete Permanently"": Files are deleted permanently without recovery option

7. FALSE POSITIVES
   • If two images are incorrectly matched, select them and click ""Mark As False Positive""
   • These pairs will be remembered and won't appear in future scans
   • To clear the false positive database, go to Options > Clear False Positive Database

8. LANGUAGE OPTIONS
   • Go to Options > Language to switch between English and Türkçe
   • The interface will update immediately

9. ADDITIONAL OPTIONS
   • ""Include Subfolders"": Scans all subfolders within selected folders
   • ""Skip Files With Different Orientation"": Ignores portrait vs landscape matches
   • ""Find Exact Duplicates Only"": Only shows files with identical content

10. SAVING AND LOADING RESULTS
    • File > Save Results: Saves your current findings to continue later
    • File > Load Results: Loads previously saved results

═══════════════════════════════════════════════════════════

TIPS:
• You can zoom in/out on preview images using the mouse wheel or +/- buttons
• Right-click on files to open them or view their location
• Use keyboard navigation (arrow keys) to browse through results quickly
• Results can be sorted by clicking on the bullet points

For more information or to report issues, visit the project repository.";
        }

        private string GetTurkishContent()
        {
            return @"GÖRÜNTÜ KARŞILAŞTIRICI - KULLANICI KILAVUZU

Görüntü Karşılaştırıcı'ya hoş geldiniz! Bu uygulama klasörlerinizdeki kopya görselleri bulmanıza ve yönetmenize yardımcı olur.

═══════════════════════════════════════════════════════════

1. KLASÖR EKLEME
   • ""Klasör Ekle"" düğmesine tıklayın veya klasörleri alt kısımdaki çıktı listesine sürükleyip bırakın
   • Uygulama seçilen klasördeki görüntü dosyalarını tarayacaktır
   • Farklı konumlardaki görselleri karşılaştırmak için birden fazla klasör ekleyebilirsiniz

2. KOPYALARI BULMA
   • Karşılaştırma işlemini başlatmak için ""Kopyaları Bul""a tıklayın
   • Uygulama görselleri analiz edecek ve benzer veya kopya dosyaları bulacaktır
   • İlerleme çubuğunda ilerleme gösterilecektir
   • İlgili düğmeleri kullanarak işlemi istediğiniz zaman duraklatabilir veya durdurabilirsiniz

3. SONUÇLARI ANLAMA
   • Kopya çiftleri yan yana iki listede gösterilir
   • Renk kodlu göstergeler güven seviyelerini gösterir:
     - Yeşil: Tam kopyalar (%100 eşleşme)
     - Sarı: Çok benzer (yüksek güven)
     - Kırmızı: Benzer (orta güven)
     - Mavi: Kullanıcı tarafından hatalı olarak işaretlenmiş
   • Herhangi bir dosyaya tıklayarak orta panelde önizlemesini görebilirsiniz

4. ARAMA FORMATLARI
   • Seçenekler > Aranacak Formatlar'a giderek hangi görüntü türlerinin taranacağını seçin
   • Desteklenen formatlar: JPEG, BMP, PNG, GIF, TIFF, ICO
   • Tüm formatlar varsayılan olarak etkindir

5. SİLME SEÇENEKLERİ
   • Onay kutularını kullanarak silinecek dosyaları seçin
   • ""Seçilenleri Sil"": İşaretli dosyaları sisteminizden kaldırır
   • ""Silinecek Olarak İşaretle"": Dosyaları daha sonra silinmek üzere vurgular (kırmızı gösterilir)
   • ""Listeden Kaldır"": Dosyaları silmeden sonuçlardan kaldırır

6. SİLME YÖNTEMİ
   • Seçenekler > Silme Yöntemi'ne giderek seçin:
     - ""Geri Dönüşüm Kutusuna Gönder"": Daha güvenli seçenek, dosya kurtarmaya izin verir (varsayılan)
     - ""Kalıcı Olarak Sil"": Dosyalar kurtarma seçeneği olmadan kalıcı olarak silinir

7. HATALI SONUÇLAR
   • İki görsel yanlış eşleştirilmişse, bunları seçin ve ""Hatalı Sonuç Olarak İşaretle""ye tıklayın
   • Bu çiftler hatırlanacak ve gelecekteki taramalarda görünmeyecektir
   • Hatalı sonuç veritabanını temizlemek için Seçenekler > Hatalı Sonuç Veritabanını Temizle'ye gidin

8. DİL SEÇENEKLERİ
   • Seçenekler > Dil'e giderek İngilizce ve Türkçe arasında geçiş yapın
   • Arayüz hemen güncellenecektir

9. EK SEÇENEKLER
   • ""Alt Klasörlerde Ara"": Seçilen klasörlerdeki tüm alt klasörleri tarar
   • ""Farklı Oryantasyondaki Dosyaları Geç"": Portre ve manzara eşleşmelerini yok sayar
   • ""Sadece Kopyaları Bul"": Yalnızca özdeş içeriğe sahip dosyaları gösterir

10. SONUÇLARI KAYDETME VE YÜKLEME
    • Dosya > Sonuçları Kaydet: Mevcut bulgularınızı daha sonra devam etmek için kaydeder
    • Dosya > Sonuçları Yükle: Önceden kaydedilmiş sonuçları yükler

═══════════════════════════════════════════════════════════

İPUÇLARI:
• Fare tekerleğini veya +/- düğmelerini kullanarak önizleme görsellerini yakınlaştırabilir/uzaklaştırabilirsiniz
• Dosyaları açmak veya konumlarını görüntülemek için sağ tıklayın
• Sonuçlara hızlıca göz atmak için klavye gezintisini (ok tuşları) kullanın
• Madde işaretlerine tıklayarak sonuçlar sıralanabilir

Daha fazla bilgi için veya sorun bildirmek için proje deposunu ziyaret edin.";
        }
    }
}
