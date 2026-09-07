# Plano atualizado — Importação do LinkedIn e análise de lacunas

> Escopo: Tarefa #10 do backlog. Este documento foi atualizado a partir do código existente em `backend/CareerOS.Api` e `frontend/src/app`. O objetivo é concluir o fluxo ponta a ponta sem tratar como futuro o que já está implementado.

## 1. Estado atual — fotografia do código

### 1.1 Backend já implementado [DONE]

- `LinkedinParserService` implementa `ILinkedinParserService` e extrai texto de PDF usando `UglyToad.PdfPig`.
- O parser reconhece cabeçalho, e-mail, localização e seções em português/inglês para resumo, experiência, formação, certificações, competências, idiomas e contato. (Nota: a seção `CONTACT`/`Contato` é reconhecida como cabeçalho, mas **não é processada** — telefone não é extraído, apenas e-mail e localização do cabeçalho.)
- O parser produz `ParsedCandidateProfileDto`, com experiências, formação e certificações estruturadas, além de competências e idiomas.
- `LinkedinGapAnalysisService` implementa `ILinkedinGapAnalysisService` e calcula `CompletenessScore`, `MissingFields` e recomendações com severidade.
- Os contratos `ParsedCandidateProfileDto`, `GapAnalysisDto`, `GapItemDto` e `LinkedinImportResponseDto` existem em `Contracts/LinkedinImportContracts.cs`.
- `CandidateProfileService.ImportLinkedin(IFormFile)` valida arquivo vazio e extensão/content-type PDF, faz parse e análise e retorna `ParsedProfile` + `GapAnalysis`.
- `POST /api/candidate-profiles/import-linkedin` existe em `CandidateProfilesController`, exige autenticação pelo `[Authorize]` da controller. Responde `200` em caso de sucesso e `400` quando a validação de `ImportLinkedin` retorna erro (arquivo vazio/ausente ou tipo/extensão não-PDF), mas **não captura exceções de parse**: um arquivo com bytes inválidos termina em `500` através de `PdfDocument.Open`.
- Os serviços estão registrados no DI em `Program.cs`.

### 1.2 Frontend já implementado [DONE]

- `CandidateProfileService.importLinkedin(file)` envia `multipart/form-data` no campo `file` para o endpoint.
- `app.html` exibe banner de importação, input `.pdf`, estado de carregamento, tabs de perfil versus dicas e seção de gap analysis.
- `app.ts` recebe o arquivo, rejeita nomes sem `.pdf`, chama a API, guarda `parsedProfile` e `gapAnalysis` e abre o modal em caso de sucesso.
- O modal existente compara os valores atuais do formulário com os valores extraídos e tem ações de aplicar campo individual e de mesclar todos os dados.
- `mergeAllLinkedinData()` popula campos simples e recria arrays de experiências, formações e certificações.
- A análise mostra score, campos ausentes, severidade e recomendações.
- A alteração do formulário dispara o mecanismo de rascunho; a mesclagem explícita chama `saveDraft()`.

### 1.3 Testes existentes [DONE — cobertura parcial]

- `LinkedinParserAndGapAnalysisTests.cs` cobre duas regras do gap analysis: ausência de resumo/contato e ausência de métricas em experiência.
- `candidate-profile.service.spec.ts` cobre criação do serviço e `save()` para POST/PUT.
- `app.spec.ts` cobre formulário, arrays, salvamento do perfil, logout e exportação.

### 1.4 O que ainda não está concluído [PENDENTE]

