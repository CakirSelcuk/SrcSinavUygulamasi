using Microsoft.Maui.Graphics;

namespace SrcSinavUygulamasi.Models
{
    public class CategoryModel
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Color { get; set; }

        // BU SATIRI EKLE (Hatanın Çözümü):
        public string IconText { get; set; }
    }
}