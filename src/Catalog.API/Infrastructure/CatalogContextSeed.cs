using System.Text.Json;
using eShop.Catalog.API.Services;
using Pgvector;

namespace eShop.Catalog.API.Infrastructure;

public partial class CatalogContextSeed : IDatabaseInitializer
{

    private readonly CatalogContext _context;

    private readonly IWebHostEnvironment _env;
    private readonly IOptions<CatalogOptions> _settings;
    private readonly ILogger<CatalogContextSeed> _logger;

    public CatalogContextSeed(CatalogContext context, IWebHostEnvironment env, IOptions<CatalogOptions> settings, ILogger<CatalogContextSeed> logger)
    {
        _context = context;
        _env = env;
        _settings = settings;
        _logger = logger;
    }

    public async Task EnsureAdminIdentitiesAsync()
    {
        var useCustomizationData = _settings.Value.UseCustomizationData;
        var contentRootPath = _env.ContentRootPath;
        var picturePath = _env.WebRootPath;

        if (!_context.CatalogItems.Any())
        {
            var sourcePath = Path.Combine(contentRootPath, "Setup", "catalog.json");
            var sourceJson = File.ReadAllText(sourcePath);
            var sourceItems = JsonSerializer.Deserialize<CatalogSourceEntry[]>(sourceJson);

            _context.CatalogBrands.RemoveRange(_context.CatalogBrands);
            await _context.CatalogBrands.AddRangeAsync(sourceItems.Select(x => x.Brand).Distinct()
                .Select(brandName => new CatalogBrand { Brand = brandName }));
            _logger.LogInformation("Seeded catalog with {NumBrands} brands", _context.CatalogBrands.Count());

            _context.CatalogTypes.RemoveRange(_context.CatalogTypes);
            await _context.CatalogTypes.AddRangeAsync(sourceItems.Select(x => x.Type).Distinct()
                .Select(typeName => new CatalogType { Type = typeName }));
            _logger.LogInformation("Seeded catalog with {NumTypes} types", _context.CatalogTypes.Count());

            var brandIdsByName = await _context.CatalogBrands.ToDictionaryAsync(x => x.Brand, x => x.Id);
            var typeIdsByName = await _context.CatalogTypes.ToDictionaryAsync(x => x.Type, x => x.Id);

            var catalogItems = sourceItems.Select(source => new CatalogItem
            {
                Id = source.Id,
                Name = source.Name,
                Description = source.Description,
                Price = source.Price,
                CatalogBrandId = brandIdsByName[source.Brand],
                CatalogTypeId = typeIdsByName[source.Type],
                AvailableStock = 100,
                MaxStockThreshold = 200,
                RestockThreshold = 10,
                PictureFileName = $"{source.Id}.webp",
            }).ToArray();

            await _context.CatalogItems.AddRangeAsync(catalogItems);
            _logger.LogInformation("Seeded catalog with {NumItems} items", _context.CatalogItems.Count());
            await _context.SaveChangesAsync();
        }
    }

    public async Task SeedAsync()
    {
        //await MigrateAsync();

        await EnsureAdminIdentitiesAsync();
    }

    private async Task MigrateAsync()
    {
        await _context.Database.MigrateAsync();
    }

    //public async Task SeedAsync(CatalogContext context)
    //{
    //    var useCustomizationData = settings.Value.UseCustomizationData;
    //    var contentRootPath = env.ContentRootPath;
    //    var picturePath = env.WebRootPath;

    //    // Workaround from https://github.com/npgsql/efcore.pg/issues/292#issuecomment-388608426

    //    if (!context.CatalogItems.Any())
    //    {
    //        var sourcePath = Path.Combine(contentRootPath, "Setup", "catalog.json");
    //        var sourceJson = File.ReadAllText(sourcePath);
    //        var sourceItems = JsonSerializer.Deserialize<CatalogSourceEntry[]>(sourceJson);

    //        context.CatalogBrands.RemoveRange(context.CatalogBrands);
    //        await context.CatalogBrands.AddRangeAsync(sourceItems.Select(x => x.Brand).Distinct()
    //            .Select(brandName => new CatalogBrand { Brand = brandName }));
    //        logger.LogInformation("Seeded catalog with {NumBrands} brands", context.CatalogBrands.Count());

    //        context.CatalogTypes.RemoveRange(context.CatalogTypes);
    //        await context.CatalogTypes.AddRangeAsync(sourceItems.Select(x => x.Type).Distinct()
    //            .Select(typeName => new CatalogType { Type = typeName }));
    //        logger.LogInformation("Seeded catalog with {NumTypes} types", context.CatalogTypes.Count());

    //        await context.SaveChangesAsync();

    //        var brandIdsByName = await context.CatalogBrands.ToDictionaryAsync(x => x.Brand, x => x.Id);
    //        var typeIdsByName = await context.CatalogTypes.ToDictionaryAsync(x => x.Type, x => x.Id);

    //        var catalogItems = sourceItems.Select(source => new CatalogItem
    //        {
    //            Id = source.Id,
    //            Name = source.Name,
    //            Description = source.Description,
    //            Price = source.Price,
    //            CatalogBrandId = brandIdsByName[source.Brand],
    //            CatalogTypeId = typeIdsByName[source.Type],
    //            AvailableStock = 100,
    //            MaxStockThreshold = 200,
    //            RestockThreshold = 10,
    //            PictureFileName = $"{source.Id}.webp",
    //        }).ToArray();

    //        //if (catalogAI.IsEnabled)
    //        //{
    //        //    logger.LogInformation("Generating {NumItems} embeddings", catalogItems.Length);
    //        //    IReadOnlyList<Vector> embeddings = await catalogAI.GetEmbeddingsAsync(catalogItems);
    //        //    for (int i = 0; i < catalogItems.Length; i++)
    //        //    {
    //        //        //catalogItems[i].Embedding = embeddings[i];
    //        //    }
    //        //}

    //        await context.CatalogItems.AddRangeAsync(catalogItems);
    //        logger.LogInformation("Seeded catalog with {NumItems} items", context.CatalogItems.Count());
    //        await context.SaveChangesAsync();
    //    }
    //}

    private class CatalogSourceEntry
    {
        public int Id { get; set; }
        public string Type { get; set; }
        public string Brand { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
    }
}
