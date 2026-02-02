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
![Executar o Conteiner](images/docker.jpg)

2. **Subir os serviços**:
   ```bash
   docker-compose up --build -d
   ```

3. **Acessar**:
   - ContaCorrente API: https://localhost:62723/swagger/index.html
![Swagger](images/swagger.jpg)

   - Transferencia API: https://localhost:62724/swagger/index.html
![Swagger](images/endpoint8.jpg)

### Opção 2: Rodar localmente (Desenvolvimento/Debug)

1. **Configurar o banco**:
   - Certifique-se de que `./data/contacorrente.db` .
   - O `appsettings.Development.json` aponta para `../data/contacorrente.db`.

![Banco de dados](images/BancodeDados-SqLite.jpg)
![Tabelas](images/BancodeDados-SqLite2.jpg)

2. **Executar a API**:
   ```bash
   cd ContaCorrente.Api
   dotnet run
   ```
   - Acesse: http://localhost:62723/swagger

3. **Debugging**:
   - Abra o projeto no VS Code.
   - Coloque breakpoints em `Controllers/ContaController.cs`.
   - Execute `dotnet run` e teste via Swagger ou curl.

## Testando os Endpoints

### 1. Registrar uma conta
```bash
curl -X POST http://localhost:62723/api/conta/register \
  -H "Content-Type: application/json" \
  -d '{"cpf":"12345678901","senha":"senha123","nome":"João Silva"}'
```
- Resposta: `{"numero": 123456}` (número da conta gerado).
![Swagger](images/endpoint1.jpg)

### 2. Fazer login
```bash
curl -X POST http://localhost:62723/api/conta/login \
  -H "Content-Type: application/json" \
  -d '{"login":"123456","senha":"senha123"}'
```
- Resposta: `{"token": "eyJ..."}`

Observação: Os passos 3 , 4 e 5 devem possuir autorização
![Swagger](images/swagger2.jpg)

![Swagger](images/endpoint2.jpg)

### 3. Movimentação (depósito)
```bash
curl -X POST http://localhost:62723/api/conta/movimentacao \
  -H "Authorization: Bearer <TOKEN>" \
  -H "Content-Type: application/json" \
  -d '{"identificacaoRequisicao":"uuid","valor":100.0,"tipo":"C"}'
```
- Resposta: 200
![Swagger](images/endpoint3.jpg)
### 4. Consultar saldo
```bash
curl -X GET http://localhost:62723/api/conta/saldo \
  -H "Authorization: Bearer <TOKEN>"
```
- Resposta: `{"numero":123456,"nome":"João Silva","data":"2026-01-30T...","saldo":"100.00"}`
![Swagger](images/endpoint4.jpg)
### 5. Inativar conta
```bash
curl -X POST http://localhost:62723/api/conta/inactivate \
  -H "Authorization: Bearer <TOKEN>" \
  -H "Content-Type: application/json" \
  -d '{"senha":"senha123"}'
```
- Resposta: 400 
![Swagger](images/endpoint5.jpg)

- Resposta: 200
![Swagger](images/endpoint6.jpg)

### 6. Transferencia
```bash
curl -X 'POST' \
  'https://localhost:62724/api/Transferencia' \
  -H 'accept: */*' \
  -H 'Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYW1laWQiOiJiZjExZTM2MS0zMDc1LTRmZTMtYWQ5OS0wYTlhZTQyYWQzODEiLCJhY2NvdW50TnVtYmVyIjoiMTI3NTI2IiwibmJmIjoxNzY5OTk5MjA2LCJleHAiOjE3NzAwMDY0MDYsImlhdCI6MTc2OTk5OTIwNn0.4vmW_MgDfSaHUwZAAk8ZDaauN_oamtWkT2sKJm7LYgo' \
  -H 'Content-Type: application/json' \
  -d '{
  "identificacaoRequisicao": "Transferencia",
  "contaOrigem": "127526",
  "contaDestino": "245587",
  "valor": 50
}'
```
- Resposta: 200
![Swagger](images/endpoint8.jpg)

### Testes Integrados
![Testes](images/Testes.jpg)


