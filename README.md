# FCG.PaymentsAPI

Microsserviço responsável pelo processamento de pagamentos da plataforma **Fiap Cloud Games (FCG)**.

## Responsabilidades

- **Consome** `OrderPlacedEvent` publicado pelo `fcg-catalog-api`
- **Simula** processamento de pagamento via gateway externo (latência de 200ms)
- **Publica** `PaymentProcessedEvent` com status `Approved` ou `Rejected`
- Taxa de aprovação padrão: **90%**

## Fluxo de Eventos

```
CatalogAPI → [OrderPlacedEvent] → PaymentsAPI → [PaymentProcessedEvent] → CatalogAPI + NotificationsAPI
```

## Estrutura

```
fcg-payments-api/
├── src/
│   └── FCG.PaymentsAPI/
│       ├── Consumers/
│       │   └── OrderPlacedConsumer.cs   # Consome pedido, publica resultado
│       ├── Program.cs
│       └── appsettings.json
├── k8s/
│   ├── namespace.yaml
│   ├── configmap.yaml
│   ├── secret.yaml
│   ├── deployment.yaml
│   ├── service.yaml
│   └── mongo.yaml
├── Dockerfile
└── docker-compose.yml
```

## NoSQL (MongoDB)

O serviço utiliza **MongoDB** (banco `fcg_payments`) para persistência de dados de
pagamento e controle de idempotência no processamento de eventos (`OrderPlacedEvent`).

| Variável                       | Padrão                          | Descrição                          |
|---------------------------------|----------------------------------|-------------------------------------|
| `Mongo__ConnectionString`      | `mongodb://mongo:27017`         | String de conexão MongoDB           |
| `Mongo__Database`              | `fcg_payments`                  | Banco de dados usado pelo serviço   |

## Variáveis de Ambiente

| Variável                  | Padrão      | Descrição                         |
|---------------------------|-------------|-----------------------------------|
| `RabbitMQ__Host`          | `localhost` | Host do RabbitMQ                  |
| `RabbitMQ__VirtualHost`   | `/`         | VirtualHost do RabbitMQ           |
| `RabbitMQ__Username`      | `guest`     | Usuário do RabbitMQ               |
| `RabbitMQ__Password`      | `guest`     | Senha do RabbitMQ *(via Secret)*  |
| `Mongo__ConnectionString` | `mongodb://mongo:27017` | String de conexão MongoDB  |
| `Mongo__Database`         | `fcg_payments` | Banco de dados MongoDB           |

## Executar localmente

```bash
# Sobe RabbitMQ + MongoDB + PaymentsAPI
docker compose up -d

# Health check
curl http://localhost:8083/health

# Validar o MongoDB
mongosh mongodb://localhost:27017/fcg_payments --eval "db.getCollectionNames()"
```

## Exemplo de log

```
[PAGAMENTO] 📥 Pedido recebido | OrderId: abc123 | UserId: def456 | Jogo: Elden Ring | Valor: R$ 149,99
[PAGAMENTO] ✅ Pagamento APROVADO | OrderId: abc123 | TransactionId: FA3C9B21... | Valor: R$ 149,99
[PAGAMENTO] 📤 PaymentProcessedEvent publicado | OrderId: abc123 | Status: Approved
```

## Deploy no Kubernetes

```bash
kubectl apply -f k8s/namespace.yaml
kubectl apply -f k8s/secret.yaml
kubectl apply -f k8s/configmap.yaml
kubectl apply -f k8s/mongo.yaml
kubectl apply -f k8s/deployment.yaml
kubectl apply -f k8s/service.yaml
```

## Dependências

- **FCG.Contracts** 1.0.0 (NuGet) — contratos compartilhados de eventos
- **MassTransit.RabbitMQ** 8.2.5
- **Serilog.AspNetCore** 8.0.2

## Grupo 17 — Pos-Tech FIAP
- Letícia Lopes Ribeiro Vasconcelos
- Marcelo Henrique Cornelis Rei
- Washington Santana dos Santos
- Raul Hentz Rodrigues
