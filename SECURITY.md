# 🔒 Guia de Segurança - FinanceSimplify

## ⚠️ NUNCA EXPONHA CREDENCIAIS

Este documento descreve as práticas essenciais de segurança para o projeto FinanceSimplify.

---

## 1. Configuração Segura de Variáveis de Ambiente

### Arquivos Protegidos

Os seguintes arquivos **NUNCA devem ser commitados** com dados sensíveis:

- ❌ `appsettings.Development.json`
- ❌ `appsettings.Production.json`
- ❌ `appsettings.Staging.json`
- ❌ `.env`
- ❌ `.env.local`

Estes arquivos estão no `.gitignore` e não serão commitados automaticamente.

### Arquivo de Exemplo

Use o arquivo de exemplo como referência:

```bash
cp appsettings.Development.json.example appsettings.Development.json
# Edite o arquivo e preencha suas credenciais locais
```

---

## 2. Geração de JWT Secret

Para gerar uma chave JWT segura:

### Windows PowerShell:
```powershell
$secret = [Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes((New-Guid).ToString() + (New-Guid).ToString()))
Write-Host $secret
```

### Linux/macOS (bash):
```bash
openssl rand -base64 32
```

### Online (teste apenas):
Use https://www.base64encode.org/ (⚠️ NUNCA para produção)

---

## 3. MongoDB Connection String

### Formato Seguro:
```
mongodb+srv://username:password@cluster.mongodb.net/database?retryWrites=true&w=majority
```

### ⚠️ CUIDADO:

- **NUNCA** adicione a string de conexão com senha ao repositório
- **NUNCA** use senhas simples ou sequências numéricas
- **Sempre** use variáveis de ambiente ou User Secrets em desenvolvimento

### User Secrets (Recomendado para Desenvolvimento Local):

```bash
# Inicializar User Secrets
dotnet user-secrets init

# Adicionar valores
dotnet user-secrets set "MongoDbSettings:ConnectionString" "sua-connection-string"
dotnet user-secrets set "AppSettings:Token" "sua-chave-jwt"

# Listar valores
dotnet user-secrets list

# Remover um valor
dotnet user-secrets remove "MongoDbSettings:ConnectionString"
```

---

## 4. Variáveis de Ambiente por Ambiente

### Desenvolvimento Local
```json
{
  "AppSettings": {
    "Token": "sua-chave-local-32-chars-aqui"
  },
  "MongoDbSettings": {
    "ConnectionString": "mongodb://localhost:27017",
    "DatabaseName": "FinanceSimplify"
  }
}
```

### Staging/Produção
```bash
# Defina no painel da plataforma de deployment (Railway, Azure, etc)
ASPNETCORE_ENVIRONMENT=Production
AppSettings__Token=SUA_CHAVE_SEGURA_PRODUÇÃO
MongoDbSettings__ConnectionString=mongodb+srv://user:pass@prod-cluster.mongodb.net/
```

---

## 5. Detectando Exposição de Credenciais

### Se você acidentalmente commitou credenciais:

1. **REVOGUE IMEDIATAMENTE** as credenciais comprometidas
2. **Remova do histórico Git**:
   ```bash
   # Remove o arquivo do histórico
   git filter-branch --tree-filter 'rm -f FinanceSimplify/appsettings.Development.json' HEAD
   
   # Force push (cuidado!)
   git push origin --force
   ```

3. **Crie novas credenciais** em seus serviços

---

## 6. Checklist de Segurança

Antes de cada commit:

- [ ] Nenhum arquivo `appsettings.*.json` foi modificado com credenciais
- [ ] Nenhuma chave JWT está visível no código
- [ ] Nenhuma string de conexão MongoDB está visível
- [ ] Nenhuma senha hardcoded
- [ ] Arquivos sensíveis estão no `.gitignore`

---

## 7. Recursos Adicionais

- [OWASP Top 10](https://owasp.org/www-project-top-ten/)
- [Git Secrets](https://github.com/awslabs/git-secrets)
- [Detect Secrets](https://github.com/Yelp/detect-secrets)
- [.NET Configuration Secrets](https://docs.microsoft.com/en-us/aspnet/core/security/app-secrets)

---

## 8. Relatório de Vulnerabilidade

Se encontrar uma vulnerabilidade de segurança:

1. **NÃO** abra uma issue pública
2. Entre em contato privadamente com o mantenedor
3. Espere por confirmação antes de divulgar

---

**Última atualização:** Maio 2026  
**Versão:** 1.0
