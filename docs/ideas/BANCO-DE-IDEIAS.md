# Banco de ideias

Todas as ideias do jogo num lugar só, organizadas pelo estado de cada uma.
Quando uma ideia muda de estado, ela muda de seção (e o diário registra o porquê).

| Estado | Significado |
|---|---|
| ✅ **Utilizada** | Decidida e entra na versão atual (10 fases) |
| 🔨 **Em andamento** | Decidida em parte; falta definir detalhes |
| 🧩 **Possível (com espaço na arquitetura)** | Não entra agora, mas a arquitetura já deixa o encaixe pronto |
| 💭 **Ideia pensada** | Só levantada, ainda sem decisão nem encaixe |

---

## 🧭 Rota de prioridade

O que fazer primeiro e o que fica para depois. Uma faixa só começa quando a anterior está pronta
(exceto a documentação, que acompanha tudo).

### P0 — Essencial (sem isso não tem nota nem demo)
0. **C4 de Contexto e Container** — o enunciado exige antes do código.
1. **`rules`:** troca jogador ↔ objeto de posição, peso e energia; recarga; travas e cores; testes.
2. **`levels`:** formato JSON das fases + validador + testes.
3. **`game` mínimo:** personagem com peso e energia simulados, mira, troca, números na tela,
   grade de saída (barreira 1 + barreira 3),
   botão, porta e saída → **fase 1 jogável** (critério de corte do dia 3).
4. **Fases 1, 3, 5 e 10** (as garantidas do corte).
5. **`profiles`:** nome + ícone, código de acesso, hash, bloqueio por tentativas, limite de envios; testes.
6. **`progress`:** fases liberadas em ordem, melhor resultado, ranking nos dois modos; testes.
7. **`sync`:** save local + fila de envio quando houver internet; testes.
8. **Documentação final:** C4 de Componentes e Código, README de como rodar, diário, prints.

### P1 — Jogo completo planejado
- Fases 2, 4, 6, 7, 8 e 9.
- Fases contínuas (uma leva direto à próxima).
- Barreiras 1, 2 e 3 sozinhas como peças de puzzle, introduzidas uma por vez, em parede ou em zona.
- Tela de ranking e escolha de ícone dentro do jogo.
- Teste da fase 1 com uma pessoa sem explicação (premissa do tutorial).
- **Densidade:** 3 meios (ar, gás denso, água) para afundar, boiar e **flutuar no ar**; peças quebradiças e **pressão parada** (peso ÷ área de contato,
  verificada quando o objeto está parado em cima da peça; a área de contato é um número no JSON).
- **Site explicando a arquitetura** (só para mim: simples e bem compreensível, não precisa ser bonito).

### P2 — Se sobrar tempo
- Fase 11: troca simultânea.
- Fantasma das partidas dos primeiros do ranking.

### P3 — Futuro (a arquitetura já deixa o espaço)
- Objeto ↔ objeto e a arma que guarda valores.
- Tempo (roubar/entregar) e o tipo de operação "transferir".
- Objetos instáveis, power-up de recarga, peso glorioso, downgrade de tempo limitado.
- **Pressão de impacto:** objeto que cai sobre a peça quebradiça (estimar a pancada pela física).
- Momento e as outras propriedades novas.
- Steam.

---

## ✅ Utilizadas

### Mecânica
- **Troca de propriedades** como mecânica central: tudo é troca entre dois "recipientes".
- **Jogador ↔ objeto** como a troca básica (mais fácil de entender, limita atalhos).
- **Efeito colateral:** toda troca mexe em duas coisas; é o coração do puzzle.
- **Trocas permanentes** com recarga entre quaisquer trocas.
- **Números na tela** (ex.: 5 kg, 3 m/s, 9 J).
- **Regras seguem a física**, explicáveis por contas.

### Propriedades
- **Posição:** os dois trocam de lugar.
- **Peso:** volume constante, muda a densidade.
- **Energia cinética** (só cinética, ½·m·v²; a mecânica foi descartada): quem recebe sai na direção em que o outro se movia.
- **Peso com movimento:** a energia se mantém e a velocidade se ajusta.

### Design de fases
- 10 fases: 2 por habilidade sozinha, 1 por dupla, 1 com as três (ordem no one-pager).
- Fases seguidas com a mesma habilidade, como no Portal.
- Controles de dificuldade: quantos objetos, quantas propriedades, ordem das trocas, limite de trocas.
- Contra atalhos: objetos travados, objetos que só aceitam certas propriedades, só trocar com o que está à vista.
- **Cor no contorno, não no objeto:** o objeto que dá para segurar ou trocar (e o que estou segurando)
  ganha um contorno; a **cor do contorno** mostra quais propriedades ele aceita (ex.: azul = só peso).
  Objetos que aceitam tudo têm o contorno **branco** padrão.
