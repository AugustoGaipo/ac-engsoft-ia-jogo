# Swapping (nome provisório) — jogo de troca de propriedades

> One-pager gerado com a skill `agent-skills:idea-refine` (fase 3). Todas as ideias, inclusive as
> que ainda não entram no jogo, ficam em [`BANCO-DE-IDEIAS.md`](BANCO-DE-IDEIAS.md).

## Problem Statement
Como podemos fazer um puzzle 3D em que o jogador entende física "sentindo" a troca de propriedades
(posição, peso, energia) entre ele e os objetos, e em que cada troca tem um efeito colateral que vira
o próprio puzzle?

## Recommended Direction
Puzzle em primeira pessoa no **Godot 4 (C#)**. As regras de troca ficam numa **biblioteca em C# puro**,
fora da engine e coberta por testes automatizados. Tudo é troca entre dois "recipientes", e o jogador
é só mais um recipiente.

As regras **seguem as leis da física** e cada efeito pode ser explicado com uma conta (ex.: ½·m·v²).
Os números aparecem na tela para o jogador entender a conta.

O jogo **roda offline**: o progresso fica salvo no PC e é enviado para a API quando há internet.
A API (ASP.NET Core + SQLite) cuida do login, do progresso na nuvem e do ranking por fase.
Passar de fase é chegar à saída; não há validação antitrapaça, porque é um jogo para um jogador.

A arquitetura deixa **pontos de extensão** prontos para as ideias futuras (ver abaixo), para que
elas entrem sem gambiarra.

## Regras de física decididas
- **Peso:** o volume continua o mesmo; muda a massa e, portanto, a densidade.
- **Peso com o objeto em movimento:** a **energia cinética se mantém** e a velocidade se ajusta.
  Ex.: uma caixa de 2 kg a 3 m/s tem E = ½·2·3² = 9 J. Se ela fica com 8 kg, a energia continua 9 J
  e a velocidade cai para √(2·9/8) = 1,5 m/s.
- **Energia cinética:** quem recebe a energia sai na **direção em que o outro se movia** (no mundo,
  não em relação à câmera). Ex.: uma bola vai para a direita da sala; se eu troco de costas para a
  direita, sou jogado para trás. Se o outro estava parado, quem recebe para.

## Key Assumptions to Validate
- [ ] O jogador entende a troca sem tutorial → uma pessoa joga a fase 1 sem explicação.
- [ ] Peso e energia do jogador simulados "parecem certos" (o personagem do Godot não é um corpo
      físico comum) → protótipo no dia 2, antes das fases.
- [ ] A diferença de energia fica clara com números na tela → teste da fase 5.
- [ ] Ser lançado "para trás" pela direção do objeto não desorienta demais → teste da fase 5.
- [ ] 10 fases cabem na semana se montadas a partir de JSON e peças reutilizáveis → medir o tempo
      para montar a fase 2; se passar de ~1 h, cortar.
- [ ] Botões que só detectam objetos (ou exigem mais peso que o jogador) evitam atalhos → fases 3–4.

## MVP Scope
Dentro:
- Troca **jogador ↔ objeto** com posição, peso e energia cinética.
- **Trocas permanentes** com recarga entre quaisquer trocas (modelo C começando pelo A).
- 10 fases: 1–2 posição, 3–4 peso, 5–6 energia, 7 pos+peso, 8 pos+energia, 9 peso+energia, 10 as três.
- Números na tela; objetos travados e propriedades permitidas indicadas por cor.
- Save local + envio para a API quando houver internet.
- Biblioteca de regras com testes; fases em JSON; API com login, progresso e ranking.
- C4 de Contexto e Container antes do código; Componentes e Código no final.

Corte se apertar: fases 1, 3, 5 e 10 garantidas.

## Pontos de extensão na arquitetura (sem implementar agora)
| Ponto de extensão | Ideias que ele permite |
|---|---|
| Propriedades como peças separadas | momento, temperatura, elasticidade, mais 2 habilidades numéricas |
| Modos de troca | objeto ↔ objeto, arma que guarda valores |
| Duração da troca | permanente (agora), temporária / objetos instáveis |
| Modificadores da recarga | power-up que zera a recarga, "peso glorioso", downgrade de tempo limitado |
| Combinação de trocas | duas com intervalo (agora), simultâneas (fase 11) |
| Tipos de operação: trocar (A ↔ B) e transferir (A → reserva → B) | tempo (roubar/entregar), arma que guarda valores |
| Escala de tempo por objeto | habilidade de tempo (mundo mais lento ou mais rápido) |
| Peças com limites físicos (quebradiça, água) | quebrar coisas pela densidade/peso, afundar e boiar |
| Fases em JSON com peças | novas fases e novos tipos de peça sem mexer no código das regras |

## Not Doing (and Why)
- **Momento/velocidade:** ainda não há um jeito claro de aplicar.
- **Objeto ↔ objeto e a arma:** são a evolução do jogo; só o ponto de extensão entra agora.
- **Troca temporária, power-ups, peso glorioso:** dependem de ver as fases básicas funcionando primeiro.
- **Troca simultânea:** vira a fase 11 extra, se sobrar tempo.
- **Validação antitrapaça:** jogo offline para um jogador; passar de fase é chegar à saída.
- **Steam/Steamworks:** só depois de provar que a mecânica é divertida.
- **Arte, som e iluminação caprichados:** o visual simples e limpo é escolha, não falta.

## Open Questions
- Duração da recarga nas fases 7–10 → decidir jogando, conforme a dificuldade de cada fase.
- Nome do jogo → "Swapping" provisório.
