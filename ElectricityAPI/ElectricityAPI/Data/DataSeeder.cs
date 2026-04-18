using BLL.Models;
using Core.Entities;
using DAL;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.Text.Json;

namespace ElectricityAPI.Data
{
    public static class DatabaseSeeder
    {
        public static async Task SeedUsersAsync(AppDbContext context)
        {
            const string defaultUserEmail = "user@electricity.local";
            const string defaultAdminEmail = "admin@electricity.local";

            if (!await context.Users.AnyAsync(u => u.Email == defaultUserEmail))
            {
                context.Users.Add(new User
                {
                    Username = "user",
                    Email = defaultUserEmail,
                    PasswordHash = HashPassword("User123!"),
                    IsAdmin = false,
                    CreatedAt = DateTime.UtcNow
                });
            }

            if (!await context.Users.AnyAsync(u => u.Email == defaultAdminEmail))
            {
                context.Users.Add(new User
                {
                    Username = "admin",
                    Email = defaultAdminEmail,
                    PasswordHash = HashPassword("Admin123!"),
                    IsAdmin = true,
                    CreatedAt = DateTime.UtcNow
                });
            }

            await context.SaveChangesAsync();
        }

        public static async Task SeedBuildingsAsync(AppDbContext context)
        {
            if (context.Buildings.Any()) return;

            string districtGeoJsonPath = "districts.geojson";
            Dictionary<string, Geometry> districtPolygons = new Dictionary<string, Geometry>();

            if (File.Exists(districtGeoJsonPath))
            {
                string geoJson = await File.ReadAllTextAsync(districtGeoJsonPath);
                GeoJsonReader reader = new GeoJsonReader();
                FeatureCollection featureCollection = reader.Read<NetTopologySuite.Features.FeatureCollection>(geoJson);

                foreach (Feature feature in featureCollection)
                {
                    string? districtName = feature.Attributes.Exists("name") ? feature.Attributes["name"]?.ToString() : null;
                    if (!string.IsNullOrEmpty(districtName))
                    {
                        districtPolygons[districtName] = feature.Geometry;

                        if (!context.Districts.Any(d => d.Name == districtName))
                        {
                            context.Districts.Add(new District { Name = districtName });
                        }
                    }
                }
                await context.SaveChangesAsync();
            }

            List<District> districtsFromDb = context.Districts.ToList();

            District? fallbackDistrict = districtsFromDb.FirstOrDefault();
            if (fallbackDistrict == null)
            {
                fallbackDistrict = new District { Name = "Невідомий район" };
                context.Districts.Add(fallbackDistrict);
                await context.SaveChangesAsync();
                districtsFromDb.Add(fallbackDistrict);
            }

            string buildingsPath = "buildings.json";
            if (!File.Exists(buildingsPath)) return;

            string buildingsJson = await File.ReadAllTextAsync(buildingsPath);
            List<OsmBuildingJsonDTO> osmBuildings = JsonSerializer.Deserialize<List<OsmBuildingJsonDTO>>(buildingsJson) ?? new List<OsmBuildingJsonDTO>();
            List<Building> buildingsToInsert = new List<Building>();

            GeometryFactory geometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);

            Random rnd = new Random();
            string[] randomMaterials = new[] { "brick", "concrete", "wood", "glass", "cement_block" };

