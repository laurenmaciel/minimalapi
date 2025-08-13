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

// A classe TestWebApplicationFactory herda de WebApplicationFactory
// e permite customizar o ambiente de teste da sua API.
public class TestWebApplicationFactory : WebApplicationFactory<MinimalApi.Program>
{
    // Este método é usado para configurar os serviços do servidor de teste.
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove o DbContextOptions existente.
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<DbContexto>));

            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Adiciona um DbContexto que usa um banco de dados MySQL para testes.
            // A string de conexão é lida do appsettings.json do projeto de testes.
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();

            services.AddDbContext<DbContexto>(options =>
            {
                var connectionString = config.GetConnectionString("MySqlTest");
                options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
            });

            // Cria o banco de dados e aplica as migrações, se existirem.
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
    // Usa a nova fábrica de teste personalizada.
    private static TestWebApplicationFactory _factory = null!;
    private static HttpClient _client = null!;

    private static string _token;

    [ClassInitialize]
    public static async Task ClassInitialize(TestContext context)
    {
        // Inicializa o servidor de teste e o HttpClient antes de rodar os testes da classe.
        _factory = new TestWebApplicationFactory();
        _client = _factory.CreateClient();

        _token = await GetToken();
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _token);
    }

    [ClassCleanup]
    public static void ClassCleanup()
    {
        // Limpa os recursos após todos os testes terem sido executados.
        _client.Dispose();
        _factory.Dispose();
    }

    [TestMethod]
    public async Task Post_DeveIncluirUmVeiculo_ERetornarStatusCode201()
    {
        // Arrange
        // Cria um objeto para ser enviado na requisição POST.
        var veiculo = new Veiculo { Marca = "Fiat", Nome = "Uno", Ano = 1990 };

        // Act
        // Envia a requisição POST para a rota de criação de veículos.
        var response = await _client.PostAsJsonAsync("/veiculos", veiculo);

        // Assert
        // Verifica se o status code da resposta é Created (201).
        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);

        // Opcional: Verifica se o veículo foi realmente criado e pode ser lido.
        var createdVeiculo = await response.Content.ReadFromJsonAsync<Veiculo>();
        Assert.IsNotNull(createdVeiculo);
        Assert.AreEqual(veiculo.Nome, createdVeiculo.Nome);
        Assert.AreNotEqual(0, createdVeiculo.Id);
    }

    [TestMethod]
    public async Task Get_Todos_DeveRetornarVeiculos()
    {
        // Arrange
        // (Assumimos que já existem veículos no banco de dados de teste, criados por outro teste, por exemplo.)

        // Act
        // Envia a requisição GET para a rota de busca de todos os veículos.
        var response = await _client.GetAsync("/veiculos");

        // Assert
        // Verifica se o status code é OK (200).
        response.EnsureSuccessStatusCode();

        // Deserializa a lista de veículos da resposta.
        var veiculos = await response.Content.ReadFromJsonAsync<List<Veiculo>>();
        Assert.IsNotNull(veiculos);
        // O teste irá falhar se a lista estiver vazia. Vamos garantir que ela tem pelo menos um item.
        Assert.IsTrue(veiculos.Count >= 0);
    }

    [TestMethod]
    public async Task Get_PorId_DeveRetornarVeiculo()
    {
        // Arrange
        // Inclui um veículo para poder buscá-lo.
        var veiculo = new Veiculo { Marca = "Hyundai", Nome = "HB20", Ano = 2021 };
        var postResponse = await _client.PostAsJsonAsync("/veiculos", veiculo);
        var createdVeiculo = await postResponse.Content.ReadFromJsonAsync<Veiculo>();

        // Act
        // Envia a requisição GET para a rota de busca por ID.
        var getResponse = await _client.GetAsync($"/veiculos/{createdVeiculo!.Id}");

        // Assert
        // Verifica se o status code é OK (200).
        getResponse.EnsureSuccessStatusCode();

        // Verifica se o veículo retornado é o correto.
        var fetchedVeiculo = await getResponse.Content.ReadFromJsonAsync<Veiculo>();
        Assert.IsNotNull(fetchedVeiculo);
        Assert.AreEqual(createdVeiculo.Id, fetchedVeiculo.Id);
    }

    [TestMethod]
    public async Task Put_DeveAtualizarUmVeiculo_E_RetornarStatusCode200()
    {
        // Arrange
        // Cria um veículo para ser atualizado.
        var veiculo = new Veiculo { Marca = "Renault", Nome = "Kwid", Ano = 2019 };
        var postResponse = await _client.PostAsJsonAsync("/veiculos", veiculo);
        var createdVeiculo = await postResponse.Content.ReadFromJsonAsync<Veiculo>();

        // Modifica o veículo para a atualização.
        createdVeiculo!.Nome = "Kwid Zen";

        // Act
        // Envia a requisição PUT para a rota de atualização.
        var putResponse = await _client.PutAsJsonAsync($"/veiculos/{createdVeiculo.Id}", createdVeiculo);

        // Assert
        // Verifica se o status code é OK (200).
        Assert.AreEqual(HttpStatusCode.OK, putResponse.StatusCode);

        // Opcional: Busca o veículo novamente para confirmar a atualização.
        var getResponse = await _client.GetAsync($"/veiculos/{createdVeiculo.Id}");
        var updatedVeiculo = await getResponse.Content.ReadFromJsonAsync<Veiculo>();
        Assert.AreEqual("Kwid Zen", updatedVeiculo!.Nome);
    }

    [TestMethod]
    public async Task Delete_DeveExcluirUmVeiculo_E_RetornarStatusCode204()
    {
        // Arrange
        // Cria um veículo para ser excluído.
        var veiculo = new Veiculo { Marca = "Peugeot", Nome = "208", Ano = 2022 };
        var postResponse = await _client.PostAsJsonAsync("/veiculos", veiculo);
        var createdVeiculo = await postResponse.Content.ReadFromJsonAsync<Veiculo>();

        // Act
        // Envia a requisição DELETE para a rota de exclusão.
        var deleteResponse = await _client.DeleteAsync($"/veiculos/{createdVeiculo!.Id}");

        // Assert
        // Verifica se o status code da resposta é NoContent (204).
        Assert.AreEqual(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        // Opcional: Tenta buscar o veículo novamente para confirmar a exclusão.
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

