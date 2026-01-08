using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SrcSinavUygulamasi.Models;
using SrcSinavUygulamasi.Views;

namespace SrcSinavUygulamasi.ViewModels
{
    public partial class CategoriesViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<CategoryModel> categories;

        public CategoriesViewModel()
        {
            // İŞTE BURASI: Menüyü SRC formatına çeviriyoruz.
            // Id kısımları dosya isimleriyle eşleşmeli: "src1" -> "sorular_src1.json"
            Categories = new ObservableCollection<CategoryModel>
{
    new CategoryModel
    {
        Id = "src1",
        Title = "SRC 1",
        IconText = "🚌", // Uluslararası yolcu otobüsü
        Description = "Uluslararası Yolcu Taşımacılığı",
        Color = "#FF5722"
    },
    new CategoryModel
    {
        Id = "src2",
        Title = "SRC 2",
        IconText = "🚐", // Yurtiçi minibüs/otobüs
        Description = "Yurtiçi Yolcu Taşımacılığı",
        Color = "#2196F3"
    },
    new CategoryModel
    {
        Id = "src3",
        Title = "SRC 3",
        IconText = "🚛", // Uluslararası tır
        Description = "Uluslararası Eşya/Kargo Taşımacılığı",
        Color = "#4CAF50"
    },
    new CategoryModel
    {
        Id = "src4",
        Title = "SRC 4",
        IconText = "🚚", // Yurtiçi kamyon
        Description = "Yurtiçi Eşya/Kargo Taşımacılığı",
        Color = "#9C27B0"
    },
    new CategoryModel
    {
        Id = "src5",
        Title = "SRC 5",
        IconText = "☢️", // Tehlikeli madde
        Description = "Tehlikeli Madde Taşımacılığı (ADR)",
        Color = "#00BCD4"
    }
};
        }

        [RelayCommand]
        private async Task KategoriSec(CategoryModel category)
        {
            if (category == null) return;

            // Seçilen SRC türüne göre Deneme Listesi Sayfasına git
            await Shell.Current.GoToAsync($"{nameof(ExamListPage)}",
                new Dictionary<string, object>
                {
                    { "KategoriId", category.Id }
                });
        }
    }
}