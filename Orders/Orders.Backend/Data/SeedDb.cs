﻿using Orders.Shared.Entities;
using Orders.Backend.Services;
using Microsoft.EntityFrameworkCore;
using Orders.Shared.Responses;
using Orders.Backend.UnitsWork.Interfaces;
using Orders.Shared.Enums;

namespace Orders.Backend.Data
{
    public class SeedDb
    {
        private readonly DataContext _context;
        private readonly IApiService _apiService;
        private readonly IUsersUnitOfWork _usersUnitOfWork;

        public SeedDb(DataContext context, IApiService apiService, IUsersUnitOfWork usersUnitOfWork)
        {
            _context = context;
            _apiService = apiService;
            _usersUnitOfWork = usersUnitOfWork;
        }

        public async Task SeedAsync()
        {
            await _context.Database.EnsureCreatedAsync();
            await CheckCountriesAsync();
            await CheckCategoriesAsync();
            await CheckRolesAsync();
            await CheckUserAsync("1010", "Luis", "Viana", "viana1217@yopmail.com", "3113660723", "Calle Luna Calle Sol", UserType.Admin);
            

        }

        private async Task CheckCountriesAsync()
        {
            if (!_context.Countries.Any())
            {

                var responseCountries = await _apiService.GetAsync<List<CountryResponse>>("/v1", "/countries");
                if (responseCountries.WasSuccess)
                {
                    var countries = responseCountries.Result!;
                    foreach (var CountryResponse in countries)
                    {
                        var country = await _context.Countries.FirstOrDefaultAsync(c => c.Name == CountryResponse.Name!)!;
                        if (country == null)
                        {
                            country = new() { Name = CountryResponse.Name!, States = new List<State>() };
                            var responseStates = await _apiService.GetAsync<List<StateResponse>>("/v1", $"/countries/{CountryResponse.Iso2}/states");
                            if (responseStates.WasSuccess)
                            {
                                var states = responseStates.Result!;
                                foreach (var StateResponse in states!)
                                {
                                    var state = country.States!.FirstOrDefault(s => s.Name == StateResponse.Name!)!;
                                    if (state == null)
                                    {
                                        state = new() { Name = StateResponse.Name!, Cities = new List<City>() };
                                        var responseCities = await _apiService.GetAsync<List<CityResponse>>("/v1", $"/countries/{CountryResponse.Iso2}/states/{StateResponse.Iso2}/cities");
                                        if (responseCities.WasSuccess)
                                        {
                                            var cities = responseCities.Result!;
                                            foreach (var CityResponse in cities)
                                            {
                                                if (CityResponse.Name == "Mosfellsbær" || CityResponse.Name == "Șăulița")
                                                {
                                                    continue;
                                                }
                                                var city = state.Cities!.FirstOrDefault(c => c.Name == CityResponse.Name!)!;
                                                if (city == null)
                                                {
                                                    state.Cities.Add(new City() { Name = CityResponse.Name! });
                                                }
                                            }
                                        }
                                        if (state.CitiesNumber > 0)
                                        {
                                            country.States.Add(state);
                                        }
                                    }
                                }
                            }
                            if (country.StatesNumber > 0)
                            {
                                _context.Countries.Add(country);
                                await _context.SaveChangesAsync();
                            }
                        }
                    }
                }

                //-------------------------------------------

                //_context.Countries.Add(new Country
                //{
                //    Name = "Colombia",
                //    States = new List<State>()
                //{
                //    new State()
                //    {
                //        Name = "Antioquia",
                //        Cities = new List<City>() {
                //            new City() { Name = "Medellín" },
                //            new City() { Name = "Itagüí" },
                //            new City() { Name = "Envigado" },
                //            new City() { Name = "Bello" },
                //            new City() { Name = "Rionegro" },
                //        }
                //    },
                //    new State()
                //    {
                //        Name = "Bogotá",
                //        Cities = new List<City>() {
                //            new City() { Name = "Usaquen" },
                //            new City() { Name = "Champinero" },
                //            new City() { Name = "Santa fe" },
                //            new City() { Name = "Useme" },
                //            new City() { Name = "Bosa" },
                //        }
                //    },
                //}
                //});
                //_context.Countries.Add(new Country
                //{
                //    Name = "Estados Unidos",
                //    States = new List<State>()
                //{
                //    new State()
                //    {
                //        Name = "Florida",
                //        Cities = new List<City>() {
                //            new City() { Name = "Orlando" },
                //            new City() { Name = "Miami" },
                //            new City() { Name = "Tampa" },
                //            new City() { Name = "Fort Lauderdale" },
                //            new City() { Name = "Key West" },
                //        }
                //    },
                //    new State()
                //    {
                //        Name = "Texas",
                //        Cities = new List<City>() {
                //            new City() { Name = "Houston" },
                //            new City() { Name = "San Antonio" },
                //            new City() { Name = "Dallas" },
                //            new City() { Name = "Austin" },
                //            new City() { Name = "El Paso" },
                //        }
                //    },
                //}
                //});
            }

            await _context.SaveChangesAsync();
        }

        private async Task CheckCategoriesAsync()
        {
            if (!_context.Categories.Any())
            {
                _context.Categories.Add(new Shared.Entities.Category { Name = "Electrodomesticos" });
                _context.Categories.Add(new Shared.Entities.Category { Name = "Licores" });
                _context.Categories.Add(new Shared.Entities.Category { Name = "Calzado" });
                _context.Categories.Add(new Shared.Entities.Category { Name = "Jugeteria" });
                await _context.SaveChangesAsync();
            }
        }

        private async Task CheckRolesAsync()
        {
            await _usersUnitOfWork.CheckRoleAsync(UserType.Admin.ToString());
            await _usersUnitOfWork.CheckRoleAsync(UserType.User.ToString());
        }

        private async Task<User> CheckUserAsync(string document, string firstName, string lastName, string email, string phone, string address, UserType userType)
        {
            var user = await _usersUnitOfWork.GetUserAsync(email);
            if (user == null)
            {
                user = new User
                {
                    FirstName = firstName,
                    LastName = lastName,
                    Email = email,
                    UserName = email,
                    PhoneNumber = phone,
                    Address = address,
                    Document = document,
                    City = _context.Cities.FirstOrDefault(),
                    UserType = userType,
                };

                await _usersUnitOfWork.AddUserAsync(user, "123456");
                await _usersUnitOfWork.AddUserToRoleAsync(user, userType.ToString());
            }

            return user;
        }


    }
}