- **Alcance máximo da troca:** cada habilidade tem uma distância máxima; longe demais, a troca não
  acontece (a mira avisa, ex.: muda de cor). O alcance pode ser diferente por habilidade e é configurado
  em cada fase, servindo como mais um controle de dificuldade.
- Botões que só detectam objetos ou exigem mais peso que o jogador.

### Tecnologia e plataforma
- Godot 4 com C#; biblioteca de regras em C# puro com testes.
- Fases montadas a partir de arquivos JSON com peças reutilizáveis.
- Jogo offline; progresso enviado para a API quando há internet.
- **Perfil sem senha:** o jogador escolhe nome e ícone; o servidor gera uma chave secreta guardada no PC
  (o servidor guarda só o hash). Nada de senha ou e-mail para vazar.
- **Código de acesso:** o servidor gera um código legível (ex.: K7QM-4XRT-9WPA-HC3N); o jogo mostra,
  guarda e pede para o jogador anotar. Entrar em outro PC = nome + código. O servidor guarda só o hash,
  bloqueia após tentativas erradas e permite gerar um código novo. Perdeu código e PC = perdeu o perfil.
- **Ranking com dois modos:** por tempo (desempate por trocas) ou por movimentos (desempate por tempo).
- API com perfil, progresso e ranking; sem validação antitrapaça.
- Visual simples e limpo (formas geométricas, cores sólidas).

---

## 🔨 Em andamento
- **Duração da recarga** nas fases 7–10: decidir conforme a dificuldade de cada fase.
- **Nome do jogo:** "Swapping" provisório.
- **Ordem exata das fases:** proposta no one-pager, ainda pode mudar.
- **Fases contínuas, como no Portal:** o jogo não para entre as fases; uma leva direto à próxima
  (corredor ou elevador carrega a fase seguinte). Cada fase continua tendo início e fim para medir
  tempo e trocas no ranking.
- **Paredes especiais** (peças das fases):
  - **Só entidades passam:** o jogador atravessa, os objetos não.
  - **Só objetos passam:** os objetos atravessam, o jogador não.
  - **Restauração:** deixa passar, mas quem atravessa recupera o **valor verdadeiro** trocando com
    quem está com ele. Vale para posição, peso e energia (ver "modelo de variáveis" abaixo).
    Ex.: eu 10 kg, A 2 kg, B 3 kg. Troco com A (eu 2, A 10) e com B (eu 3, B 2). A atravessa a parede →
    troca com B, que estava com o peso verdadeiro dela → eu 3, A 2, B 10.
  - **Combinadas:** a **parede de saída** de cada fase junta "só entidades" + "restauração": nenhum
    objeto e nenhum valor trocado passa para a próxima fase.
  - **Restauração de posição:** também vale. Ex.: uma caixa em movimento bate na parede e troca de
    posição com o objeto parado que estava com a variável de posição dela. Eles trocam só de lugar,
    não de energia: a caixa continua em movimento e o objeto que estava parado aparece na parede e
    só cai no chão.
  - **A parede é um filtro:** cada parede define o que passa (jogador, objetos) e quais variáveis
    passam. A **mira da troca obedece ao mesmo filtro**: não dá para trocar
    nada com uma caixa atrás de uma parede que barra variáveis. (A arquitetura ainda permite filtrar
    por tipo de variável no futuro, mas o jogo usa sempre "todas".)
  - **Cada variável volta para o seu dono:** se o objeto trocou posição com um e peso com outro, ao
    atravessar a parede a posição volta de um e o peso volta do outro.
  - **Dentro de uma barreira que bloqueia trocas** (barreira não sólida, o objeto está *dentro* dela):
    o objeto fica com os atributos originais e não pode ser trocado até sair. O jogador dentro dela
    não consegue usar a troca. Encostar numa parede sólida não conta.
  - **Barreira que só bloqueia a passagem** (de objetos ou do jogador): restringe só o **segurar**.
    Se eu atravesso segurando um objeto que não pode passar, a pegada se solta quando um dos dois
    atravessa 100% e o objeto cai.
  - **Decisão: 3 barreiras básicas, combináveis** (para não confundir o jogador):
    1. **Barra objetos:** só o jogador passa; o objeto segurado cai (cor própria + ícone 📦 riscado).
    2. **Barra o jogador:** só os objetos passam (cor própria + ícone 🧍 riscado).
    3. **Barra variáveis:** barra **todas** as variáveis de uma vez (não uma por tipo). Quem passa
       recupera todas as próprias variáveis; não dá para trocar nada através dela
       nem dentro dela (cor própria + ícone ⇄ riscado).
    As outras são combinações. A **grade de saída** de cada fase = 1 + 3 (aparece desde a fase 1).
    As barreiras sozinhas entram aos poucos, como peças de puzzle, a partir das fases do P1.
    Se um dia fizer sentido, dá para voltar a filtrar por tipo de variável.
  - **Zonas (blocos de barreira):** a barreira é um **volume**; a parede é só um volume fino. Uma zona
    é o mesmo volume, maior (ex.: uma sala inteira onde não dá para trocar nada). No JSON muda só o
    tamanho. Puzzles: preparar as trocas antes de entrar na sala; "cofre" de caixas onde só o jogador
    entra; objeto dentro da zona sempre com os valores originais, servindo de referência.
  - **Nas referências:** paredes assim são usadas no gênero para montar as fases e impedir que objetos
    e valores sejam levados para a fase seguinte.
