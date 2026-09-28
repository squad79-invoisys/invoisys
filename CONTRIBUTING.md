# Como contribuir — Squad 79

Guia rápido do fluxo de trabalho no repositório. Leia antes do primeiro commit.

## O fluxo, em 6 passos

```bash
# 1. Atualize a main
git checkout main
git pull

# 2. Crie a branch a partir dela
git checkout -b feat/kan-41-cadastrar-fonte

# 3. Trabalhe e vá commitando
git add .
git commit -m "feat: adiciona formulário de cadastro de fonte"

# 4. Suba a branch
git push -u origin feat/kan-41-cadastrar-fonte

# 5. Abra o Pull Request para a main, mesmo com o trabalho pela metade

# 6. Depois do merge, apague a branch
git checkout main && git pull
git branch -d feat/kan-41-cadastrar-fonte
```

Não trabalhamos com `develop` nem `release`. Só `main` e as branches de tarefa.
Com nove pessoas e prazo curto, camada a mais só gera conflito.

## Nome da branch

```
<tipo>/kan-<número>-<descrição-curta>
```

Tudo em minúsculo, palavras separadas por hífen, sem acento.

| Exemplo | Quando |
| --- | --- |
| `feat/kan-41-cadastrar-fonte` | Funcionalidade nova |
| `fix/kan-36-documento-duplicado` | Correção de defeito |
| `chore/kan-64-migrations-producao` | Manutenção, configuração |
| `docs/kan-73-guia-instalacao` | Documentação |
| `test/kan-57-testes-login` | Testes |
| `ci/kan-63-valida-pull-request` | Pipeline |

Branch sem card no Jira é exceção. Nesse caso, omita o número: `docs/git-flow`.

## Mensagem de commit

Seguimos [Conventional Commits](https://www.conventionalcommits.org/pt-br/), que é o
padrão que o repositório já usa desde o início.

```
<tipo>: <o que mudou, em minúsculo e no imperativo>
```

```bash
git commit -m "feat: adiciona filtro por status na listagem de fontes"
git commit -m "fix: corrige duplicação de documento na recoleta"
git commit -m "test: cobre login com usuário inativo"
```

Os tipos são os mesmos da tabela de branches: `feat`, `fix`, `chore`, `docs`,
`test`, `ci`, `refactor`.

Não escreva "ajustes", "correções" ou "wip" — daqui a duas semanas ninguém
lembra o que era.

## Pull Request

**Abra cedo.** Não espere terminar. PR aberto pela metade deixa a liderança
acompanhar e evita descobrir problema grande no fim.
Se ainda não está pronto, marque como *Draft*.

**Título no mesmo padrão do commit,** com o card na frente:

```
KAN-41: feat: adiciona cadastro de fonte pela interface
```

**Um PR por card.** Se você mexeu em duas coisas sem relação, são dois PRs.

**Antes de pedir revisão,** rode na sua máquina:

```bash
dotnet build
dotnet test
```

**Merge é por squash.** O histórico da `main` fica com um commit por card.

## Regras da main

- Ninguém commita direto na `main`
- Todo código entra por Pull Request
- O PR precisa de aprovação antes do merge
- Assim que o CI estiver no ar (KAN-63), o build e os testes passam a ser
  obrigatórios para o merge

## Conflito na sua branch

Atualize com a `main` antes de pedir revisão:

```bash
git checkout main && git pull
git checkout sua-branch
git merge main
# resolva os conflitos, teste, e só então
git push
```

Se o conflito for grande, chame a pessoa que mexeu no mesmo arquivo. Resolver
sozinho conflito de código alheio costuma custar mais caro.

## Dúvidas

O critério de aceite do card no Jira responde a maioria das dúvidas de escopo.
Se não responder, chame a liderança para ajustar o card — não decida sozinho.
