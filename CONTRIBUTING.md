# Como contribuir — Squad 79

Guia rápido do fluxo de trabalho no repositório. Leia antes do primeiro commit.

## As duas branches fixas

| Branch | Para que serve |
| --- | --- |
| `develop` | Onde o trabalho do dia a dia é integrado. **É daqui que você tira a sua branch e é para cá que o seu Pull Request vai.** |
| `main` | Só recebe o que já foi aprovado e está estável. É o que a gente demonstra. |

Nenhuma das duas recebe commit direto. Tudo entra por Pull Request.

A diferença está em quem aprova: **na `develop`, você mesmo aprova e faz o merge
do seu PR** — não precisa esperar ninguém. Na `main`, quem aprova é a liderança.

## O fluxo, em 6 passos

```bash
# 1. Atualize a develop
git checkout develop
git pull

# 2. Crie a branch a partir dela
git checkout -b feat/kan-41-cadastrar-fonte

# 3. Trabalhe e vá commitando
git add .
git commit -m "feat: adiciona formulário de cadastro de fonte"

# 4. Suba a branch
git push -u origin feat/kan-41-cadastrar-fonte

# 5. Abra o Pull Request para a develop, mesmo com o trabalho pela metade

# 6. Depois do merge, apague a branch
git checkout develop && git pull
git branch -d feat/kan-41-cadastrar-fonte
```

O erro mais comum é abrir o Pull Request para a `main` por distração. O GitHub
sugere a branch padrão, então confira o destino antes de criar.

## Da develop para a main

Quem promove é a liderança, não quem fez o card. O caminho é:

1. A `develop` está estável, com build e testes passando
2. A liderança abre um Pull Request de `develop` para `main`
3. **A aprovação é da liderança.** Ninguém mais leva código para a `main`
4. Depois de aprovado, entra na `main`

Esse passo acontece quando há um conjunto pronto para mostrar — não a cada card.

> Enquanto não existe ambiente de homologação (KAN-65), a `main` faz esse papel:
> é a versão que a gente considera apresentável. Quando o ambiente existir, este
> trecho é revisto.

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

**O destino é a `develop`.** Sempre.

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

**O CI valida todo PR.** Ao abrir ou atualizar um PR para a `develop` ou a
`main`, o GitHub Actions restaura as dependências, compila em Release e roda os
testes unitários. Teste falhando ou warning de compilação reprova o check
*Build e testes*, e o merge fica bloqueado até ele passar. Para reproduzir na
sua máquina exatamente o que o CI roda:

```bash
dotnet restore InvoiSys.sln
dotnet build InvoiSys.sln --configuration Release --no-restore -p:TreatWarningsAsErrors=true
dotnet test tests/InvoiSys.UnitTests/InvoiSys.UnitTests.csproj --configuration Release --no-build
```

**Você mesmo aprova e faz o merge.** Não espere revisor. Abrir o PR serve para
registrar o que entrou, deixar o CI validar e permitir que a liderança acompanhe
— não para te travar.

Isso aumenta a sua responsabilidade: como ninguém vai revisar antes, **rode o
build e os testes de verdade** e confira os critérios de aceite do card.

**Merge é por squash.** O histórico da `develop` fica com um commit por card.

## Regras das branches

- Ninguém commita direto na `develop` nem na `main`
- Todo código entra por Pull Request
- Na `develop`, o próprio autor aprova e faz o merge
- Na `main`, só a liderança aprova
- O merge só é liberado com o check *Build e testes* do CI aprovado

## Conflito

Conflito não é erro nem sinal de que alguém fez besteira. Acontece quando duas
pessoas mexem no mesmo trecho do mesmo arquivo. Com nove pessoas na mesma base,
é rotina.

### Como evitar

**Atualize sua branch com a `develop` todo dia.** Conflito pequeno resolvido hoje
custa minutos; o mesmo conflito daqui a uma semana custa horas.

```bash
git checkout develop && git pull
git checkout sua-branch
git merge develop
```

**Mantenha a branch curta.** Card que leva cinco dias acumula cinco dias de
diferença. Se o card for grande, abra o PR no meio do caminho.

### Como resolver

Quando o `git merge develop` acusa conflito, o git marca o trecho assim:

```
<<<<<<< HEAD
o que está na sua branch
=======
o que veio da develop
>>>>>>> develop
```

Você escolhe o que fica: um dos dois lados, ou uma combinação dos dois. Depois
apaga as três linhas de marcação — `<<<<<<<`, `=======` e `>>>>>>>`. Elas não
podem sobrar no arquivo.

No VS Code aparecem os botões *Accept Current*, *Accept Incoming* e
*Accept Both*. Eles ajudam, mas leia o resultado antes de aceitar: às vezes o
certo é juntar os dois lados na mão.

Depois de resolver:

```bash
git add .
git commit
dotnet build && dotnet test
git push
```

**Só faça o push depois de compilar e testar.** Conflito mal resolvido compila
errado ou, pior, compila certo e quebra em execução.

### Se der errado no meio

Dá para desfazer e voltar ao estado anterior ao merge:

```bash
git merge --abort
```

Nada se perde. Respire e tente de novo.

### Quando chamar alguém

Chame a pessoa que mexeu no mesmo arquivo quando:

- o conflito é em código que você não escreveu
- são muitos arquivos de uma vez
- você não entende o que o outro lado estava tentando fazer

Para descobrir quem foi:

```bash
git log --oneline -5 develop -- caminho/do/arquivo
```

Resolver sozinho conflito de código alheio costuma custar mais caro que a
conversa de dois minutos.

## Dúvidas

O critério de aceite do card no Jira responde a maioria das dúvidas de escopo.
Se não responder, chame a liderança para ajustar o card — não decida sozinho.
