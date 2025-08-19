using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MinimalApi.Dominio.Entidades;
using MinimalApi.Infraestrutura.Db;
using System;
using System.Linq;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System.Text;
using System.Text.Json;
using MinimalApi.Dominio.ModelViews;
using MinimalApi.DTOs;
using Test.Helpers;

namespace Test.API;

public class TestWebApplicationFactory : WebApplicationFactory<MinimalApi.Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<DbContexto>));

            if (descriptor != null)
            {
                services.Remove(descriptor);
            }
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();

            services.AddDbContext<DbContexto>(options =>
            {
                var connectionString = config.GetConnectionString("MySqlTest");
                options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
            });

            var serviceProvider = services.BuildServiceProvider();

            using (var scope = serviceProvider.CreateScope())
            {
                var scopedServices = scope.ServiceProvider;
                var dbContext = scopedServices.GetRequiredService<DbContexto>();

                try
                {
                    dbContext.Database.EnsureDeleted();
                    dbContext.Database.EnsureCreated();

                    dbContext.Administradores.Add(new Administrador
                    {
                        Email = "administrador@teste.com",
                        Senha = "123456",
                        Perfil = "Adm"
                    });

                    dbContext.SaveChanges();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erro ao criar o banco de dados de testes: {ex.Message}");
                    throw;
                }
            }
        });
    }
}

[TestClass]
public class VeiculoApiIntegrationTest
{
    private static TestWebApplicationFactory _factory = null!;
    private static HttpClient _client = null!;

    private static string _token;

    [ClassInitialize]
    public static async Task ClassInitialize(TestContext context)
    {
        _factory = new TestWebApplicationFactory();
        _client = _factory.CreateClient();

        _token = await GetToken();
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _token);
    }

    [ClassCleanup]
    public static void ClassCleanup()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [TestMethod]
    public async Task Post_DeveIncluirUmVeiculo_ERetornarStatusCode201()
    {
        // Arrange
        var veiculo = new Veiculo { Marca = "Fiat", Nome = "Uno", Ano = 1990 };

        // Act
        var response = await _client.PostAsJsonAsync("/veiculos", veiculo);

        // Assert
        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);

        var createdVeiculo = await response.Content.ReadFromJsonAsync<Veiculo>();
        Assert.IsNotNull(createdVeiculo);
        Assert.AreEqual(veiculo.Nome, createdVeiculo.Nome);
        Assert.AreNotEqual(0, createdVeiculo.Id);
    }

    [TestMethod]
    public async Task Get_Todos_DeveRetornarVeiculos()
    {

        // Act
        var response = await _client.GetAsync("/veiculos");

        // Assert
        response.EnsureSuccessStatusCode();

        var veiculos = await response.Content.ReadFromJsonAsync<List<Veiculo>>();
        Assert.IsNotNull(veiculos);
        Assert.IsTrue(veiculos.Count >= 0);
    }

    [TestMethod]
    public async Task Get_PorId_DeveRetornarVeiculo()
    {
        // Arrange
        var veiculo = new Veiculo { Marca = "Hyundai", Nome = "HB20", Ano = 2021 };
        var postResponse = await _client.PostAsJsonAsync("/veiculos", veiculo);
        var createdVeiculo = await postResponse.Content.ReadFromJsonAsync<Veiculo>();

        // Act
        var getResponse = await _client.GetAsync($"/veiculos/{createdVeiculo!.Id}");

        // Assert
        getResponse.EnsureSuccessStatusCode();
        
        var fetchedVeiculo = await getResponse.Content.ReadFromJsonAsync<Veiculo>();
        Assert.IsNotNull(fetchedVeiculo);
        Assert.AreEqual(createdVeiculo.Id, fetchedVeiculo.Id);
    }

    [TestMethod]
    public async Task Put_DeveAtualizarUmVeiculo_E_RetornarStatusCode200()
    {
        // Arrange
        var veiculo = new Veiculo { Marca = "Renault", Nome = "Kwid", Ano = 2019 };
        var postResponse = await _client.PostAsJsonAsync("/veiculos", veiculo);
        var createdVeiculo = await postResponse.Content.ReadFromJsonAsync<Veiculo>();

        createdVeiculo!.Nome = "Kwid Zen";

        // Act
        var putResponse = await _client.PutAsJsonAsync($"/veiculos/{createdVeiculo.Id}", createdVeiculo);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, putResponse.StatusCode);

        var getResponse = await _client.GetAsync($"/veiculos/{createdVeiculo.Id}");
        var updatedVeiculo = await getResponse.Content.ReadFromJsonAsync<Veiculo>();
        Assert.AreEqual("Kwid Zen", updatedVeiculo!.Nome);
    }

    [TestMethod]
    public async Task Delete_DeveExcluirUmVeiculo_E_RetornarStatusCode204()
    {
        // Arrange
        var veiculo = new Veiculo { Marca = "Peugeot", Nome = "208", Ano = 2022 };
        var postResponse = await _client.PostAsJsonAsync("/veiculos", veiculo);
        var createdVeiculo = await postResponse.Content.ReadFromJsonAsync<Veiculo>();

        // Act
        var deleteResponse = await _client.DeleteAsync($"/veiculos/{createdVeiculo!.Id}");

        // Assert
        Assert.AreEqual(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await _client.GetAsync($"/veiculos/{createdVeiculo.Id}");
        Assert.AreEqual(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    private async static Task<string> GetToken()
    {
        var content = new StringContent(JsonSerializer.Serialize(new LoginDTO { Email = "administrador@teste.com", Senha = "123456" }), Encoding.UTF8, "Application/json");

        var response = await _client.PostAsync("/administradores/login", content);

        var result = await response.Content.ReadAsStringAsync();
        var admLogado = JsonSerializer.Deserialize<AdministradorLogado>(result, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return admLogado?.Token;
    }
}

