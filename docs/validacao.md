# Validação da base InvoiSys

## Validação estática realizada

A base atual foi revisada estaticamente após a criação da documentação completa.

Resultados:

```text
Projetos (.csproj): 10
Arquivos C#: 144
Arquivos Razor: 11
Documentação completa: 3541 linhas
XML/JSON dos arquivos principais: válido
PackageReference sem PackageVersion correspondente: 0
ProjectReference apontando para arquivo inexistente: 0
Referências antigas InvoiSys.Coleta no código: 0
Referências indevidas a projetos externos no código: 0
Erros estáticos detectados pelo script de consistência: 0
```

## O que ainda não pôde ser executado neste ambiente

Este ambiente de geração não possui o executável `dotnet` nem Docker. Por isso, ainda não foi possível executar aqui:

```powershell
dotnet restore InvoiSys.sln
dotnet build InvoiSys.sln
dotnet test tests\InvoiSys.UnitTests\InvoiSys.UnitTests.csproj
dotnet test tests\InvoiSys.IntegrationTests\InvoiSys.IntegrationTests.csproj
```

O teste de integração também permanece marcado como `Skip` enquanto a migration `InitialCreate` não for gerada e o ambiente Docker não estiver preparado.

## Critério para ZIP final

O ZIP final só deve ser considerado validado depois de:

1. gerar a migration `InitialCreate`;
2. executar `dotnet restore`;
3. executar `dotnet build` sem erros;
4. executar os testes unitários;
5. habilitar e executar os testes de integração com Docker;
6. atualizar esta documentação caso alguma correção altere o código.