- **Modelo de variáveis (troca de variáveis, não de valores):** cada objeto nasce com a sua própria
  variável por propriedade (Xa, Xb, Xc…). A troca muda **quem usa qual variável**; a física continua
  alterando o valor da variável enquanto outro objeto a usa. Na restauração, o objeto pega a **sua
  variável de volta**, com o valor que ela tiver naquele momento, e quem estava com ela fica com a
  variável que o objeto usava. Ex.: A usa Xc e B usa Xa; A atravessa → A volta a usar Xa, B fica com Xc.
  O peso e a energia usam o mesmo mecanismo (a variável de peso só não muda sozinha).
- **Impacto na arquitetura:** a biblioteca `rules` guarda as variáveis e quem está usando cada uma.
  As variáveis só são embaralhadas: cada uma está sempre com exatamente um recipiente.

---

## 🧩 Possíveis (com espaço na arquitetura)

### Alcance da troca (eixo de evolução 1)
- **Objeto ↔ objeto:** upgrade do aparelho; mirar em dois objetos.
- **Arma que guarda valores:** suga um valor, guarda e solta em outro objeto depois.

### Combinação de trocas (eixo de evolução 2)
- **Duas habilidades com intervalo** (já nas fases 7–10) → **duas ao mesmo tempo**, com intervalo zero.
- **Fase 11 extra:** troca simultânea.

### Duração da troca
- **Objetos instáveis:** a troca com eles volta sozinha depois de X segundos (modelo C).
- **Troca temporária** como regra geral (modelo B) — mais tensão, mais difícil de desenhar.

### Modificadores da recarga
- **Power-up temporário:** ao consumir, a recarga some por alguns segundos.
- **Sem recarga enquanto segura algo.**
- **Peso glorioso:** um objeto especial; enquanto o jogador está com o peso dele, a recarga some.
- **Downgrade de tempo limitado:** efeito negativo temporário (ex.: recarga maior por um tempo).

### Novas propriedades
- **Momento/velocidade:** ainda sem um jeito claro de aplicar. Lembrete: momento tem direção, energia não.
- **Mais 2 habilidades numéricas** (a escolher). Candidatas:
  - **Temperatura** (°C): derreter gelo, congelar água em ponte.
  - **Elasticidade** (0 a 1): roubar o "quique" de uma bola de borracha.
  - **Atrito** (coeficiente): rampa lisa ou que gruda.
  - **Gravidade do objeto** (m/s²): objeto que "cai para cima".
  - **Carga elétrica** (C): atrair e repelir, ligar circuitos.
- Do documento original: tamanho, direção da gravidade.

### Densidade (consequência do peso com volume constante)
- **3 meios com densidade:** **ar** (1,2 kg/m³, o padrão em todo lugar), **gás denso** (6,1 kg/m³,
  como o hexafluoreto de enxofre real) e **água** (1.000 kg/m³). Cada meio é uma zona.
- **Flutuar no ar:** se a densidade do objeto for menor que a do meio, ele sobe, inclusive no ar.
  Ex.: o jogador (0,07 m³) só flutua no ar se ficar mais leve que 0,084 kg, por exemplo trocando o
  peso com um balão de 0,05 kg.
- **3 estados em cada meio** (simplificação de jogo): **afunda** (densidade > 1,1 × a do meio, não
  sobe), **nada** (entre 0,1 × e 1,1 ×, sobe e desce à vontade; no ar ou no gás é "voar") e **boia**
  (< 0,1 ×, é empurrado para cima e não mergulha). Na água: afunda acima de 1.100, nada de 100 a 1.100,
  boia abaixo de 100 kg/m³.
- **Segurar objetos conta:** jogador + objeto viram um corpo só, com densidade = massas somadas ÷
  volumes somados. Mais leve que o ar segurando uma pedra não sobe; soltando, sobe.
