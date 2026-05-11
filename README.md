# 💰 FinanceSimplify

[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE.txt)
[![.NET](https://img.shields.io/badge/.NET-8.0-purple.svg)](https://dotnet.microsoft.com/)
[![MongoDB](https://img.shields.io/badge/MongoDB-Database-green.svg)](https://www.mongodb.com/)

> Uma API robusta e escalável para gerenciamento simplificado de finanças pessoais. Organize suas contas bancárias, cartões de crédito, faturas, transações e categorias em um único lugar.

## 📋 Sobre o Projeto

**FinanceSimplify** é um backend desenvolvido em **ASP.NET Core 8.0** que fornece uma solução completa para gerenciamento de finanças pessoais. Com recursos como autenticação JWT, importação de transações via CSV e gerenciamento de múltiplas contas bancárias e cartões, a aplicação oferece uma base sólida para aplicações frontend modernas.

### ✨ Recursos Principais

- 🔐 **Autenticação e Autorização** - Segurança com JWT Bearer
- 🏦 **Gerenciamento de Contas Bancárias** - Múltiplas contas com rastreamento
- 💳 **Cartões de Crédito** - Cadastro e gerenciamento de cartões com categorias de transação
- 📝 **Faturas** - Acompanhamento de faturas com histórico
- 💸 **Transações** - Registro detalhado com categorização e filtros avançados
- 📊 **Parcelamento** - Suporte para transações em parcelas
- 📁 **Importação de CSV** - Importação em massa de transações
- 🏷️ **Categorias** - Organização de transações por categoria
- 📖 **Swagger/OpenAPI** - Documentação interativa da API

## 🚀 Tecnologias Utilizadas

| Tecnologia | Versão | Propósito |
|-----------|--------|----------|
| .NET | 8.0 | Framework principal |
| MongoDB | 2.30.0 | Banco de dados NoSQL |
| ASP.NET Core | 8.0 | Web framework |
| JWT Bearer | 8.0.13 | Autenticação |
| Swagger/OpenAPI | 7.2.0 | Documentação da API |

## 📦 Pré-requisitos

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) ou superior
- [MongoDB](https://www.mongodb.com/try/download/community) 4.0+ (ou acesso a uma instância remota)
- [Git](https://git-scm.com/)

## 🔧 Instalação e Configuração

### 1. Clonar o Repositório

```bash
git clone https://github.com/seu-usuario/FinanceSimplify.git
cd FinanceSimplify/Back-end/FinanceSimplify
```

### 2. Configurar Variáveis de Ambiente

Crie ou edite o arquivo `appsettings.Development.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AppSettings": {
    "Token": "sua-chave-secreta-jwt-aqui"
  },
  "MongoDbSettings": {
    "ConnectionString": "mongodb://localhost:27017",
    "DatabaseName": "FinanceSimplify"
  },
  "AllowedHosts": "*"
}
```

**Variáveis Obrigatórias:**
- `AppSettings:Token` - Chave secreta para geração de JWT (mínimo 32 caracteres recomendado)
- `MongoDbSettings:ConnectionString` - String de conexão do MongoDB
- `MongoDbSettings:DatabaseName` - Nome do banco de dados (padrão: FinanceSimplify)

### 3. Restaurar Dependências

```bash
dotnet restore
```

### 4. Executar Migrações (se aplicável)

```bash
dotnet ef database update
```

### 5. Iniciar a Aplicação

```bash
dotnet run
```

A API estará disponível em `https://localhost:5001` ou `http://localhost:5000`

## 🌐 Endpoints da API

### Autenticação
- `POST /api/auth/register` - Registrar novo usuário
- `POST /api/auth/login` - Fazer login e obter JWT token

### Contas Bancárias
- `GET /api/bankaccount` - Listar todas as contas
- `POST /api/bankaccount` - Criar nova conta
- `PUT /api/bankaccount/{id}` - Atualizar conta
- `DELETE /api/bankaccount/{id}` - Deletar conta

### Cartões de Crédito
- `GET /api/card` - Listar todos os cartões
- `POST /api/card` - Criar novo cartão
- `PUT /api/card/{id}` - Atualizar cartão
- `DELETE /api/card/{id}` - Deletar cartão

### Transações
- `GET /api/transaction` - Listar transações com filtros
- `POST /api/transaction` - Criar nova transação
- `PUT /api/transaction/{id}` - Atualizar transação
- `DELETE /api/transaction/{id}` - Deletar transação
- `POST /api/transaction/import-csv` - Importar transações via CSV

### Categorias
- `GET /api/category` - Listar categorias
- `POST /api/category` - Criar categoria
- `PUT /api/category/{id}` - Atualizar categoria
- `DELETE /api/category/{id}` - Deletar categoria

### Faturas
- `GET /api/invoice` - Listar faturas
- `GET /api/invoice/{id}` - Detalhes da fatura

### Parcelamentos
- `GET /api/installment` - Listar parcelamentos

## 📂 Estrutura do Projeto

```
FinanceSimplify/
├── Controllers/           # Controladores da API
│   ├── AuthController.cs
│   ├── BankAccountController.cs
│   ├── CardController.cs
│   ├── CategoryController.cs
│   ├── InvoiceController.cs
│   └── TransactionController.cs
├── Services/             # Lógica de negócio
│   ├── AuthService/
│   ├── BankAccountService/
│   ├── CardService/
│   ├── CategoryService/
│   ├── InvoiceService/
│   ├── TransactionService/
│   ├── InstallmentService/
│   └── PasswordService/
├── Models/               # Modelos de domínio
│   ├── BankAccount/
│   ├── Card/
│   ├── Category/
│   ├── Invoice/
│   ├── Transaction/
│   ├── Installment/
│   └── User/
├── Dtos/                 # Data Transfer Objects
│   ├── BankAccount/
│   ├── Card/
│   ├── Category/
│   ├── Invoice/
│   ├── Transactions/
│   ├── Installment/
│   └── User/
├── Data/                 # Acesso a dados
│   ├── MongoDbContext.cs
│   └── MongoDbSettings.cs
├── Enum/                 # Enumerações
│   ├── TypeCardTransactionEnum.cs
│   ├── TypePaymentMethodEnum.cs
│   └── TypeTransactionEnum.cs
└── Program.cs            # Configuração da aplicação
```

## 🔒 Segurança

### Autenticação JWT
A API utiliza **JWT (JSON Web Tokens)** para autenticação. Para acessar endpoints protegidos:

1. Registre ou faça login para obter um token
2. Inclua o token no header `Authorization`: `Bearer {seu_token}`

### Exemplo com cURL:
```bash
curl -H "Authorization: Bearer seu_token_aqui" \
  https://localhost:5001/api/bankaccount
```

### Exemplo com JavaScript/Fetch:
```javascript
const headers = {
  'Authorization': `Bearer ${token}`,
  'Content-Type': 'application/json'
};

fetch('https://localhost:5001/api/bankaccount', { headers })
  .then(res => res.json())
  .then(data => console.log(data));
```

## 📊 Importação de Transações via CSV

O projeto suporta importação em massa de transações através de arquivos CSV:

**Formato esperado:**
```csv
date,description,amount,category,paymentMethod
2024-01-15,Compras no mercado,150.50,Alimentação,Cartão de Crédito
2024-01-16,Conta de luz,89.90,Utilidades,Débito
```

**Endpoint:**
```bash
POST /api/transaction/import-csv
Content-Type: multipart/form-data

[arquivo.csv]
```

## 🐳 Docker

### Build da Imagem
```bash
docker build -t finance-simplify .
```

### Executar Container
```bash
docker run -e MongoDbSettings__ConnectionString="mongodb://mongo:27017" \
           -e AppSettings__Token="sua-chave-secreta" \
           -p 5000:8080 \
           finance-simplify
```

## 🚢 Deployment

### Railway
O projeto inclui configuração para deploy no [Railway](https://railway.app/):

```bash
# Ver arquivo railway.toml para configurações específicas
cat railway.toml
```

**Variáveis de ambiente no Railway:**
- `ASPNETCORE_ENVIRONMENT` = Production
- `MongoDbSettings__ConnectionString` = Sua string de conexão MongoDB
- `AppSettings__Token` = Sua chave JWT

## 📖 Documentação Interativa

Após iniciar a aplicação, acesse a documentação Swagger em:

```
https://localhost:5001/swagger
```

Aqui você pode:
- Explorar todos os endpoints
- Testar requisições
- Visualizar modelos de resposta
- Autenticar com JWT

## 🤝 Contribuindo

Contribuições são bem-vindas! Por favor:

1. Faça um Fork do projeto
2. Crie uma branch para sua feature (`git checkout -b feature/AmazingFeature`)
3. Commit suas mudanças (`git commit -m 'Add some AmazingFeature'`)
4. Push para a branch (`git push origin feature/AmazingFeature`)
5. Abra um Pull Request

## 🐛 Reportar Bugs

Encontrou um bug? Por favor, abra uma [issue](https://github.com/seu-usuario/FinanceSimplify/issues) com:
- Descrição clara do problema
- Passos para reproduzir
- Comportamento esperado
- Seu ambiente (.NET version, OS, etc)

## 📝 Licença

Este projeto está licenciado sob a Licença MIT - veja o arquivo [LICENSE.txt](LICENSE.txt) para detalhes.

## 👨‍💻 Autor

Desenvolvido com ❤️ para simplificar o gerenciamento de finanças pessoais.

## 📞 Suporte

Para suporte, abra uma issue ou entre em contato através do repositório.

---

**Last Updated:** Maio 2024  
**Version:** 1.0.0