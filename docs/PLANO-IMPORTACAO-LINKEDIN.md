# Plano de Importação do LinkedIn e Análise de Lacunas (Gap Analysis)

## Visão Geral

Este documento descreve a arquitetura e o plano de implementação da funcionalidade de **Importação do LinkedIn ("Import from LinkedIn")** e do **Diagnóstico de Perfil (Gap Analysis)** no sistema CareerOS.

A solução visa reutilizar e centralizar os dados profissionais já cadastrados pelo candidato no LinkedIn (através da exportação em PDF "Salvar como PDF"), evitando o re-preenchimento manual e fornecendo recomendações estratégicas baseadas nas melhores práticas de criação de currículos e posicionamento no mercado.

---

## 1. Arquitetura e Fluxo de Dados

```
[ Usuário ] -> Seleciona PDF do LinkedIn no Frontend (Angular)
                     │
                     ▼
             [ API ASP.NET Core 9 ] -> Endpoint POST /api/candidate-profiles/import-linkedin
                     │
         ┌───────────┴───────────┐
         ▼                       ▼
[ LinkedinParserService ]  [ LinkedinGapAnalysisService ]
(UglyToad.PdfPig)          (Calcula Score % e Gaps)
         │                       │
         └───────────┬───────────┘
                     ▼
             [ Resposta JSON ] -> ParsedProfile + GapAnalysis
                     │
                     ▼
            [ Frontend Angular ]
      ┌──────────────┴──────────────┐
      ▼                             ▼
[ Modal Comparativo ]     [ Aba Dicas / TO-DOs ]
(Mesclagem Lado a Lado)    (Nível de Completação %)
```

---

## 2. Componentes Backend (C# / .NET 9)

### 2.1 Parser de PDF do LinkedIn (`LinkedinParserService`)
- **Biblioteca:** `UglyToad.PdfPig` (extração performática de texto de documentos PDF).
- **Suporte Bilingüe:** Mapeamento inteligente de seções nos idiomas Português (PT) e Inglês (EN):
  - *Resumo / Summary / Sobre / About*
  - *Experiência / Experience / Work Experience*
  - *Formação Acadêmica / Education*
  - *Licenças e Certificações / Certifications*
  - *Principais Competências / Core Skills*
  - *Idiomas / Languages*
  - *Contato / Contact*
- **Extração por Regex e Heurísticas:** Leitura de e-mails, telefones com DDD/DDI, períodos de experiência (ex: "Jan 2020 - Presente") e parsing de datas.

### 2.2 Diagnóstico do Perfil (`LinkedinGapAnalysisService`)
Calcula a pontuação de completude do perfil (0 a 100%) e gera itens acionáveis:
1. **Resumo Profissional Faltante:** Recomenda redação de 3 a 5 frases focadas em valor e resultados.
2. **Contato Incompleto:** Identificação da ausência de telefone/WhatsApp ou localização (Cidade/País).
3. **Formação Acadêmica e Certificações:** Alerta sobre a falta de registros acadêmicos ou badges técnicas.
4. **Métricas Quantificáveis nas Experiências:** Análise de presença de números, percentuais (ex: `%`, `$`, `R$`, `usuários`, `equipe`) nas descrições de cargo.

### 2.3 Contratos de API (`LinkedinImportContracts.cs`)
- `ParsedCandidateProfileDto`: DTO com os dados estruturados extraídos do PDF.
- `GapAnalysisDto` & `GapItemDto`: DTOs com o score percentual, lista de campos faltantes e recomendações categorizadas por severidade (`high`, `medium`, `low`).
- `LinkedinImportResponseDto`: Wrapper unificado retornado ao frontend.

---

## 3. Interface do Usuário Frontend (Angular 21)

### 3.1 Banner de Importação do LinkedIn
- Banner destacado no topo da área autenticada permitindo o envio do PDF em um clique (`📄 Importar PDF do LinkedIn`).

### 3.2 Modal Lado a Lado (Side-by-Side Merge)
- Exibe duas colunas para comparação direta:
  - **Coluna Esquerda:** Dados atuais armazenados no CareerOS.
  - **Coluna Direita:** Dados recém-extraídos do PDF do LinkedIn.
- **Ações de Mesclagem:**
  - Botão `Usar Este` em cada campo individual (Nome, Cargo, Resumo, Contato, Localização).
  - Botão `Substituir e Mesclar Todos os Dados do LinkedIn` para importação total de experiências, formações e certificações nos Reactive Forms da aplicação.

### 3.3 Aba de Diagnóstico e Recomendações ("💡 Dicas & TO-DOs")
- Exibição da nota percentual de completude do perfil com selo colorido de desempenho.
- Tags visualizando campos não preenchidos.
- Cards de recomendações prioritárias com dicas práticas para destaque no mercado de trabalho e passagem em filtros ATS.

---

## 4. Plano de Testes e Validação

1. **Testes Unitários de Backend (`LinkedinParserAndGapAnalysisTests.cs`):**
   - Validação da detecção de ausência de resumo e telefone.
   - Validação da identificação de falta de métricas quantificáveis em descrições de cargos.
   - Validação de execução de testes com `DOTNET_ROLL_FORWARD=Major dotnet test`.

2. **Testes Unitários de Frontend (`npm test`):**
   - Execução das suítes de teste Karma/Jasmine em `ChromeHeadless`.

3. **Compilação e Verificação Visual:**
   - Verificação de build com `npm run build`.
   - Teste de fluxo completo do formulário com salvamento em rascunho local (`localStorage`) e persistência via API REST.
