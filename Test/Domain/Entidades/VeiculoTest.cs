using MinimalApi.Dominio.Entidades;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test.Domain.Entidades
{

    [TestClass]
    public class VeiculoTest
    {
        [TestMethod]
        public void TestarGetSetPropriedades()
        {
            // Arrange
            var veiculo = new Veiculo();

            // Act
            veiculo.Id = 1;
            veiculo.Nome = "Fiat";
            veiculo.Marca= "Uno";
            veiculo.Ano = 2022;

            // Assert
            Assert.AreEqual(1, veiculo.Id);
            Assert.AreEqual("Fiat", veiculo.Nome);
            Assert.AreEqual("Uno", veiculo.Marca);
            Assert.AreEqual(2022, veiculo.Ano);
        }
    }
}

