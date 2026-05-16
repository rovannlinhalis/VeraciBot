# VeraciBot v2

VeraciBot e uma aplicacao web em ASP.NET Core + Blazor Server para monitoramento de mencoes no X/Twitter, execucao de comandos por usuarios autorizados, verificacao de noticias com apoio de LLM e acompanhamento de pontuacao da comunidade.

## Visao Geral

O projeto combina:

- Painel web administrativo e de usuario
- Integracao com X/Twitter (OAuth + leitura de mencoes + respostas)
- Regras de autorizacao por papeis
- Processamento em background de mencoes
- Configuracao dinamica por parametros (sem recompilar)
- Identidade ASP.NET com fluxos de login/registro
- Envio de e-mails via SMTP configuravel

## Stack Tecnica

- .NET 10 (`net10.0`)
- ASP.NET Core + Blazor Server (Interactive Server Components)
- Entity Framework Core (SQL Server)
- ASP.NET Core Identity
- Tweetinvi API
- Microsoft Agents AI OpenAI

## Estrutura Principal

- `VeraciBot.App/`: aplicacao web principal
- `VeraciBot.App/Components/`: paginas e componentes Blazor
- `VeraciBot.App/Services/`: servicos de dominio e workers
- `VeraciBot.App/Entities/`: entidades e parametros dinamicos
- `VeraciBot.App/Data/`: DbContext e mapeamentos EF Core
- `VeraciBot.App/External/`: integracoes externas (Twitter)
- `VeraciBot.App/Migrations/`: historico de migracoes

## Funcionalidades

### 1) Landing page publica

- Home publica para usuarios deslogados
- Conteudo e imagens customizaveis via Settings (grupo `Landing`)
- CTAs configuraveis para login/cadastro

### 2) Autenticacao e autorizacao

- Login local (pode ser desabilitado por parametro)
- Suporte a provedores externos (Twitter OAuth configuravel)
- Controle por papeis com politicas como `ApplicationRole:Admin`

### 3) Painel administrativo

- Monitoramento do worker de mencoes
- Dashboard de acompanhamento
- Gestao de usuarios
- Parametros de sistema em tempo real (Settings)

### 4) Processamento de mencoes

- Polling de mencoes no X/Twitter em background
- Fila em memoria com limites configuraveis
- Processamento de comandos do bot
- Registro de historico e rastreabilidade da execucao

### 5) Recursos de comunidade

- Convites para novos usuarios
- Aceite/recusa de convite
- Pontuacao por interacoes e avaliacoes
- Ranking e painel individual

### 6) Verificacao de noticias

- Comandos para avaliacao de alegacoes
- Consulta a fontes e apoio de LLM
- Resultado e trilha de processamento no painel

### 7) Envio de e-mail por SMTP

- Implementacao real de `IEmailSender<ApplicationUser>`
- Usado por fluxos de confirmacao de e-mail e recuperacao de senha
- Configuracao via novo grupo `SMTP` nos Settings

## Requisitos

- SDK .NET 10 instalado
- SQL Server (ou LocalDB no Windows)
- Acesso de rede para APIs externas (OpenAI/X, se habilitadas)

## Instalacao Rapida

1. Clonar o repositorio:

```bash
git clone https://github.com/ralbuque/VeraciBot.git
cd VeraciBot
```

2. Restaurar dependencias:

```bash
dotnet restore VeraciBot.App/VeraciBot.App.csproj
```

3. Configurar ambiente:

- Copie `VeraciBot.App/secrets.example.json` para um arquivo local de configuracao (ex.: `appsettings.Development.json`) ou use User Secrets.
- Preencha no minimo:
  - `ConnectionStrings:DefaultConnection`
  - `Encryption:Key`

4. Aplicar migracoes:

```bash
dotnet ef database update --project VeraciBot.App/VeraciBot.App.csproj
```

5. Executar:

```bash
dotnet run --project VeraciBot.App/VeraciBot.App.csproj
```

6. Acessar no navegador:

- URL local exibida no terminal (ex.: `https://localhost:7xxx`)

## Configuracao

### Configuracao base (appsettings)

Arquivo base: `VeraciBot.App/appsettings.json`

Chaves importantes:

- `ConnectionStrings:DefaultConnection`
- `Encryption:Key`
- `Authentication:Twitter:ClientId`
- `Authentication:Twitter:ClientSecret`
- `TwitterApi:UserId`
- `BlobStorage:LocalPath`
- `BlobStorage:PublicPath`

Exemplo completo: `VeraciBot.App/secrets.example.json`

### Configuracao dinamica via Settings

No painel admin, voce pode ajustar parametros sem recompilar. Principais grupos:

- `Agent`
- `Twitter`
- `OpenAI`
- `Landing`
- `SMTP`

### Grupo SMTP

Parametros disponiveis:

- `SMTP_ENABLED` (0/1)
- `SMTP_HOST`
- `SMTP_PORT` (ex.: 587 ou 465)
- `SMTP_ENABLE_SSL` (0/1)
- `SMTP_USERNAME`
- `SMTP_PASSWORD`
- `SMTP_FROM_EMAIL`
- `SMTP_FROM_NAME`

Fluxo recomendado para habilitar envio real:

1. Definir `SMTP_ENABLED = 1`
2. Preencher host, porta e remetente
3. Preencher credenciais de autenticacao
4. Testar registro/recuperacao para validar envio

## Conta inicial (bootstrap)

No startup, o sistema garante uma conta admin padrao para inicializacao:

- Usuario: `admin@admin.com`
- Senha: `Senha123456`

Recomendacao:

1. Alterar senha imediatamente apos primeiro acesso.
2. Restringir acesso externo durante bootstrap.

## Comandos uteis

Build:

```bash
dotnet build VeraciBot.App/VeraciBot.App.csproj
```

Atualizar banco:

```bash
dotnet ef database update --project VeraciBot.App/VeraciBot.App.csproj
```

Criar nova migracao:

```bash
dotnet ef migrations add NomeDaMigracao --project VeraciBot.App/VeraciBot.App.csproj
```

## Solucao de Problemas

- Nao abre login / redireciona incorretamente:
  - confirme que a aplicacao esta com as ultimas mudancas de rotas de login (`/Account/Login`).
- Erros de banco/migracao:
  - valide string de conexao e execute `dotnet ef database update`.
- E-mail nao enviado:
  - valide grupo `SMTP` (host, porta, SSL, usuario/senha, remetente) e `SMTP_ENABLED=1`.
- Worker sem processar mencoes:
  - confira configuracoes em `Twitter` e permissao da conta bot.

## Roadmap Sugerido

- Endpoint/tela de teste de SMTP no painel
- Observabilidade adicional (metricas por comando)
- Cobertura de testes automatizados

## Creditos

Projeto original de **ralbuque (Peter)**.

Esta edicao corresponde a um **fork com nova versao (v2)**, evoluida por **rovannlinhalis**.

- Projeto original: https://github.com/ralbuque/VeraciBot
- Fork / nova versao: https://github.com/rovannlinhalis

Se este projeto te ajudou, considere dar uma estrela no repositorio original e creditar tanto o autor original quanto as contribuicoes da nova versao.
