# API de Veículos (Minimal API) 🚗⌨️

## Visão Geral

Uma API REST completa, desenvolvida em ASP.NET Core, que gerencia informações de veículos. O projeto é um exemplo de **arquitetura em camadas** e usa a abordagem **Minimal API** para endpoints.

## Conceitos Aplicados

* **Arquitetura em Camadas:** O projeto é estruturado em camadas (`Dominio`, `Infraestrutura`), separando a **lógica de negócio** (serviços) da **infraestrutura** (banco de dados) e da **apresentação** (endpoints).
* **Injeção de Dependência:** Utiliza a injeção de dependência para conectar as interfaces (`IVeiculoServico`) às suas implementações, tornando o código modular.
* **Testes Unitários e de Integração:** O projeto inclui uma suíte de testes que valida a funcionalidade dos serviços (testes unitários) e a comunicação entre as camadas (testes de integração).
* **Entity Framework Core:** A camada de infraestrutura usa o Entity Framework para a persistência de dados.
