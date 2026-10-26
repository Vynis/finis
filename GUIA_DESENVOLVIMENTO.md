# 📘 Guia Técnico de Desenvolvimento & Manutenção - Finis

Este documento serve como referência técnica detalhada para qualquer desenvolvedor que vá realizar novos ajustes, melhorias ou extensões no sistema **Finis**.

---

## 1. 🧮 Motor Matemático do Termômetro Financeiro

O arquivo central de regras de negócio é o `backend/Finis.Api/Services/TermometroEngineService.cs`.

### Como o saldo é calculado:
1. **Ponto de Partida**: O usuário possui `User.InitialBalance` (definido no cadastro e editável via `/api/auth/initial-balance`).
2. **Saldo de Transporte (Carry-over)**:
   ```csharp
   public async Task<decimal> GetCarryOverBalanceAsync(int userId, DateTime targetMonthStart)
   ```
   Soma todas as Entradas históricas do usuário anteriores a `targetMonthStart` e subtrai todas as Saídas Fixas e Diárias. O resultado é somado ao `InitialBalance` do usuário.
3. **Cascata Diária**:
   Para cada dia $d \in [1, \text{diasNoMes}]$:
   ```csharp
   runningBalance = runningBalance + dayIncome - (dayFixed + dayDaily);
   ```
4. **Métricas Consolidadas**:
   - `Performance = TotalIncome - TotalExpenses`
   - `SavingsRate = (Performance / TotalIncome) * 100`

---

## 2. 🗃️ Banco de Dados & Entity Framework Core

### Entidades (`Finis.Api/Entities/`):
- `User`: Id, Name, Email, PasswordHash, InitialBalance, CreatedAt.
- `Category`: Id, Name, Type (Entrada = 1, SaidaFixa = 2, Diario = 3), ColorHex, Icon, IsDefault, UserId.
- `Transaction`: Id, UserId, Date, Type, Amount, Description, CategoryId, Status (Realizado = 1, Previsto = 2), PaymentMethod, Notes.

### Comandos Úteis do Entity Framework:

#### Criar uma nova migration após alterar entidades:
```powershell
cd backend/Finis.Api
dotnet ef migrations add NomeDaAlteracao
```

#### Aplicar migrações pendentes no MySQL:
```powershell
dotnet ef database update
```

#### Gerar script SQL atualizado para o MySQL:
```powershell
dotnet ef migrations script -o mysql_schema.sql --idempotent
```

---

## 3. 🔐 Autenticação & Segurança (JWT + BCrypt)

- **Senhas**: Hasheadas usando `BCrypt.Net-Next` com salt dinâmico.
- **Tokens**: JWT emitido em `Finis.Api/Services/TokenService.cs` com 30 dias de validade contendo claims (`NameIdentifier`, `Name`, `Email`).
- **Angular**: O `JwtInterceptor` em `frontend/src/app/services/jwt.interceptor.ts` injeta o cabeçalho `Authorization: Bearer <token>` em todas as requisições HTTP e desloga o usuário se o servidor retornar 401.

---

## 4. 💻 Frontend Angular - Padrão de Componentes

### Componentes Criados:
1. `NavbarComponent`: Navegação superior, visualizador de saldo inicial e gatilho de novo lançamento.
2. `LoginComponent`: Formulário de autenticação.
3. `RegisterComponent`: Formulário de cadastro com campo de Saldo Inicial.
4. `DashboardComponent`: Cards com KPIs do mês, gráfico vetorial do saldo diário e distribuição de despesas por categoria.
5. `MonthlyFlowComponent`: Grade de 1 a 31 dias simulando a planilha, com expansão de linhas para ver os lançamentos do dia.
6. `EconomyReportComponent`: Relatório consolidado dos 12 meses do ano com a taxa de poupança (aba Economia).
7. `TransactionModalComponent`: Modal rápido e reutilizável para criação e edição de transações.
8. `InitialBalanceModalComponent`: Modal para atualização do saldo inicial de partida.
9. `ResetModalComponent`: Modal de confirmação segura com palavra-chave para zerar todos os dados e o saldo.
10. `CategoriesComponent`: Tela completa de gerenciamento de categorias com cadastro, edição, exclusão, filtros por tipo, paleta de cores e ícones.

### Como compilar o Frontend:
```powershell
cd frontend
npm run build
```

---

## 5. 🔌 Endpoints da API REST

| Método | Rota | Descrição |
| :--- | :--- | :--- |
| `POST` | `/api/auth/register` | Cria novo usuário e define saldo inicial |
| `POST` | `/api/auth/login` | Autentica e retorna token JWT |
| `GET` | `/api/auth/me` | Retorna dados e saldo do usuário logado |
| `PUT` | `/api/auth/initial-balance` | Atualiza o saldo inicial de partida |
| `POST` | `/api/auth/reset-data` | Apaga definitivamente todos os lançamentos e zera o saldo |
| `GET` | `/api/dashboard?year={y}&month={m}` | Retorna KPIs e gráfico do termômetro |
| `GET` | `/api/monthlyflow/{year}/{month}` | Retorna grade diária de 1 a 31 dias |
| `GET` | `/api/economy/{year}` | Retorna os 12 meses consolidados |
| `GET` | `/api/transactions` | Lista lançamentos com filtros opcionais |
| `POST` | `/api/transactions` | Cria novo lançamento financeiro |
| `PUT` | `/api/transactions/{id}` | Atualiza lançamento existente |
| `DELETE`| `/api/transactions/{id}` | Exclui lançamento |
| `GET` | `/api/categories` | Lista categorias de receitas e despesas |
| `POST` | `/api/categories` | Cria nova categoria personalizada |
| `PUT` | `/api/categories/{id}` | Edita categoria personalizada existente |
| `DELETE`| `/api/categories/{id}` | Exclui categoria personalizada |
| `GET` | `/api/creditcards` | Lista cartões do usuário com limite e fatura atual |
| `POST` | `/api/creditcards` | Cadastra novo cartão de crédito |
| `PUT` | `/api/creditcards/{id}` | Atualiza limites ou datas de fechamento/vencimento |
| `DELETE`| `/api/creditcards/{id}` | Desativa cartão de crédito |
| `GET` | `/api/creditcards/{id}/invoices/upcoming` | Retorna projeção das próximas faturas com compras |
| `GET` | `/api/creditcards/{id}/invoices/{year}/{month}` | Detalhes da fatura do mês selecionado |
| `POST` | `/api/creditcards/{id}/invoices/{year}/{month}/pay` | Quita fatura e agenda saída fixa no Termômetro |