1. **Persistência após a mesclagem:** importar/mesclar não chama `saveProfileToApi()`; o resultado fica no formulário e em `localStorage` até o usuário salvar manualmente. Não há endpoint de confirmação de importação nem histórico/auditoria.
2. **Merge de dados:** o comportamento atual é “substituir arrays inteiros quando o array importado não está vazio”, não um merge semântico lado a lado. Não há seleção por item para experiências, formação ou certificações, nem proteção contra substituir alterações do usuário.
3. **Mapeamento incompleto:** `Skills` e `Languages` são extraídos e devolvidos, mas não existem nos campos do `CandidateProfile`, no `CandidateProfileRequest/Response` ou no formulário atual. `PreferredName` também não é extraído. O mapeamento de educação usa valores artificiais (`Bacharelado` e `2020-01-01`) quando o PDF não fornece esses dados.
4. **Robustez do parser:** não há fixtures reais de PDFs, teste do parser, teste de PDF vazio/imagem-only, validação de limites, nem contrato explícito para layouts diferentes. A heurística de experiência, formação e certificação depende da ordem das linhas e pode atribuir dados incorretos.
5. **Tratamento de erro:** arquivo com extensão `.pdf` mas conteúdo inválido pode lançar exceção (`PdfDocument.Open` → `500`); não há limite de tamanho, validação de assinatura/magic bytes, timeout/cancelamento, mensagem específica para PDF sem texto ou telemetria. O backend aceita PDF se content-type **ou** extensão for compatível.
6. **Testes de integração:** não há teste da rota autenticada com `multipart/form-data`, respostas `400`, isolamento do usuário, PDF válido e composição do DTO completo.
7. **Testes do comportamento do modal:** não há testes para seleção do arquivo, sucesso/erro da importação, abertura/fechamento, aplicação de campo, mesclagem de arrays, preservação de dados e chamada de persistência.
8. **Acessibilidade:** o modal não declara `role="dialog"`, nome acessível, `aria-modal`, foco inicial/retorno, fechamento por `Escape` ou bloqueio de foco; o input de arquivo está visualmente oculto sem fluxo alternativo claramente testado.
9. **i18n e conteúdo:** textos, severidades, categorias e mensagens estão hard-coded em português no Angular e no serviço de análise. O parser aceita PT/EN, mas a interface e as recomendações não têm estratégia de idioma.
10. **Segurança/privacidade:** o PDF é processado em memória, o que é desejável, mas não existe política explícita de retenção, limite de payload, sanitização de conteúdo, rate limiting específico, logging sem dados pessoais ou confirmação de que o arquivo não será armazenado.
11. **Consistência de contrato:** a UI usa `any`; não há tipos TypeScript para a resposta. O botão “mesclar todos” pode substituir arrays com dados existentes e o rótulo não deixa claro que a ação não salva automaticamente na API.

## 2. Fluxo alvo

```text
Usuário autenticado
  -> seleciona PDF
  -> API valida tamanho/tipo e extrai dados
  -> parser normaliza resultado
  -> gap analysis calcula diagnóstico
  -> UI mostra preview tipado e comparação por campo/item
  -> usuário escolhe substituir, manter ou combinar
  -> formulário é validado
  -> usuário confirma
  -> perfil é salvo na API em uma operação normal de create/update
  -> UI confirma persistência e atualiza o rascunho
```

O endpoint de análise deve continuar sem persistir o PDF nem alterar o perfil. A confirmação deve usar a mesma operação de perfil já existente, garantindo uma única fonte de verdade para a gravação. Se for necessário rastrear a origem, adicionar metadados de importação somente após decisão explícita de produto.

## 3. Decisões e trade-offs debatidas

### PDF exportado versus HTML/API do LinkedIn

- **Alternativa PDF:** é o formato atualmente disponível ao usuário, não exige OAuth nem permissões de terceiros e mantém o processamento sob controle do CareerOS; em contrapartida, o layout e a ordem do texto são instáveis.
- **Alternativa HTML ou API:** pode preservar mais estrutura; exige exportação diferente ou integração OAuth, aumenta custo, dependências, privacidade e superfície de segurança, e a API do LinkedIn tem restrições de acesso.
- **Recomendação:** concluir primeiro o PDF como contrato suportado, com fixtures de versões PT/EN e tratamento explícito de baixa confiança. Deixar HTML/API fora da Tarefa #10, como evolução separada.

### Persistir automaticamente versus apenas pré-visualizar

- **Persistência automática:** reduz cliques, mas pode sobrescrever dados corretos e torna difícil desfazer uma extração imperfeita.
- **Pré-visualização sem persistência:** dá controle ao candidato e evita mutação acidental; exige uma confirmação clara e mais um passo.
- **Recomendação:** manter parse/análise sem efeito colateral e persistir somente após revisão/confirmar. O botão final deve chamar o salvamento da API e informar sucesso/erro; `localStorage` permanece apenas como rascunho de recuperação.

### Onde colocar a lógica de merge

- **Frontend:** permite comparação e escolha por campo/item, preserva a intenção do usuário e reutiliza o formulário existente.
- **Backend:** centraliza regras, facilita auditoria e clientes futuros, mas não conhece a seleção visual e pode aumentar o risco de sobrescrita implícita.
- **Recomendação:** manter no frontend a seleção/interação e o mapeamento para o formulário; manter no backend validação, normalização, limites e persistência final. Se o merge evoluir para vários clientes, extrair uma biblioteca/serviço de domínio compartilhado no backend, sem colocar “substituir tudo” escondido no endpoint de upload.

### Substituir arrays versus mesclar semanticamente

- **Substituir:** simples e previsível para uma primeira importação, mas destrutivo quando já há dados.
- **Mesclar por similaridade:** preserva registros, porém exige critérios de matching por empresa/cargo/período e revisão de conflitos.
- **Recomendação:** oferecer “usar item importado”, “manter atual” e “adicionar como novo” por item; manter substituição total somente como ação explícita, com confirmação e aviso do impacto. Nunca inventar valores não extraídos.

### Score fixo versus regras versionadas

