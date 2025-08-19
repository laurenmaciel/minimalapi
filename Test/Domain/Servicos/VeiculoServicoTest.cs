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
        var context = CriarContextoDeTeste();
        context.Database.ExecuteSqlRaw("TRUNCATE TABLE Veiculos");

        var veiculo = new Veiculo();
        veiculo.Marca = "Chevrolet";
        veiculo.Nome = "Onix";
        veiculo.Ano = 2022;

        var veiculoServico = new VeiculoServico(context);

        // Act
        veiculoServico.Incluir(veiculo);

        // Assert
        Assert.AreEqual(1, veiculoServico.Todos(1).Count());
    }

    [TestMethod]
    public void TestandoBuscaPorId()
    {
        // Arrange
        var context = CriarContextoDeTeste();
        context.Database.ExecuteSqlRaw("TRUNCATE TABLE Veiculos");

        var veiculo = new Veiculo();
        veiculo.Marca = "Volkswagen";
        veiculo.Nome = "Gol";
        veiculo.Ano = 2020;

        var veiculoServico = new VeiculoServico(context);
        veiculoServico.Incluir(veiculo);

        // Act
        var veiculoDoBanco = veiculoServico.BuscaPorId(veiculo.Id);

        // Assert
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

        var veiculo1 = new Veiculo { Marca = "Ford", Nome = "Ka", Ano = 2018 };
        var veiculo2 = new Veiculo { Marca = "Hyundai", Nome = "HB20", Ano = 2021 };

        var veiculoServico = new VeiculoServico(context);
        veiculoServico.Incluir(veiculo1);
        veiculoServico.Incluir(veiculo2);

        // Act
        veiculoServico.Apagar(veiculo2);

        // Assert
        Assert.AreEqual(1, veiculoServico.Todos(1).Count());
        Assert.IsNull(veiculoServico.BuscaPorId(veiculo2.Id));
    }
}