- **Ação e reação ao lançar:** quem não está apoiado no chão (nadando ou boiando) é empurrado para o
  lado contrário ao lançar algo (conservação do momento). No ar e no gás desliza por muito tempo, como
  no espaço; na água para logo, porque a água freia mais. É assim que o **momento** entra no jogo:
  pela física, sem virar habilidade de troca.
  **Soltar ≠ lançar:** só soltar não empurra (a gravidade puxa a pedra, não você); você só sobe se
  ficou mais leve que o meio. Lançar com força empurra para o lado contrário. São duas ações no jogo.
- **Nado (física):** a velocidade máxima de nado é a mesma em todos os meios; o que muda é a
  resistência (na água para rápido, no ar e no gás desliza como no espaço).
- **Pulo (fisicamente correto, ajustar depois):** as pernas dão uma energia fixa por pulo, com limite de
  velocidade; mais leve = pula mais alto, mas cai na velocidade normal. Só perto da densidade do meio a
  queda fica lenta, "lunar". Ex.: 70 kg → 0,5 m; 35 kg → 1 m; 300 kg → 12 cm.
  ⚠️ Recado: testar jogando e comparar com um pulo ajustado "de jogo".
- **Afundar ou boiar:** na água, objetos com densidade maior que a da água afundam.
- **Quebrar coisas quebradiças:** vidro, tábua fina, gelo. Uma peça quebradiça quebra quando a
  pancada ou o peso em cima passa de um limite, e o limite aparece como número.
  Ex.: o chão de vidro aguenta 50 kg; fico com o peso do bloco de ferro (200 kg) e caio para o andar
  de baixo.
- **Pressão (força ÷ área de contato):** o que quebra é a pressão, não só o peso. Um objeto pequeno e
  pesado quebra o vidro mais fácil que um objeto grande com o mesmo peso.
  Ex.: 10 kg sobre 0,01 m² → P = 10·9,8 / 0,01 = 9.800 Pa; os mesmos 10 kg sobre 0,1 m² → 980 Pa.
  O limite da peça quebradiça passa a ser uma pressão.

### Tempo como habilidade (roubar e entregar, não trocar)
- **Roubar tempo:** só funciona com objetos em **movimento constante** (ventilador, pêndulo, esteira,
  plataforma que vai e volta). O objeto fica mais lento ou para, e o jogador acumula esse tempo.
- **Efeito no jogador:** quem tem mais tempo fica "mais rápido" que o mundo, então **o mundo ao redor
  fica mais lento**. Quem entrega o próprio tempo fica "mais lento", então **o mundo ao redor fica
  mais rápido**.
- **Entregar tempo:** o jogador aplica o tempo guardado em si mesmo ou em outro objeto, que acelera.
- **É numérico:** ex.: escala de tempo 0,5× / 1× / 2× na tela. Forte candidata a uma das
  2 habilidades numéricas futuras.
- **O que muda na arquitetura:** hoje tudo é **troca** (A ↔ B). O tempo é uma **transferência de mão
  única** (A → reserva → B), igual à ideia da arma que guarda valores. A arquitetura passa a prever
  dois tipos de operação: trocar e transferir, com uma "reserva" no jogador ou na arma.
- **O jogador nunca muda:** a velocidade e a sensibilidade do personagem continuam iguais; só o mundo
  ao redor acelera ou desacelera.
- **Cuidado técnico:** desacelerar o mundo todo no Godot também desacelera o jogador; vai precisar de
  uma escala de tempo por objeto.

### Fantasma (compartilhar a partida jogada) — se sobrar tempo
- Assistir à partida dos primeiros do ranking como um **fantasma** do personagem andando pela fase.
- Grava **só o personagem**: posição e ângulo/direção da cabeça, várias vezes por segundo.
  **Os objetos não aparecem** e a física não é re-simulada.
- Tamanho pequeno: ~20 amostras por segundo × 5 números ≈ 120 KB para 5 minutos de partida.
- Na API: arquivo opcional junto do resultado, com limite de tamanho e validação de formato; só os
  primeiros do ranking ficam disponíveis. O módulo `progress` já nasce com esse espaço.
- Evolução possível: replay completo, com os objetos e as trocas.

### Plataforma
- **Steam** via Steamworks (login, ranking e saves da Steam no lugar da API).
- **Conta da Steam no lugar da chave secreta** do perfil.
- **Contas com senha própria:** só se um dia fizer sentido fora da Steam.

---

## 💭 Ideias pensadas
- **Tempo como dificuldade:** trocar a velocidade de algo em queda no momento certo.
- **Ideias descartadas na sessão 1** (substituídas pela ideia própria): puzzle diário com semente,
  fases criadas pelos jogadores com solver, jogo educativo de programar um robô.