- **Score fixo atual:** simples, mas os pesos estão embutidos no serviço e não comunicam versão nem justificativa.
- **Regras configuráveis/versionadas:** permitem evolução e explicabilidade, com maior complexidade.
- **Recomendação:** primeiro tornar as regras determinísticas, testadas e documentadas; depois extrair pesos/versão para configuração ou um objeto de política. A resposta deve identificar que o score é heurístico, não uma avaliação definitiva de empregabilidade.

## 4. Fases de implementação

### Fase 0 — Contrato e observabilidade

**Entregas**

- Tipar a resposta no TypeScript e documentar o contrato JSON, incluindo campos nulos, listas vazias e formato ISO de datas.
- Definir limite de upload, comportamento para PDF sem texto, erros públicos e correlação de logs sem registrar conteúdo pessoal do PDF.
- Definir se `Skills`/`Languages` entram no perfil persistido nesta tarefa. Recomendação: se não entrarem agora, exibi-los como “somente importados” e registrar a decisão; não descartá-los silenciosamente.

**Critérios de aceite**

- Existe uma especificação do contrato consumida pelo backend e frontend.
- Nenhum PDF é armazenado após a requisição; logs não contêm nome, e-mail, telefone ou texto extraído.
- Limite, tipo inválido, PDF ilegível e ausência de texto têm respostas estáveis e mensagens acionáveis.

**Testes**

- Casos de contrato com resposta completa, parcial e vazia.
- Testes de limite, arquivo vazio, content-type divergente e conteúdo que não é PDF.

### Fase 1 — Parser confiável e gap analysis determinística

**Entregas**

- Adicionar fixtures sanitizadas de PDFs PT/EN e, se possível, PDFs sintéticos com layouts controlados.
- Testar extração de cabeçalho, e-mail, localização, resumo, experiência, datas, formação, certificação, skills e idiomas.
- Tornar explícitos campos não encontrados, evitar valores inventados e revisar a heurística de datas/linhas.
- Cobrir perfil completo, perfil vazio, apenas imagem, seções desconhecidas, caracteres acentuados e datas “atual/presente”.
- Documentar a fórmula do score, pesos, regra de métricas e versão da política.

**Critérios de aceite**

- Cada campo suportado possui caso positivo e negativo.
- PDF sem texto não produz exceção não tratada e é distinguido de perfil realmente vazio quando possível.
- O mesmo fixture produz o mesmo DTO e score em execuções repetidas.

**Testes**

- Testes unitários do parser e do gap analysis, incluindo limites 0/100 e listas nulas/vazias.
- Testes de regressão para cada fixture; não considerar apenas os dois testes atuais de gap analysis como cobertura suficiente.

### Fase 2 — API segura e integração backend

**Entregas**

- Validar assinatura/conteúdo do PDF, tamanho máximo e número/complexidade de páginas conforme a capacidade definida.
- Traduzir exceções de parsing para `400`/`422` consistente; preservar `401`/`403` e não vazar detalhes internos.
- Considerar `CancellationToken` e limites de tempo/recursos.
- Adicionar testes da controller/endpoint com autenticação, multipart correto, erro de arquivo e resposta completa.
- Confirmar que o endpoint não lê nem altera `CandidateProfile` nem grava no banco.

**Critérios de aceite**

- Upload inválido não derruba a requisição nem revela stack trace.
- Usuário não autenticado não acessa a rota; não há possibilidade de acessar perfil de outro usuário pelo import.
- Upload válido devolve `parsedProfile` e `gapAnalysis` compatíveis com o contrato.

**Testes**

- Teste de integração HTTP com `multipart/form-data` e arquivo fixture.
- Casos `401`, `400`, `422`, PDF válido e parser que lança exceção.
- Verificação de que não houve `SaveChanges`/mutação do perfil durante a análise.

### Fase 3 — Preview e merge no Angular

**Entregas**

- Substituir `any` por interfaces de importação e um adaptador explícito DTO -> formulário.
- Corrigir/decidir o mapeamento de educação e não preencher datas/grau fictícios; tratar `Skills`/`Languages` conforme a decisão da Fase 0.
- Transformar o modal em comparação por campo e por item: manter atual, usar importado, adicionar e remover/ignorar.
- Mostrar aviso quando uma ação substitui dados existentes; garantir que listas vazias não apaguem dados sem confirmação.
- Separar “aplicar ao formulário/rascunho” de “salvar no CareerOS” e fornecer ação final de confirmação.

**Critérios de aceite**

- O usuário consegue revisar cada campo importado antes de alterar o formulário.
- Nenhum valor ausente no PDF apaga valor existente por ação padrão.
- Após confirmar, o payload salvo usa o mesmo schema de create/update já aceito pelo backend.
- Erro de API mantém a seleção/formulário recuperável e informa o próximo passo.

**Testes**

