using Microsoft.AspNetCore.Mvc;
using MyApi.BLL.Service;
using MyApi.DAL.Data;
using MyApi.DAL.Models;

namespace MyApi.PLL.Controllers
{
    [ApiController]
    [Route("api/admin")]
    public class SeedProductsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IFileService _fileService;
        private readonly IWebHostEnvironment _env;

        public SeedProductsController(
            ApplicationDbContext context,
            IFileService fileService,
            IWebHostEnvironment env)
        {
            _context = context;
            _fileService = fileService;
            _env = env;
        }

        [HttpPost("seed-products-cloudinary")]
        public async Task<IActionResult> SeedProductsCloudinary([FromQuery] int count = 100)
        {
            var folderPath = Path.Combine(_env.ContentRootPath, "SeedFiles", "ProductImages");

            if (!Directory.Exists(folderPath))
                return BadRequest("Folder SeedFiles/ProductImages not found.");

            var imagePaths = Directory.GetFiles(folderPath)
                .Where(x =>
                    x.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                    x.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) ||
                    x.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                    x.EndsWith(".webp", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (!imagePaths.Any())
                return BadRequest("No images found inside SeedFiles/ProductImages.");

            var random = new Random();

            var productTypes = new List<ProductSeedType>
            {
                new ProductSeedType
                {
                    CategoryId = 11, // عدليه حسب الداتا بيس عندك
                    EnType = "Phone",
                    ArType = "هاتف",
                    MinPrice = 699,
                    MaxPrice = 4200,
                    EnNames = new List<string>
                    {
                        "Galaxy A15",
                        "iPhone 11",
                        "Redmi Note 13",
                        "Galaxy S23",
                        "iPhone 13",
                        "Infinix Note 30",
                        "Tecno Spark 20",
                        "Xiaomi 13T"
                    },
                    ArNames = new List<string>
                    {
                        "جالاكسي A15",
                        "آيفون 11",
                        "ريدمي نوت 13",
                        "جالاكسي S23",
                        "آيفون 13",
                        "إنفينيكس نوت 30",
                        "تيكنو سبارك 20",
                        "شاومي 13T"
                    },
                    EnDescription = "A modern smartphone with reliable performance, clear display, and long battery life.",
                    ArDescription = "هاتف ذكي عصري بأداء ممتاز وشاشة واضحة وبطارية تدوم لفترة طويلة."
                },
                new ProductSeedType
                {
                    CategoryId = 12,
                    EnType = "Dress",
                    ArType = "فستان",
                    MinPrice = 89,
                    MaxPrice = 320,
                    EnNames = new List<string>
                    {
                        "Floral Summer Dress",
                        "Elegant Evening Dress",
                        "Casual Cotton Dress",
                        "Pleated Midi Dress",
                        "Long Sleeve Dress",
                        "Classic Black Dress",
                        "Soft Chiffon Dress",
                        "Printed Maxi Dress"
                    },
                    ArNames = new List<string>
                    {
                        "فستان صيفي مورّد",
                        "فستان سهرة أنيق",
                        "فستان قطن كاجوال",
                        "فستان ميدي بكسرات",
                        "فستان بأكمام طويلة",
                        "فستان أسود كلاسيكي",
                        "فستان شيفون ناعم",
                        "فستان ماكسي مزخرف"
                    },
                    EnDescription = "Elegant and comfortable dress suitable for daily wear and special occasions.",
                    ArDescription = "فستان أنيق ومريح مناسب للاستخدام اليومي والمناسبات."
                },
                new ProductSeedType
                {
                    CategoryId = 3,
                    EnType = "Pants",
                    ArType = "بنطال",
                    MinPrice = 79,
                    MaxPrice = 220,
                    EnNames = new List<string>
                    {
                        "Classic Denim Jeans",
                        "Slim Fit Pants",
                        "Wide Leg Pants",
                        "Casual Cotton Pants",
                        "Straight Cut Jeans",
                        "Formal Trousers",
                        "High Waist Pants",
                        "Relaxed Fit Pants"
                    },
                    ArNames = new List<string>
                    {
                        "جينز دنيم كلاسيكي",
                        "بنطال بقصة ضيقة",
                        "بنطال واسع",
                        "بنطال قطن كاجوال",
                        "جينز بقصة مستقيمة",
                        "بنطال رسمي",
                        "بنطال خصر عالي",
                        "بنطال بقصة مريحة"
                    },
                    EnDescription = "Comfortable pants made for everyday style and easy movement.",
                    ArDescription = "بنطال مريح مناسب للإطلالة اليومية وسهولة الحركة."
                },
                new ProductSeedType
                {
                    CategoryId = 4,
                    EnType = "Bag",
                    ArType = "حقيبة",
                    MinPrice = 69,
                    MaxPrice = 260,
                    EnNames = new List<string>
                    {
                        "Leather Handbag",
                        "Mini Crossbody Bag",
                        "Classic Shoulder Bag",
                        "Travel Backpack",
                        "Canvas Tote Bag",
                        "Elegant Clutch Bag",
                        "Daily Use Handbag",
                        "Compact Side Bag"
                    },
                    ArNames = new List<string>
                    {
                        "حقيبة يد جلد",
                        "حقيبة كروس صغيرة",
                        "حقيبة كتف كلاسيكية",
                        "حقيبة ظهر للسفر",
                        "حقيبة قماش كبيرة",
                        "حقيبة سهرة أنيقة",
                        "حقيبة استخدام يومي",
                        "حقيبة جانبية صغيرة"
                    },
                    EnDescription = "Practical and stylish bag designed for everyday use.",
                    ArDescription = "حقيبة عملية وأنيقة مصممة للاستخدام اليومي."
                },
                new ProductSeedType
                {
                    CategoryId = 5,
                    EnType = "Headphones",
                    ArType = "سماعات",
                    MinPrice = 99,
                    MaxPrice = 650,
                    EnNames = new List<string>
                    {
                        "Wireless Earbuds",
                        "Noise Cancelling Headphones",
                        "Bluetooth Headset",
                        "Gaming Headphones",
                        "Sport Earphones",
                        "Over Ear Headphones",
                        "Compact Wireless Earbuds",
                        "Stereo Bluetooth Headphones"
                    },
                    ArNames = new List<string>
                    {
                        "سماعات لاسلكية",
                        "سماعات عزل ضوضاء",
                        "سماعة بلوتوث",
                        "سماعات ألعاب",
                        "سماعات رياضية",
                        "سماعات فوق الأذن",
                        "سماعات لاسلكية صغيرة",
                        "سماعات بلوتوث ستيريو"
                    },
                    EnDescription = "High quality audio device with clear sound and comfortable fit.",
                    ArDescription = "سماعات عالية الجودة بصوت واضح وراحة ممتازة أثناء الاستخدام."
                },
                new ProductSeedType
                {
                    CategoryId = 6,
                    EnType = "Glasses",
                    ArType = "نظارات",
                    MinPrice = 59,
                    MaxPrice = 220,
                    EnNames = new List<string>
                    {
                        "Classic Sunglasses",
                        "Round Frame Glasses",
                        "Square Frame Sunglasses",
                        "Lightweight Eyewear",
                        "Modern Fashion Glasses",
                        "UV Protection Sunglasses",
                        "Premium Black Frame Glasses",
                        "Daily Style Sunglasses"
                    },
                    ArNames = new List<string>
                    {
                        "نظارات شمسية كلاسيكية",
                        "نظارات بإطار دائري",
                        "نظارات شمسية بإطار مربع",
                        "نظارات خفيفة",
                        "نظارات عصرية",
                        "نظارات حماية من الأشعة",
                        "نظارات بإطار أسود فاخر",
                        "نظارات شمسية للاستخدام اليومي"
                    },
                    EnDescription = "Stylish glasses with comfortable frame and modern look.",
                    ArDescription = "نظارات أنيقة بإطار مريح ومظهر عصري."
                },
                new ProductSeedType
                {
                    CategoryId = 7,
                    EnType = "Watch",
                    ArType = "ساعة",
                    MinPrice = 120,
                    MaxPrice = 950,
                    EnNames = new List<string>
                    {
                        "Classic Leather Watch",
                        "Silver Metal Watch",
                        "Digital Sport Watch",
                        "Elegant Wrist Watch",
                        "Minimalist Black Watch",
                        "Premium Quartz Watch",
                        "Casual Daily Watch",
                        "Modern Smart Watch"
                    },
                    ArNames = new List<string>
                    {
                        "ساعة جلد كلاسيكية",
                        "ساعة معدنية فضية",
                        "ساعة رياضية رقمية",
                        "ساعة يد أنيقة",
                        "ساعة سوداء بسيطة",
                        "ساعة كوارتز فاخرة",
                        "ساعة يومية كاجوال",
                        "ساعة ذكية عصرية"
                    },
                    EnDescription = "Stylish watch with elegant details and reliable time performance.",
                    ArDescription = "ساعة أنيقة بتفاصيل جميلة وأداء موثوق."
                },
                new ProductSeedType
                {
                    CategoryId = 8,
                    EnType = "Shoes",
                    ArType = "حذاء",
                    MinPrice = 99,
                    MaxPrice = 420,
                    EnNames = new List<string>
                    {
                        "Running Shoes",
                        "Casual Sneakers",
                        "Classic Leather Shoes",
                        "Sport Training Shoes",
                        "Comfort Walking Shoes",
                        "Daily Wear Sneakers",
                        "Elegant Formal Shoes",
                        "Lightweight Sports Shoes"
                    },
                    ArNames = new List<string>
                    {
                        "حذاء رياضي للجري",
                        "سنيكرز كاجوال",
                        "حذاء جلد كلاسيكي",
                        "حذاء تدريب رياضي",
                        "حذاء مريح للمشي",
                        "سنيكرز للاستخدام اليومي",
                        "حذاء رسمي أنيق",
                        "حذاء رياضي خفيف"
                    },
                    EnDescription = "Comfortable shoes designed for daily wear and long use.",
                    ArDescription = "حذاء مريح مصمم للاستخدام اليومي ولساعات طويلة."
                }
            };

            var products = new List<Product>();

            for (int i = 1; i <= count; i++)
            {
                var selectedType = productTypes[(i - 1) % productTypes.Count];

                var nameIndex = random.Next(selectedType.EnNames.Count);
                var enName = selectedType.EnNames[nameIndex];
                var arName = selectedType.ArNames[nameIndex];

                var mainImagePath = imagePaths[random.Next(imagePaths.Count)];
                var mainImageUrl = await _fileService.UploadLocalFileAsync(mainImagePath);

                var price = random.Next(selectedType.MinPrice, selectedType.MaxPrice + 1);
                var stock = random.Next(5, 60);
                var discount = random.Next(0, 25);

                var product = new Product
                {
                    Price = price,
                    Stock = stock,
                    Discount = discount,
                    CategoryId = selectedType.CategoryId,
                    MainImage = mainImageUrl ?? "",
                    Translations = new List<ProductTranslation>
                    {
                        new ProductTranslation
                        {
                            Language = "en",
                            Name = enName,
                            Description = selectedType.EnDescription
                        },
                        new ProductTranslation
                        {
                            Language = "ar",
                            Name = arName,
                            Description = selectedType.ArDescription
                        }
                    },
                    SubImages = new List<ProductImage>()
                };

                var subImagesCount = random.Next(1, 3);

                for (int s = 0; s < subImagesCount; s++)
                {
                    var subImagePath = imagePaths[random.Next(imagePaths.Count)];
                    var subImageUrl = await _fileService.UploadLocalFileAsync(subImagePath);

                    product.SubImages.Add(new ProductImage
                    {
                        ImageName = subImageUrl ?? ""
                    });
                }

                products.Add(product);
            }

            await _context.Products.AddRangeAsync(products);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = $"{count} products added successfully."
            });
        }
    }

    public class ProductSeedType
    {
        public int CategoryId { get; set; }
        public string EnType { get; set; } = string.Empty;
        public string ArType { get; set; } = string.Empty;
        public int MinPrice { get; set; }
        public int MaxPrice { get; set; }
        public string EnDescription { get; set; } = string.Empty;
        public string ArDescription { get; set; } = string.Empty;
        public List<string> EnNames { get; set; } = new();
        public List<string> ArNames { get; set; } = new();
    }
}