# Tilim

**Tilim** é um gerador de página de gorjeta via Pix. Cada profissional que
vive de gorjeta (garçom, manicure, músico de rua, entregador, barbeiro...)
cria, em menos de um minuto, a própria página pública com um QR Code Pix
pronto para ser escaneado, impresso e deixado na mesa, ou mostrado na tela
do celular. O nome vem do som do "tilintar" — a moeda caindo na caixinha,
só que via Pix.

O cliente escaneia, escolhe (ou digita) o valor, e paga **direto na conta
do profissional**. O dinheiro nunca passa pelo Tilim — isso é uma decisão
de arquitetura, não uma limitação (veja "O que o Tilim propositalmente não
faz" mais abaixo).

## Como funciona, na prática

1. O profissional preenche um formulário simples: nome, cidade, tipo e valor
   da chave Pix, e alguns valores sugeridos de gorjeta (R$ 5, R$ 10, R$ 20...).
2. O Tilim gera uma página pública só dele, em `/w/<endereço-escolhido>`,
   com um QR Code Pix "copia e cola" pronto para qualquer valor, e QR Codes
   específicos para cada valor sugerido.
3. O cliente abre a página (ou escaneia o QR impresso), escolhe um valor com
   um toque e paga pelo próprio app do banco. Sem cadastro, sem app para
   baixar, sem intermediário.

Todo o payload do QR Code segue o padrão público EMV/BR Code do Banco
Central — a mesma tecnologia por trás de qualquer Pix estático. O algoritmo
foi validado campo a campo contra o exemplo oficial do manual do Banco
Central antes de qualquer linha de UI ser escrita (detalhes técnicos em
[`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md)).

## O que tem pronto

- **Página de cadastro** (`/`) — formulário responsivo que valida os campos,
  dá dicas de formato pra cada tipo de chave Pix e mostra o endereço da
  página em tempo real.
- **Página pública de gorjeta** (`/w/{endereço}`) — nome, mensagem de
  agradecimento, QR Code (com opção de ampliar em um modal ou imprimir),
  chips de valores sugeridos que atualizam o QR Code na hora, e botão de
  copiar o código Pix.
- **API REST** com três endpoints (criar perfil, ler perfil público, gerar
  QR Code em PNG), documentada em Swagger UI (`/swagger` em desenvolvimento).
- **Rate limiting** no único endpoint de escrita público, e **cache** nas
  respostas de leitura (que mudam raramente).

## O que o Tilim propositalmente *não* faz (ainda)

- **Não movimenta dinheiro.** O QR Code é só uma codificação de dados no
  padrão público do Banco Central — não uma transferência intermediada.
  Por isso o Tilim não precisa ser instituição de pagamento licenciada.
- **Não confirma se o pagamento aconteceu.** Sem integração com um parceiro
  de pagamento, o Tilim gera o QR Code mas não sabe se o cliente pagou.
- **Não tem login ainda.** Um profissional cria o perfil uma vez; editar ou
  desativar depois vai exigir autenticação (a ideia é um link mágico por
  e-mail, sem senha).
- **Não cobra assinatura.** Não há cobrança nenhuma neste MVP — a ideia é
  validar se alguém usa a ferramenta de graça antes de monetizar.

Esse roadmap completo (autenticação, split de pagamento, assinatura, LGPD,
edição de perfil) está detalhado em
[`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md), na seção final.

## Stack técnica

| Camada | Tecnologia |
|---|---|
| Backend | .NET 9 / ASP.NET Core Minimal APIs |
| Banco de dados | PostgreSQL (produção) ou SQLite (dev local, zero setup) via EF Core 9 |
| QR Code | [QRCoder](https://github.com/codebude/QRCoder) |
| Documentação da API | Swagger UI sobre OpenAPI nativo do .NET (`Microsoft.AspNetCore.OpenApi`) |
| Frontend | HTML, CSS e JavaScript puros — sem framework, sem build step |
| Testes | xUnit (unitários, integração e um teste de integração HTTP ponta a ponta) |
| Deploy | Dockerfile multi-stage, pronto para Railway, Fly.io ou qualquer PaaS |

### Arquitetura em camadas

```
Tilim.Domain            Worker, Slug, PixKeyType, DomainException
      ↑
Tilim.Application       WorkerService (casos de uso) + interfaces
      ↑
Tilim.Infrastructure    EF Core + PostgreSQL/SQLite, geração do payload Pix, QR Code
      ↑
Tilim.Api               Minimal API, páginas estáticas, cache, rate limiting
```

Um monólito modular, não microsserviços: cada camada só conhece a de baixo,
o que mantém o domínio testável sem banco de dados e deixa claro onde
cortar se um dia for preciso separar em serviços. O porquê de cada decisão
de arquitetura (por que não CQRS/MediatR, como isso escala sem reescrever
nada, de onde vem cada campo do payload Pix) está em
[`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md).

## Rodando localmente

Pré-requisitos: .NET 9 SDK. Docker é opcional — só necessário se você quiser
rodar contra PostgreSQL em vez do SQLite de desenvolvimento.

```bash
# Clone e restaure os pacotes
git clone https://github.com/<seu-usuario>/tilim.git
cd tilim
dotnet restore

# Rode a API (usa SQLite automaticamente em ambiente de Desenvolvimento —
# ver appsettings.Development.json — sem precisar subir banco nenhum)
dotnet run --project src/Tilim.Api
```

Com a API rodando, abra no navegador:

- **`http://localhost:5278/`** — a página de cadastro (vitrine do usuário final).
- **`http://localhost:5278/swagger`** — Swagger UI, pra testar os endpoints
  da API diretamente.
- **`http://localhost:5278/w/<endereço>`** — a página pública de um
  profissional já cadastrado.

(Ajuste a porta conforme o perfil de `launchSettings.json`, ou abra o
projeto no Visual Studio e rode com F5/Ctrl+F5.)

### Rodando contra PostgreSQL

```bash
docker compose up -d
dotnet tool install --global dotnet-ef   # se ainda não tiver
dotnet ef migrations add InitialCreate -p src/Tilim.Infrastructure -s src/Tilim.Api
dotnet ef database update -p src/Tilim.Infrastructure -s src/Tilim.Api
```

E troque `"Database":{"Provider":"Sqlite"}` por `"Postgres"` (ou remova a
chave) na configuração do ambiente que for usar.

## Rodando os testes

```bash
dotnet test
```

- `Tilim.Domain.Tests` — regras de negócio do `Worker` e do `Slug`.
- `Tilim.Application.Tests` — casos de uso do `WorkerService`, com um
  repositório fake em memória.
- `Tilim.Infrastructure.Tests` — o ponto mais crítico do projeto: o
  construtor do payload Pix contra o exemplo oficial do Banco Central, e o
  mapeamento do EF Core contra SQLite em memória.
- `Tilim.Api.Tests` — testes de integração HTTP real, usando
  `WebApplicationFactory`.

## Deploy

O `Dockerfile` multi-stage publica a API como um único binário, sem SDK nem
código-fonte na imagem final — pronta para Railway, Fly.io ou qualquer PaaS
que aceite um Dockerfile.

```bash
docker build -t tilim-api .
docker run -p 8080:8080 -e ConnectionStrings__Default="<sua-connection-string>" tilim-api
```

## Licença e avisos

Projeto de estudo. Antes de operar com usuários reais e dinheiro de
terceiros (mesmo sem o Tilim tocar nesse dinheiro), revise com um contador
e, se for cobrar assinatura de profissionais autônomos, confirme as regras
de emissão de recibo/nota para o seu próprio negócio.
