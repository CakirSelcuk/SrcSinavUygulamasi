using Plugin.MauiMTAdmob;

namespace SrcSinavUygulamasi.Services
{
    /// <summary>
    /// AdMob reklam yönetim servisi.
    /// Banner ve Interstitial reklamları yönetir.
    /// Premium kullanıcılara reklam göstermez.
    /// </summary>
    public class AdMobService
    {
        private readonly PremiumService _premiumService;
        private bool _isInterstitialLoaded = false;
        private TaskCompletionSource<bool>? _adClosedTcs;
        private TaskCompletionSource<bool>? _adLoadTcs;

        // AdMob IDs
        public const string APP_ID = "ca-app-pub-1333313233367768~6014271517";
#if DEBUG
        public const string BANNER_AD_UNIT_ID = "ca-app-pub-3940256099942544/6300978111";
        public const string INTERSTITIAL_AD_UNIT_ID = "ca-app-pub-3940256099942544/1033173712";
#else
        public const string BANNER_AD_UNIT_ID = "ca-app-pub-1333313233367768/4023658725";
        public const string INTERSTITIAL_AD_UNIT_ID = "ca-app-pub-1333313233367768/4961335871";
#endif

        public AdMobService(PremiumService premiumService)
        {
            _premiumService = premiumService;
            
            // Event handlers'ı bağla
            CrossMauiMTAdmob.Current.OnInterstitialLoaded += OnInterstitialLoaded;
            CrossMauiMTAdmob.Current.OnInterstitialFailedToLoad += OnInterstitialFailedToLoad;
            CrossMauiMTAdmob.Current.OnInterstitialClosed += OnInterstitialClosed;
        }

        /// <summary>
        /// Interstitial reklamı yükle (premium değilse).
        /// </summary>
        public async Task LoadInterstitialAdAsync()
        {
            try
            {
                // Premium kontrolü
                bool isPremium = _premiumService.IsUserPremium;
                if (isPremium)
                {
                    Console.WriteLine("[AdMob] Premium kullanıcı - Interstitial reklam yüklenmiyor");
                    return;
                }

                _isInterstitialLoaded = false;
                _adLoadTcs = new TaskCompletionSource<bool>();

                // Reklamı yükle
                CrossMauiMTAdmob.Current.LoadInterstitial(INTERSTITIAL_AD_UNIT_ID);
                Console.WriteLine("[AdMob] Interstitial reklam yükleniyor...");

                var timeoutTask = Task.Delay(10000);
                var completedTask = await Task.WhenAny(_adLoadTcs.Task, timeoutTask);

                if (completedTask == timeoutTask)
                {
                    Console.WriteLine("[AdMob] Interstitial yükleme timeout");
                    return;
                }

                await _adLoadTcs.Task;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AdMob] Interstitial yükleme hatası: {ex.Message}");
            }
        }

        /// <summary>
        /// Interstitial reklamı göster ve kapanmasını bekle.
        /// Premium kullanıcılar için direkt true döner.
        /// </summary>
        public async Task<bool> ShowInterstitialAdAsync()
        {
            try
            {
                // Premium kontrolü
                bool isPremium = _premiumService.IsUserPremium;
                if (isPremium)
                {
                    Console.WriteLine("[AdMob] Premium kullanıcı - Reklam gösterilmiyor");
                    return true;
                }

                // Reklam yüklü mü kontrol et
                if (!_isInterstitialLoaded || !CrossMauiMTAdmob.Current.IsInterstitialLoaded())
                {
                    Console.WriteLine("[AdMob] Interstitial reklam yüklü değil");
                    return false;
                }

                // Reklamı göster ve kapanmasını bekle
                _adClosedTcs = new TaskCompletionSource<bool>();
                
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    try
                    {
                        CrossMauiMTAdmob.Current.ShowInterstitial();
                        Console.WriteLine("[AdMob] Interstitial reklam gösteriliyor");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[AdMob] Reklam gösterme hatası: {ex.Message}");
                        _adClosedTcs?.TrySetResult(false);
                    }
                });

                // Reklamın kapanmasını bekle (max 30 saniye)
                var timeoutTask = Task.Delay(30000);
                var completedTask = await Task.WhenAny(_adClosedTcs.Task, timeoutTask);

                if (completedTask == timeoutTask)
                {
                    Console.WriteLine("[AdMob] Reklam gösterme timeout");
                    return false;
                }

                return await _adClosedTcs.Task;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AdMob] ShowInterstitialAdAsync hatası: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Banner reklamın görünür olup olmayacağını kontrol et.
        /// Premium kullanıcılar için false döner.
        /// </summary>
        public Task<bool> ShouldShowBannerAsync()
        {
            return Task.FromResult(!_premiumService.IsUserPremium);
        }

        // Event Handlers
        private void OnInterstitialLoaded(object? sender, EventArgs e)
        {
            _isInterstitialLoaded = true;
            _adLoadTcs?.TrySetResult(true);
            Console.WriteLine("[AdMob] Interstitial reklam yüklendi");
        }

        private void OnInterstitialFailedToLoad(object? sender, EventArgs e)
        {
            _isInterstitialLoaded = false;
            _adLoadTcs?.TrySetResult(false);
            Console.WriteLine("[AdMob] Interstitial yükleme başarısız");
        }

        private void OnInterstitialClosed(object? sender, EventArgs e)
        {
            Console.WriteLine("[AdMob] Interstitial reklam kapandı");
            _adClosedTcs?.TrySetResult(true);
            _isInterstitialLoaded = false;
        }
    }
}
