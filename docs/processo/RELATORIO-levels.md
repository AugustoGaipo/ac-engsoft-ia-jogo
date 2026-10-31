# Relatório do módulo `levels`

> Módulo concluído em 2026-10-31. Spec: [`docs/specs/SPEC-levels.md`](../specs/SPEC-levels.md) ·
> Plano: [`tasks/levels/plan.md`](../../tasks/levels/plan.md) · Tarefas: [`tasks/levels/todo.md`](../../tasks/levels/todo.md) ·
> Diário: [`DIARIO.md`](DIARIO.md).

## Resumo
- **O que é:**
  - a biblioteca `Swapping.Levels` (.NET 8), que lê, valida e converte para o `rules` os arquivos de nível;
  - os **4 níveis de sexta** (1, 2, 3 e 5, com 11 salas) em `levels/`, mais o `catalog.json`.
- **Como foi feito:**
  - no PC, numa sessão só, depois da rotina na nuvem ter feito o `rules`;
  - com as skills `spec-driven-development` (spec aprovada antes do código), `planning-and-task-breakdown`
    (8 tarefas) e `test-driven-development` (teste antes do código, também para o conteúdo das salas).

| Medida | Valor |
|---|---|
| Testes do `levels` | **125 aprovados de 125** (xUnit), em 9 arquivos |
| Testes do `rules` | 94 de 94 (continuam passando) |
| Cobertura de linhas do `Swapping.Levels` | **96,7 %** (ramos 91,9 %); meta da spec ≥ 90 % |
| Build | 0 avisos (`TreatWarningsAsErrors`) |
| Critérios de sucesso | **14 de 14** cobertos |
| Conteúdo | 4 níveis, 11 salas, 21 códigos de erro no validador |

A cobertura foi medida **numa rodada em que todos os testes passaram**. Essa foi a lição do `rules`.

## O que foi feito em cada tarefa
| # | Tarefa | Principais peças | Testes ao fim |
|---|---|---|---|
| 1 | Projetos e modelo | `Box`, `ObjectDef` (volume, contato e densidade calculados), `ButtonDef`, `DoorDef`, `BarrierDef`, `KillZoneDef`, `SpawnDef`, `LevelFile`, `LevelError` | 12 |
| 2 | Leitor do JSON | `LevelParser` (formato → `InvalidJson` com caminho; nunca lança exceção) | 27 |
| 3 | Validador: valores | 11 códigos, um campo de cada vez | 63 |
| 4 | Validador: espaço e ligações | 8 códigos que comparam peças entre si | 88 |
| 5 | Ligação com o `rules` | `RulesMapping.ToLevelRules`, `BuildWorld` (montar de novo = reiniciar a sala) | 96 |
| 6 | Catálogo e níveis 1 e 2 | `LevelCatalog`, `level_1.json`, `level_2.json`, testes de design por sala | 118 |
| 7 | Níveis 3 e 5 | `level_3.json`, `level_5.json`, catálogo com os 4 níveis | 122 |
| 8 | Arquitetura, cobertura e docs | `ArchitectureTests`, README | 125 |

## Critérios de sucesso → testes
| # | Critério | Testes que cobrem |
|---|---|---|
| 1 | Build e testes sem avisos | `TreatWarningsAsErrors` (o build falha com qualquer aviso) |
| 2 | O exemplo da spec é lido e é válido | `ParserTests.SpecExample_ParsesIntoTheExpectedValues`, `ValidatorValueTests.SpecExample_IsValid` |
| 3 | Cada código de erro tem um teste que o produz (e só ele) | `ValidatorValueTests`, `ValidatorSpatialTests`, `ParserTests`, `CatalogTests`; o teste `EveryErrorCode_IsProducedByAtLeastOneTest` garante que nenhum dos 21 ficou sem teste |
| 4 | JSON quebrado → `InvalidJson`, sem exceção | `ParserTests.MalformedJson_IsAnErrorNotAnException`, `MissingRequiredField_…`, `VectorWithTwoNumbers_…` |
| 5 | Salas fora de ordem → `BadId`; id repetido → `DuplicateId` | `ValidatorValueTests.BadId_RoomIdsOutOfOrder`, `DuplicateId_AcrossRooms` |
| 6 | Densidade 300 vale; 0,001 kg e 30 000 kg não | `ValidatorValueTests.ImplausibleDensity_OutsideOneTo22600` |
| 7 | Saída sem grade → `MissingExitGrid` | `ValidatorSpatialTests.MissingExitGrid_*`, `ExitGrid_MadeOfExplicitFlags1Plus3_Counts` |
| 8 | Sala sem propriedade liberada → toda troca é `PropertyNotUnlocked` | `MappingTests.EmptyUnlockedOverride_RejectsEverySwapWithPropertyNotUnlocked` |
| 9 | Sala 3.1: troca de massa com a caixa → jogador com 300 kg; invariante vale | `MappingTests.Room31_PlayerCrateMassSwap_IsAppliedAndThePlayerWeighs300` |
| 10 | `BuildWorld` duas vezes → mundos independentes | `MappingTests.BuildWorld_TwiceGivesIndependentWorlds` |
| 11 | Os 4 arquivos e o catálogo são válidos (11 salas) | `ShippedLevelsTests.CatalogAndEveryLevelFile_AreValid`, `Catalog_HasTheFridayLevels_With11Rooms` |
| 12 | Os 11 testes de design das salas passam | `RoomDesignTests` (um por sala) |
| 13 | O `.csproj` só referencia o `Swapping.Rules` | `ArchitectureTests.LevelsProject_ReferencesOnlyTheRulesProject`, `LevelsAssembly_ReferencesNoGodotAssembly` |
| 14 | Cobertura de linhas ≥ 90 % | medição com `--collect:"XPlat Code Coverage"`: 96,7 % |

