using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MinimalApi.Dominio.Entidades;
using MinimalApi.Dominio.Servicos;
using MinimalApi.Infraestrutura.Db;

namespace Test.Domain.Servicos;

[TestClass]
public class VeiculoServicoTest
{
    // Método para criar um contexto de banco de dados em memória para os testes.
    // É uma boa prática isolar os testes de persistência do banco de dados real.
    private DbContexto CriarContextoDeTeste()
    {
        var assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        var path = Path.GetFullPath(Path.Combine(assemblyPath ?? "", "..", "..", ".."));

        var configuration = new ConfigurationBuilder()
            .SetBasePath(path ?? Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();

        var options = new DbContextOptionsBuilder<DbContexto>()
            .UseMySql(
                configuration.GetConnectionString("MySqlTest"),
                ServerVersion.AutoDetect(configuration.GetConnectionString("MySqlTest"))
            )
            .Options;

        return new DbContexto(options);
    }

    [TestMethod]
    public void TestandoSalvarVeiculo()
    {
        // Arrange
        // Cria um novo contexto de banco de dados e garante que a tabela de Veiculos está vazia.
        var context = CriarContextoDeTeste();
        context.Database.ExecuteSqlRaw("TRUNCATE TABLE Veiculos");

        // Cria um novo objeto Veiculo para ser salvo com as novas propriedades.
        var veiculo = new Veiculo();
        veiculo.Marca = "Chevrolet";
        veiculo.Nome = "Onix";
        veiculo.Ano = 2022;

        var veiculoServico = new VeiculoServico(context);

        // Act
        // Chama o método para incluir o veículo no banco de dados.
        veiculoServico.Incluir(veiculo);

        // Assert
        // Verifica se o número de veículos no banco de dados é 1.
        Assert.AreEqual(1, veiculoServico.Todos(1).Count());
    }

    [TestMethod]
    public void TestandoBuscaPorId()
    {
        // Arrange
        // Cria um novo contexto e limpa a tabela de Veiculos.
        var context = CriarContextoDeTeste();
        context.Database.ExecuteSqlRaw("TRUNCATE TABLE Veiculos");

        // Cria e salva um veículo para o teste de busca.
        var veiculo = new Veiculo();
        veiculo.Marca = "Volkswagen";
        veiculo.Nome = "Gol";
        veiculo.Ano = 2020;

        var veiculoServico = new VeiculoServico(context);
        veiculoServico.Incluir(veiculo);

        // Act
        // Busca o veículo que acabou de ser salvo usando seu ID.
        var veiculoDoBanco = veiculoServico.BuscaPorId(veiculo.Id);

        // Assert
        // Verifica se o veículo retornado é o mesmo que foi salvo, usando as novas propriedades.
        Assert.IsNotNull(veiculoDoBanco);
        Assert.AreEqual(veiculo.Id, veiculoDoBanco.Id);
        Assert.AreEqual(veiculo.Nome, veiculoDoBanco.Nome);
        Assert.AreEqual(veiculo.Marca, veiculoDoBanco.Marca);
        Assert.AreEqual(veiculo.Ano, veiculoDoBanco.Ano);
    }

    [TestMethod]
    public void TestandoExclusao()
    {
        // Arrange
        var context = CriarContextoDeTeste();
        context.Database.ExecuteSqlRaw("TRUNCATE TABLE Veiculos");

        // Cria e salva dois veículos com as novas propriedades para o teste de exclusão.
        var veiculo1 = new Veiculo { Marca = "Ford", Nome = "Ka", Ano = 2018 };
        var veiculo2 = new Veiculo { Marca = "Hyundai", Nome = "HB20", Ano = 2021 };

        var veiculoServico = new VeiculoServico(context);
        veiculoServico.Incluir(veiculo1);
        veiculoServico.Incluir(veiculo2);

        // Act
        veiculoServico.Apagar(veiculo2);

        // Assert
        // Verifica se agora existe apenas um veículo no banco de dados e se o veículo excluído não pode ser encontrado.
        Assert.AreEqual(1, veiculoServico.Todos(1).Count());
        Assert.IsNull(veiculoServico.BuscaPorId(veiculo2.Id));
    }
}