- Service spec para `importLinkedin`: URL, método, `FormData` e campo `file`.
- Component spec para arquivo inválido, loading, sucesso, erro, abertura/fechamento do modal, campo simples, localização, arrays, cancelamento e confirmação.
- Teste de regressão para não substituir dados quando a lista importada estiver vazia.

### Fase 4 — Persistência ponta a ponta

**Entregas**

- Fazer a confirmação do modal disparar `saveProfileToApi()` ou um método de domínio equivalente, sem duplicar regra de payload.
- Atualizar `candidateId`, rascunho e estado visual somente conforme o resultado da API.
- Após salvar, recarregar o perfil ou reconciliar a resposta para garantir que a UI mostra o estado persistido.
- Definir idempotência e comportamento ao importar o mesmo PDF novamente; por padrão, não criar duplicatas silenciosamente.

**Critérios de aceite**

- Um usuário importa, revisa, confirma, recarrega a página e encontra os dados persistidos.
- A confirmação falha de forma recuperável sem declarar sucesso falso.
- O perfil e suas coleções são salvos atomicamente pelo create/update existente.

**Testes**

- Integração frontend com `HttpTestingController`: importação seguida de PUT/POST e payload final.
- Teste backend de persistência via fluxo normal de perfil, incluindo coleções.
- Caso de recarregamento/relogin e caso de falha de rede no salvamento.

### Fase 5 — Acessibilidade, i18n e segurança operacional

**Entregas**

- Modal acessível: `role`, `aria-modal`, título referenciado, foco inicial/retorno, `Escape`, navegação por teclado e foco preso enquanto aberto.
- Labels, estados de loading/erro/sucesso e anúncio do score compatíveis com leitor de tela; foco visível e layout responsivo.
- Extrair textos para a estratégia de i18n escolhida; traduzir UI, severidades, categorias e mensagens.
- Aplicar política de tamanho, rate limit/abuso conforme infraestrutura, Content Security Policy quando aplicável e logs sem PII.
- Publicar nota de privacidade: o PDF é processado para análise e não persistido, salvo decisão futura explícita.

**Critérios de aceite**

- Fluxo completo operável por teclado e auditado com leitor de tela.
- Português e inglês cobrem todo o fluxo, inclusive erros e recomendações.
- Controles de segurança estão documentados e verificados em ambiente de integração.

**Testes**

- Testes de acessibilidade automatizados e checklist manual do modal.
- Testes de troca de idioma e snapshots/asserções de mensagens.
- Testes de segurança para payload grande, PDF malformado, conteúdo sem texto e repetição de requisições.

### Fase 6 — Release e monitoramento

**Entregas**

- Definir métricas agregadas: sucesso/falha de parse, PDF sem texto, tempo de processamento, confirmação e falha de salvamento.
- Adicionar feature flag se o rollout gradual for necessário.
- Validar com PDFs exportados reais e sanitizados, sem coletar o documento em logs.

**Critérios de aceite**

- Há rollback/desativação do fluxo sem perder o formulário existente.
- Alertas para aumento de falhas e documentação de suporte estão disponíveis.

**Testes**

- Smoke test do fluxo autenticado em ambiente de staging.
- Teste exploratório com diferentes idiomas, tamanhos, páginas e perfis parcialmente preenchidos.

## 5. Ordem recomendada para concluir a Tarefa #10

1. Fase 0 para fixar contrato, privacidade e limites.
2. Fases 1 e 2 para tornar o backend confiável e seguro.
3. Fase 3 para substituir o merge destrutivo/implícito por revisão explícita.
4. Fase 4 para fechar a persistência ponta a ponta.
5. Fases 5 e 6 antes do rollout amplo.

## 6. Definição de pronto da feature

- PDF válido é analisado sem mutar o perfil antes da confirmação.
- O usuário vê dados atuais e importados, revisa conflitos e não perde dados por padrão.
- Dados confirmados são persistidos no backend, incluindo coleções suportadas, e sobrevivem a reload/login.
- Parser, gap analysis, API, service Angular, modal e persistência possuem testes proporcionais ao risco.
- Erros, limites, segurança, privacidade, acessibilidade e idioma estão documentados e validados.

## Resumo executivo

A implementação atual já entrega o núcleo técnico de leitura de PDF, análise heurística, endpoint autenticado e interface de preview/mesclagem, mas ainda não conclui a persistência nem garante um merge seguro e testado; hoje a mesclagem é aplicada ao formulário e ao rascunho local, não ao perfil salvo automaticamente. O plano atualizado prioriza contrato e limites, fixtures e testes reais do parser, integração da API, merge explícito por campo/item, confirmação que salva via fluxo normal do perfil, e depois acessibilidade, i18n, privacidade e observabilidade. A recomendação central é manter PDF como primeira versão, manter análise sem efeito colateral e fazer a decisão de merge no frontend com validação/persistência no backend, sem inventar valores ausentes.
