﻿using Microsoft.EntityFrameworkCore;
using Orders.Backend.Data;
using Orders.Backend.Helpers;
using Orders.Backend.Repositories.Interfaces;
using Orders.Shared.DTOs;
using Orders.Shared.Entities;
using Orders.Shared.Responses;

namespace Orders.Backend.Repositories.Implementations
{
    public class ProductsRepository : GenericRepository<Product>, IProductsRepository
    {
        private readonly DataContext _context;
        private readonly IFileStorage _fileStorage;

        public ProductsRepository(DataContext context, IFileStorage fileStorage) : base(context)
        {
            _context = context;
            _fileStorage = fileStorage;
        }

        public override async Task<ActionResponse<IEnumerable<Product>>> GetAsync(PaginationDTO pagination)
        {
            var queryable = _context.Products
                .Include(x => x.ProductImages)
                .Include(x => x.ProductCategories)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(pagination.Filter))
            {
                queryable = queryable.Where(x => x.Name.ToLower().Contains(pagination.Filter.ToLower()));
            }

            return new ActionResponse<IEnumerable<Product>>
            {
                WasSuccess = true,
                Result = await queryable
                    .OrderBy(x => x.Name)
                    .Paginate(pagination)
                    .ToListAsync()
            };
        }

        public override async Task<ActionResponse<int>> GetTotalPagesAsync(PaginationDTO pagination)
        {
            var queryable = _context.Products.AsQueryable();

            if (!string.IsNullOrWhiteSpace(pagination.Filter))
            {
                queryable = queryable.Where(x => x.Name.ToLower().Contains(pagination.Filter.ToLower()));
            }

            double count = await queryable.CountAsync();
            int totalPages = (int)Math.Ceiling(count / pagination.RecordsNumber);
            return new ActionResponse<int>
            {
                WasSuccess = true,
                Result = totalPages
            };
        }

        public override async Task<ActionResponse<Product>> GetAsync(int id)
        {
            var product = await _context.Products
                .Include(x => x.ProductImages)
                .Include(x => x.ProductCategories!)
                .ThenInclude(x => x.Category)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (product == null)
            {
                return new ActionResponse<Product>
                {
                    WasSuccess = false,
                    Message = "Producto no existe"
                };
            }

            return new ActionResponse<Product>
            {
                WasSuccess = true,
                Result = product
            };
        }

        public async Task<ActionResponse<Product>> AddFullAsync(ProductDTO productDTO)
        {
            try
            {
                var newProduct = new Product
                {
                    Name = productDTO.Name,
                    Description = productDTO.Description,
                    Price = productDTO.Price,
                    Stock = productDTO.Stock,
                    ProductCategories = new List<ProductCategory>(),
                    ProductImages = new List<ProductImage>()
                };

                foreach (var productImage in productDTO.ProductImages!)
                {
                    var photoProduct = Convert.FromBase64String(productImage);
                    newProduct.ProductImages.Add(new ProductImage { Image = await _fileStorage.SaveFileAsync(photoProduct, ".jpg", "products") });
                }

                foreach (var productCategoryId in productDTO.ProductCategoryIds!)
                {
                    var category = await _context.Categories.FirstOrDefaultAsync(x => x.Id == productCategoryId);
                    if (category != null)
                    {
                        newProduct.ProductCategories.Add(new ProductCategory { Category = category });
                    }
                }

                _context.Add(newProduct);
                await _context.SaveChangesAsync();
                return new ActionResponse<Product>
                {
                    WasSuccess = true,
                    Result = newProduct
                };
            }
            catch (DbUpdateException)
            {
                return new ActionResponse<Product>
                {
                    WasSuccess = false,
                    Message = "Ya existe un producto con el mismo nombre."
                };
            }
            catch (Exception exception)
            {
                return new ActionResponse<Product>
                {
                    WasSuccess = false,
                    Message = exception.Message
                };
            }
        }

        public async Task<ActionResponse<Product>> UpdateFullAsync(ProductDTO productDTO)
        {
            try
            {
                var product = await _context.Products
                    .Include(x => x.ProductCategories!)
                    .ThenInclude(x => x.Category)
                    .FirstOrDefaultAsync(x => x.Id == productDTO.Id);
                if (product == null)
                {
                    return new ActionResponse<Product>
                    {
                        WasSuccess = false,
                        Message = "Producto no existe"
                    };
                }

                product.Name = productDTO.Name;
                product.Description = productDTO.Description;
                product.Price = productDTO.Price;
                product.Stock = productDTO.Stock;

                _context.ProductCategories.RemoveRange(product.ProductCategories!);
                product.ProductCategories = new List<ProductCategory>();

                foreach (var productCategoryId in productDTO.ProductCategoryIds!)
                {
                    var category = await _context.Categories.FindAsync(productCategoryId);
                    if (category != null)
                    {
                        _context.ProductCategories.Add(new ProductCategory { CategoryId = category.Id, ProductId = product.Id });
                    }
                }

                _context.Update(product);
                await _context.SaveChangesAsync();
                return new ActionResponse<Product>
                {
                    WasSuccess = true,
                    Result = product
                };
            }
            catch (DbUpdateException)
            {
                return new ActionResponse<Product>
                {
                    WasSuccess = false,
                    Message = "Ya existe un producto con el mismo nombre."
                };
            }
            catch (Exception exception)
            {
                return new ActionResponse<Product>
                {
                    WasSuccess = false,
                    Message = exception.Message
                };
            }
        }


    }
}
