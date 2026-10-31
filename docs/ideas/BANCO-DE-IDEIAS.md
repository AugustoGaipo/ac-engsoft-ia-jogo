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
- **Energia cinética:** quem recebe sai na direção em que o outro se movia.
- **Peso com movimento:** a energia se mantém e a velocidade se ajusta.

### Design de fases
- 10 fases: 2 por habilidade sozinha, 1 por dupla, 1 com as três (ordem no one-pager).
- Fases seguidas com a mesma habilidade, como no Portal.
- Controles de dificuldade: quantos objetos, quantas propriedades, ordem das trocas, limite de trocas.
- Contra atalhos: objetos travados, propriedades permitidas por cor, só trocar com o que está à vista.
- Botões que só detectam objetos ou exigem mais peso que o jogador.

### Tecnologia e plataforma
- Godot 4 com C#; biblioteca de regras em C# puro com testes.
- Fases montadas a partir de arquivos JSON com peças reutilizáveis.
- Jogo offline; progresso enviado para a API quando há internet.
- API com login, progresso e ranking; sem validação antitrapaça.
- Visual simples e limpo (formas geométricas, cores sólidas).

---

## 🔨 Em andamento
- **Duração da recarga** nas fases 7–10: decidir conforme a dificuldade de cada fase.
- **Nome do jogo:** "Swapping" provisório.
- **Ordem exata das fases:** proposta no one-pager, ainda pode mudar.

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

### Plataforma
- **Steam** via Steamworks (login, ranking e saves da Steam no lugar da API).

---

## 💭 Ideias pensadas
- **Água:** trocar o peso faz objetos afundarem ou boiarem (consequência do volume constante).
- **Tempo como dificuldade:** trocar a velocidade de algo em queda no momento certo.
- **Ideias descartadas na sessão 1** (substituídas pela ideia própria): puzzle diário com semente,
  fases criadas pelos jogadores com solver, jogo educativo de programar um robô.
