# O Banco Digital da Ana - Minimal PoC

Este repositório contém um scaffold inicial com dois microsserviços .NET 8:

- `ContaCorrente.Api`: Gerencia contas, autenticação (JWT), movimentações e saldo.
- `Transferencia.Api`: Realiza transferências entre contas chamando a API `ContaCorrente`.

Características implementadas:
- Dapper + SQLite para persistência (arquivo em `./data/contacorrente.db`).
- Autenticação JWT (todos endpoints sensíveis requerem token).
- Swagger nas APIs com suporte a Bearer token.
- Dockerfile para cada serviço e `docker-compose.yaml` com Kafka (Confluent) para extensão futura.
- Validação de CPF, hash de senha com PBKDF2, idempotência em movimentações.
- Endpoints: register, login, inactivate, movimentacao, saldo.

## Como rodar

### Requisitos
- Docker e Docker Compose.
- .NET 8 SDK (para desenvolvimento/debug local).

### Opção 1: Rodar com Docker (Produção/Deploy)

1. **Inicializar o banco de dados**:
   ```bash
   mkdir -p data
   docker run --rm -v "$(pwd)/data:/data" alpine sh -c "apk add --no-cache sqlite && sqlite3 /data/contacorrente.db < ContaCorrente.Api/sql/contacorrente.sql"
   ```

2. **Subir os serviços**:
   ```bash
   docker-compose up --build -d
   ```

3. **Acessar**:
   - ContaCorrente API: http://localhost:5000/swagger
   - Transferencia API: http://localhost:5001/swagger

### Opção 2: Rodar localmente (Desenvolvimento/Debug)

1. **Configurar o banco**:
   - Certifique-se de que `./data/contacorrente.db` existe (copie de um container ou crie localmente).
   - O `appsettings.Development.json` aponta para `../data/contacorrente.db`.

2. **Executar a API**:
   ```bash
   cd ContaCorrente.Api
   dotnet run
   ```
   - Acesse: http://localhost:5000/swagger

3. **Debugging**:
   - Abra o projeto no VS Code.
   - Coloque breakpoints em `Controllers/ContaController.cs`.
   - Execute `dotnet run` e teste via Swagger ou curl.

## Testando os Endpoints

### 1. Registrar uma conta
```bash
curl -X POST http://localhost:5000/api/conta/register \
  -H "Content-Type: application/json" \
  -d '{"cpf":"12345678901","senha":"senha123","nome":"João Silva"}'
```
- Resposta: `{"numero": 123456}` (número da conta gerado).

### 2. Fazer login
```bash
curl -X POST http://localhost:5000/api/conta/login \
  -H "Content-Type: application/json" \
  -d '{"login":"123456","senha":"senha123"}'
```
- Resposta: `{"token": "eyJ..."}`

### 3. Movimentação (depósito)
```bash
curl -X POST http://localhost:5000/api/conta/movimentacao \
  -H "Authorization: Bearer <TOKEN>" \
  -H "Content-Type: application/json" \
  -d '{"identificacaoRequisicao":"uuid","valor":100.0,"tipo":"C"}'
```
- Resposta: 204 No Content

### 4. Consultar saldo
```bash
curl -X GET http://localhost:5000/api/conta/saldo \
  -H "Authorization: Bearer <TOKEN>"
```
- Resposta: `{"numero":123456,"nome":"João Silva","data":"2026-01-30T...","saldo":"100.00"}`

### 5. Inativar conta
```bash
curl -X POST http://localhost:5000/api/conta/inactivate \
  -H "Authorization: Bearer <TOKEN>" \
  -H "Content-Type: application/json" \
  -d '{"senha":"senha123"}'
```
- Resposta: 204 No Content

## Observações
- As implementações aqui são um ponto de partida: validação de CPF, tratamento de senha (hash/salt) e segurança devem ser reforçados para produção.
- Idempotência, cache, testes automatizados e consumo/produção Kafka são deixados como próximos passos.
- Para debug, use a opção local. Breakpoints não funcionam diretamente em containers sem remote debugging.
