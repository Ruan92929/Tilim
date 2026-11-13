# Arquitetura do Tilim

## Camadas

```
Tilim.Domain            Worker, Slug, PixKeyType, DomainException
      ↑
Tilim.Application       WorkerService (casos de uso), interfaces
      ↑                 (IWorkerRepository, IPixPayloadBuilder, IQrCodeImageGenerator, IClock)
      ↑
Tilim.Infrastructure     EF Core + PostgreSQL, BrCodePayloadBuilder, QRCoder
      ↑
Tilim.Api                Minimal API, output caching, rate limiting, páginas estáticas
```

`Tilim.Api/wwwroot` guarda as duas páginas que o usuário final vê
(`index.html` — cadastro, `w.html` — página pública de gorjeta), em HTML,
CSS e JS puros, sem build step. Elas só conversam com a API pelos mesmos
três endpoints JSON documentados no Swagger — nenhum acoplamento extra.

A regra é sempre a mesma: uma camada só conhece a de baixo. `Tilim.Domain`
não sabe que EF Core existe. `Tilim.Application` não sabe que Postgres ou
QRCoder existem — só conhece as interfaces que `Tilim.Infrastructure`
implementa. Isso é o que torna o `WorkerServiceTests` possível sem banco de
dados nenhum: o repositório fake em `Tilim.Application.Tests/Fakes` é uma
implementação completa e real da mesma interface que o EF Core implementa
em produção.

## Por que monólito modular, e não microsserviços

Este projeto é exatamente o tamanho que o estudo original do RotinaPet já
argumentava contra dividir em serviços: um volume inicial pequeno não
justifica a complexidade operacional (deploys separados, observabilidade
distribuída, latência de rede entre serviços) que microsserviços trazem. As
fronteiras de camada (Domain/Application/Infrastructure/Api) já dão a
separação de responsabilidades que mais importa agora. Se um dia for
preciso separar em serviços de verdade (por exemplo, se a geração de QR
Code virar um gargalo isolado), as interfaces já isolam exatamente onde
cortar.

## Como isso escala sem reescrever nada

- **API sem estado.** Nenhum dado de sessão fica em memória do processo —
  cada requisição é independente. Isso significa que subir 1, 5 ou 50
  cópias do mesmo container atrás de um load balancer funciona sem
  nenhuma mudança de código; é só configuração de infraestrutura.
- **Leitura pesada, escrita leve.** A página pública de um profissional
  (`GET /api/w/{slug}`) e o QR Code (`GET /api/w/{slug}/qrcode.png`) só
  mudam quando ele edita o perfil — na prática, quase nunca. `Program.cs`
  usa `AddOutputCache` para cachear essas respostas por um tempo curto (30s
  para o perfil, 10min para o QR Code, que é ainda mais estável). Isso tira
  carga do banco de dados conforme o tráfego cresce, sem precisar de um
  cache distribuído (Redis) neste estágio — um `OutputCache` em memória por
  instância já resolve, porque o conteúdo é idêntico em qualquer instância.
- **O cálculo do payload Pix e a geração do QR Code são puros.** Dados os
  mesmos dados de entrada, sempre produzem a mesma saída, não dependem de
  estado compartilhado e não fazem chamada de rede nenhuma. Isso os torna
  trivialmente paralelizáveis e fáceis de testar.
- **Rate limiting no único endpoint de escrita exposto sem autenticação**
  (`POST /api/workers`), para que criar perfis não seja um vetor de abuso
  antes de existir qualquer verificação de identidade.
- **Dockerfile multi-stage** — a imagem final não carrega o SDK, o código-
  fonte nem o cache do NuGet, só o binário publicado. Isso deixa o
  container pequeno e rápido de subir, que é o que importa quando se está
  escalando horizontalmente (mais réplicas, não réplicas maiores).

## Por que não CQRS/MediatR aqui

Um mediator (MediatR e similares) ajuda quando há muitos casos de uso
cruzando-se e pipelines de comportamento (logging, validação, transação)
repetidos. Com um único agregado (`Worker`) e três casos de uso, isso seria
complexidade paga antes de ser necessária — e MediatR especificamente
passou a exigir licença comercial em versões recentes, o que é um motivo
extra para não trazê-lo para um projeto que pode vir a ser monetizado. Se
`WorkerService` crescer e ficar difícil de navegar, o próximo passo é
dividir em uma classe por caso de uso (`CreateWorker`, `GetPublicProfile`,
...), não adicionar um framework.

## O payload Pix (BR Code) — de onde vem cada campo

