using System;
using System.Collections.Generic;
using System.Linq;
using Mapster;
using MyApi.DAL.DTO.Response;
using MyApi.DAL.Models;

namespace MyApi.BLL.MapesterConfigurations
{
    public static class MapesterConfig
    {
        public static void MapesterConfRegister()
        {
            TypeAdapterConfig<Category, CategoryResponse>.NewConfig()
            .Map(dest => dest.CreatedBy, source => source.CreatedBy);

            TypeAdapterConfig<Category, CategoryUserResponse>.NewConfig()
            .Map(dest => dest.Name, source => source.Translations.
            Where(t => t.Language == MapContext.Current.Parameters["lang"].ToString())
            .Select(t => t.Name).FirstOrDefault());

            TypeAdapterConfig<Product, ProductResponse>.NewConfig()
.Map(dest => dest.MainImage, source =>
    string.IsNullOrEmpty(source.MainImage)
        ? null
        : source.MainImage.StartsWith("/images/")
            ? source.MainImage
            : $"/images/{source.MainImage}");

            TypeAdapterConfig<Product, ProductUserResponse>.NewConfig()
.Map(dest => dest.MainImage, source =>
    string.IsNullOrEmpty(source.MainImage)
        ? null
        : source.MainImage.StartsWith("/images/")
            ? source.MainImage
            : $"/images/{source.MainImage}")
            .Map(dest => dest.Name, source => source.Translations.
            Where(t => t.Language == MapContext.Current.Parameters["lang"].ToString())
            .Select(t => t.Name).FirstOrDefault());

            TypeAdapterConfig<Product, ProductUserDetails>.NewConfig()
.Map(dest => dest.MainImage, source =>
    string.IsNullOrEmpty(source.MainImage)
        ? null
        : source.MainImage.StartsWith("/images/")
            ? source.MainImage
            : $"/images/{source.MainImage}")
            .Map(dest => dest.Name, source => source.Translations
            .Where(t => t.Language == MapContext.Current.Parameters["lang"].ToString())
            .Select(t => t.Name).FirstOrDefault())
            .Map(dest => dest.Description, source => source.Translations
            .Where(t => t.Language == MapContext.Current.Parameters["lang"].ToString())
            .Select(t => t.Description).FirstOrDefault())
.Map(dest => dest.SubImages,
    source => source.SubImages.Select(x => x.ImageName));

            TypeAdapterConfig<Order, OrderResponse>.NewConfig()
            .Map(dest => dest.UserName, source => source.User.UserName)
                .Map(dest => dest.OrderStatus, src => src.OrderStatus.ToString())
    .Map(dest => dest.PaymentStatus, src => src.PaymentStatus.ToString());

            TypeAdapterConfig<Reviews, ReviewResponse>.NewConfig()
            .Map(dest => dest.FullName, source => source.User.FullName);
        }
    }
}