# 🔐 Configuração de Variáveis de Ambiente

## 📋 Para Desenvolvimento Local

### **Opção 1: Usar appsettings.Development.json (Recomendado)**

O arquivo `appsettings.Development.json` já contém suas credenciais para desenvolvimento local. 

> [!WARNING]
> **NUNCA faça commit do `appsettings.Development.json` para o GitHub!** Verifique se ele está no `.gitignore`.

### **Opção 2: Usar Variáveis de Ambiente**

Se preferir usar variáveis de ambiente localmente:

1. Copie o arquivo `.env.example` para `.env`:
   ```bash
   cp .env.example .env
   ```

2. Edite o `.env` e adicione suas credenciais reais

3. Use uma extensão como `DotNetEnv` ou configure no Visual Studio

---

## 🚀 Para Produção (Railway)

No Railway, configure as seguintes variáveis de ambiente:

### **Variáveis Obrigatórias:**

```bash
# MongoDB
MongoDbSettings__ConnectionString=mongodb+srv://luis93667:458866@finance-simplify.5urmjcp.mongodb.net/?appName=finance-simplify
MongoDbSettings__DatabaseName=FinanceSimplify

# JWT Token
AppSettings__Token=$2b$12$z6GJbFJtX8YqNfEjcB9lVdJw1Finance$$SimplifytF5x8r3DhKU8KpGZ0RXG9Pf9Lgxi

# ASP.NET Core
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:8080
```

> [!IMPORTANT]
> Use `__` (dois underscores) para separar níveis aninhados no JSON quando configurar no Railway!

---

## 🔍 Como Funciona

O ASP.NET Core lê configurações nesta ordem de prioridade:

1. **Variáveis de Ambiente** (maior prioridade)
2. `appsettings.{Environment}.json`
3. `appsettings.json` (menor prioridade)

No Railway, as variáveis de ambiente **sobrescrevem** os valores vazios do `appsettings.json`.

---

## ✅ Verificação

### **Desenvolvimento Local:**
```bash
dotnet run
```
Deve conectar ao MongoDB usando `appsettings.Development.json`

### **Produção (Railway):**
As variáveis de ambiente configuradas no Railway Dashboard serão usadas automaticamente.

---

## 🔒 Segurança

✅ **Boas Práticas Implementadas:**
- Credenciais removidas do `appsettings.json` (arquivo versionado)
- Credenciais em `appsettings.Development.json` (deve estar no .gitignore)
- Produção usa variáveis de ambiente do Railway
- `.env.example` documenta variáveis necessárias sem expor valores reais