A referência é a seção 2.6 do "Manual de Padrões para Iniciação do Pix" do
Banco Central:
<https://www.bcb.gov.br/content/estabilidadefinanceira/pix/Regulamento_Pix/II_ManualdePadroesparaIniciacaodoPix.pdf>

| Campo | Significado | Observação |
|---|---|---|
| `00` | Payload Format Indicator | Fixo `01` |
| `26` | Merchant Account Information | Contém `26-00` (GUI, fixo `br.gov.bcb.pix`) e `26-01` (a chave Pix) |
| `52` | Merchant Category Code | Fixo `0000` (não informado) |
| `53` | Transaction Currency | Fixo `986` (real) |
| `54` | Transaction Amount | Omitido quando o valor é livre (o app do pagador pergunta) |
| `58` | Country Code | Fixo `BR` |
| `59` | Merchant Name | Até 25 caracteres, maiúsculas, sem acento |
| `60` | Merchant City | Até 15 caracteres, maiúsculas, sem acento |
| `62` | Additional Data Field | Contém `62-05` (txid, `***` quando não há) |
| `63` | CRC16 | Calculado sobre tudo acima, incluindo o próprio `6304` |

Os limites de 25/15 caracteres e o charset maiúsculo-sem-acento para os
campos 59/60 não estão explícitos no trecho do manual que consultei — são a
convenção que todo PSP de Pix segue na prática (o manual do BR Code, que o
manual de Pix referencia, é quem define isso em detalhe). `PixTextSanitizer`
aplica essa normalização.

**Esse código foi verificado, não só escrito.** O exemplo oficial do manual
usa a chave `123e4567-e12b-12d1-a456-426655440000`, nome "Fulano de Tal",
cidade "BRASILIA", sem valor, documentando CRC `1D3D`. Rodei o CRC16
isoladamente (sem depender de nenhum pacote NuGet) contra esse payload
exato e o resultado bateu `1D3D` — é o mesmo teste que está em
`Crc16CcittTests` e, de ponta a ponta pelo `BrCodePayloadBuilder`, em
`BrCodePayloadBuilderTests.Build_KnownBacenExample_MatchesDocumentedPayloadAndCrc`.

## Por que não cobramos uma taxa por gorjeta (ainda)

O QR Code Pix que o Tilim gera aponta direto para a chave Pix do próprio
profissional — o dinheiro do cliente vai para a conta dele, sem passar pelo
Tilim. Isso é uma escolha deliberada: gerar um BR Code é usar um padrão
público, não movimentar dinheiro de terceiros, então o Tilim não precisa
ser uma instituição de pagamento licenciada pelo Banco Central para operar
desse jeito.

A alternativa — cobrar uma fração de cada gorjeta — exigiria intermediar o
Pix de fato (receber na conta do Tilim, repassar o líquido ao profissional),
o que é "split de pagamento" e exige um parceiro de pagamento já licenciado
(Mercado Pago, Asaas, Pagar.me...) ou licença própria. Isso é trabalho real
de integração e burocracia regulatória — fica para uma fase 2, só depois de
confirmar que profissionais pagam pela ferramenta em si.

## O que falta para "produção de verdade" (roadmap, não construído ainda)

- **Autenticação do profissional.** Hoje `POST /api/workers` cria um perfil
  mas não há como editá-lo depois — não existe conceito de "dono" do
  perfil. O caminho de menor fricção, dado o público (nem todo garçom/
  manicure quer lidar com senha), é login por link mágico enviado por
  e-mail: sem senha, sem cadastro de conta separado do perfil de gorjeta.
- **Confirmação real de pagamento**, via split com um parceiro de pagamento
  licenciado — só depois que o modelo de assinatura estiver validado.
- **Cobrança da assinatura** do profissional pela ferramenta (não da
  gorjeta em si).
- **Página de edição do perfil** (trocar valores sugeridos, mensagem de
  agradecimento, pausar a página temporariamente — já existe
  `Worker.Deactivate`/`Activate` no domínio, só falta expor via API).
- **LGPD**: mesmo sem mexer em dinheiro, o Tilim guarda dado pessoal (nome,
  chave Pix — que pode ser CPF, e-mail ou telefone — e cidade). Antes de
  operar com usuários reais, isso precisa de política de privacidade,
  finalidade clara e, no mínimo, HTTPS obrigatório em produção (o
  Dockerfile não configura TLS — isso é responsabilidade do PaaS/load
  balancer na frente, ex: Railway e Fly.io terminam TLS automaticamente).