            foreach (OsmBuildingJsonDTO osm in osmBuildings)
            {
                int.TryParse(osm.Levels, out int parsedFloors);

                int floors = parsedFloors > 0 ? parsedFloors : 1;

                string material = string.IsNullOrWhiteSpace(osm.Material)
                    ? randomMaterials[rnd.Next(randomMaterials.Length)]
                    : osm.Material;

                string fullAddress = $"{osm.Street}, {osm.HouseNumber}".Trim(',', ' ');

                double lon = osm.Coordinates != null && osm.Coordinates.Count > 0 ? osm.Coordinates[0] : 0;
                double lat = osm.Coordinates != null && osm.Coordinates.Count > 1 ? osm.Coordinates[1] : 0;

                int assignedDistrictId = fallbackDistrict.Id;

                if (lon != 0 && lat != 0)
                {
                    Point buildingPoint = geometryFactory.CreatePoint(new Coordinate(lon, lat));

                    foreach (KeyValuePair<string, Geometry> polygon in districtPolygons)
                    {
                        if (polygon.Value.Contains(buildingPoint))
                        {
                            District? matchedDistrict = districtsFromDb.FirstOrDefault(d => d.Name == polygon.Key);
                            if (matchedDistrict != null)
                            {
                                assignedDistrictId = matchedDistrict.Id;
                            }
                            break;
                        }
                    }
                }

                buildingsToInsert.Add(new Building
                {
                    Type = osm.BuildingType ?? "unknown",
                    Address = fullAddress,
                    Name = osm.Name ?? string.Empty,
                    Floors = floors, 
                    Material = material,
                    Area = osm.Area,
                    Longitude = lon,
                    Latitude = lat,
                    DistrictId = assignedDistrictId,
                    AverageConsumption = CalculateRealisticConsumption(osm.Area, floors, osm.BuildingType ?? "", material, rnd)
                });
            }

            context.Buildings.AddRange(buildingsToInsert);
            await context.SaveChangesAsync();
        }

        private static double CalculateRealisticConsumption(double footprintArea, int floors, string type, string material, Random rnd)
        {
            double totalArea = (footprintArea > 0 ? footprintArea : 100) * floors;

            double baseConsumption = totalArea * 0.15;

            double typeMultiplier = 1.0;
            switch (type.ToLower())
            {
                // Базове споживання та житло
                case "building":
                case "others (amenity)":
                case "apartments":
                case "residential":
                case "house":
                case "detached":
                    typeMultiplier = 1.0;
                    break;
                case "dormitory":
                    typeMultiplier = 1.1;
                    break;

                // Освіта
                case "kindergarten":
                case "school":
                    typeMultiplier = 1.2;
                    break;
                case "college":
                case "university":
                case "education":
                    typeMultiplier = 1.3;
                    break;

                // Фінанси
                case "office":
                case "government":
                case "public":
                case "financial":
                    typeMultiplier = 1.5;
                    break;

                // Інфраструктура
                case "waste":
                    typeMultiplier = 1.6;
                    break;
                case "transportation":
                    typeMultiplier = 1.7;
                    break;

                // Готелі
                case "hotel":
                    typeMultiplier = 1.8;
                    break;
                case "entertainment":
                    typeMultiplier = 2.0;
                    break;

                // Медицина
                case "healthcare":
                    typeMultiplier = 2.0;
                    break;
                case "clinic":
                    typeMultiplier = 2.3;
                    break;
                case "hospital":
                    typeMultiplier = 3.0;
                    break;

                // Торгівля
                case "shop":
                case "retail":
                    typeMultiplier = 2.2;
                    break;
                case "commercial":
                    typeMultiplier = 2.5;
                    break;
                case "sustenance":
                    typeMultiplier = 2.8;
                    break;
            }

            double materialMultiplier = 1.0;
            switch (material.ToLower())
            {
                case "glass": materialMultiplier = 1.3; break;
                case "concrete": materialMultiplier = 1.1; break;
                case "brick": materialMultiplier = 0.9; break;
                case "wood": materialMultiplier = 0.85; break;
                case "cement_block": materialMultiplier = 1.0; break;
            }

            double finalConsumption = baseConsumption * typeMultiplier * materialMultiplier;
            double variance = 1.0 + ((rnd.NextDouble() * 0.2) - 0.1);

            return Math.Round(finalConsumption * variance, 2);
        }

        private static string HashPassword(string password)
        {
            const int iterations = 100_000;
            byte[] salt = RandomNumberGenerator.GetBytes(16);
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, 32);

            return $"{iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
        }
    }
}