**Nenhum critério ficou descoberto.**

## Decisões tomadas sem o usuário
Estão no diário, uma seção por tarefa. As que mais importam:

**Formato do arquivo**
- **Pontos de referência:** a posição do objeto é o centro dele; a do botão é o centro da face de baixo;
  o ponto de renascimento é o pé do jogador.
- **Campos opcionais** com valores padrão (listas vazias, `accepts` nulo etc.). Os arquivos aceitam
  comentários `//` e vírgula sobrando no fim.

**Validador**
- O id `player` é reservado.
- Uma lista `accepts` vazia é erro.
- Massa ou tamanho inválido não gera também o erro de densidade.
- Recarga 0 s é permitida.
- **"Saída coberta pela grade"** = uma barreira que é **exatamente** a grade de saída e encosta por
  dentro na saída. Uma barreira que também barra o jogador não serve.
- Salas que só se encostam são válidas. Objeto encostado num botão não é sobreposição.
- O ponto de renascimento dentro de uma zona que barra variáveis é permitido.

**Ligação com o `rules` e segurança**
- **Posição no `World`:** é o **centro** do corpo (jogador: pés + 0,9 m). Área de contato do jogador: 0,36 m².
- **Segurança do catálogo:** o arquivo tem que ser um nome simples dentro de `levels/` (sem `../`,
  subpasta ou unidade).

**Conteúdo das salas (rascunho, para ajustar jogando)**
- **Coordenadas:** salas lado a lado ao longo de +X, chão em y = 0, buracos até y = -3.
- **Sala 1.3:** uma bola em cima da plataforma é o jeito de subir.
- **Sala 1.4:** a caixa que recusa a posição é **travada** (ver "Pendências").
- **Sala 5.1:** bola de 20 kg a 8 m/s.
- **Sala 5.2:** a caixa fica num corredor fechado.

## Problemas encontrados
1. **Um teste quebrou só quando a cobertura foi medida** (de novo, como no `rules`, mas por outro motivo):
   - `LevelsAssembly_HasNoStaticMutableFields` acusava os campos que o **compilador do C#** cria para guardar
     as lambdas (tipos `<>c`).
   - Corrigido para ignorar o código gerado pelo compilador. O mesmo filtro foi para o teste do `rules`, que
     quebraria na primeira lambda nova.
   - Foi achado porque o plano exigia medir a cobertura **com todos os testes passando na mesma rodada**.
2. **Na Tarefa 5, a fase vermelha não foi rodada:** os testes foram escritos antes do código, mas não foram
   executados antes. Está registrado no diário.

## Pendências
- **Para você confirmar:** na sala 1.4, a caixa "que não aceita posição" é uma caixa travada, porque o
  nível 1 só libera a posição. O contorno colorido por propriedade só faz sentido de verdade a partir do
  nível 3, quando há mais de uma propriedade.
- **Para a spec do `game`:**
  - a troca de posição troca **centros**: o jogador que vai para o lugar de uma caixa fica com os pés
    dentro do chão, e o `game` precisa subir o corpo para o ponto livre mais próximo;
  - a espessura das paredes e a detecção contínua de colisão (o validador não vê o cenário);
  - a morte por queda alta (sem buraco).
- **Ajustar jogando:** os números das salas (posições, massas, larguras dos buracos).
- **Só a cena do Godot resolve:** se cada sala dá para resolver (o validador confere as peças, não a solução).
