# 🚀 Guia Completo de Deploy no Railway

Este guia vai te ajudar a fazer o deploy da sua API FinanceSimplify no Railway.app gratuitamente.

---

## 📋 Pré-requisitos

- ✅ Conta no GitHub
- ✅ Repositório GitHub com o código da API
- ✅ MongoDB Atlas configurado (você já tem!)

---

## 🎯 Passo a Passo

### **1. Criar Conta no Railway**

1. Acesse [railway.app](https://railway.app)
2. Clique em **"Login"** ou **"Start a New Project"**
3. Faça login com sua conta do **GitHub**
4. Autorize o Railway a acessar seus repositórios

> [!NOTE]
> Você receberá **$5 de crédito gratuito por mês**, suficiente para hospedar sua API!

---

### **2. Fazer Push dos Arquivos de Deploy**

Antes de criar o projeto no Railway, você precisa enviar os arquivos `Dockerfile` e `.dockerignore` para o GitHub:

```bash
# Navegue até a pasta do projeto
cd "f:\FinanceSimplify\Back-end\FinanceSimplify\FinanceSimplify"

# Adicione os novos arquivos
git add Dockerfile .dockerignore

# Commit
git commit -m "Add Railway deployment configuration"

# Push para o GitHub
git push origin main
```

> [!TIP]
> Se sua branch principal for `master` ao invés de `main`, use `git push origin master`

---

### **3. Criar Projeto no Railway**

1. No Railway Dashboard, clique em **"New Project"**
2. Selecione **"Deploy from GitHub repo"**
3. Escolha o repositório **`FinanceSimplify-Back-end`** (ou o nome do seu repo)
4. Selecione a pasta correta: `Back-end/FinanceSimplify/FinanceSimplify`

> [!IMPORTANT]
> O Railway detectará automaticamente o `Dockerfile` e usará ele para o build!

---

### **4. Configurar Variáveis de Ambiente**

Após criar o projeto, você precisa configurar as variáveis de ambiente:

1. No Railway Dashboard, clique no seu projeto
2. Vá em **"Variables"** (aba lateral)
3. Adicione as seguintes variáveis:

#### **Variáveis Obrigatórias:**

| Variável | Valor | Descrição |
|----------|-------|-----------|
| `MongoDbSettings__ConnectionString` | `mongodb+srv://luis93667:458866@finance-simplify.5urmjcp.mongodb.net/?appName=finance-simplify` | String de conexão do MongoDB Atlas |
| `MongoDbSettings__DatabaseName` | `FinanceSimplify` | Nome do banco de dados |
| `AppSettings__Token` | `$2b$12$z6GJbFJtX8YqNfEjcB9lVdJw1Finance$$SimplifytF5x8r3DhKU8KpGZ0RXG9Pf9Lgxi` | Token JWT para autenticação |
| `ASPNETCORE_ENVIRONMENT` | `Production` | Ambiente de execução |

> [!WARNING]
> **IMPORTANTE**: No Railway, use `__` (dois underscores) para representar níveis aninhados no JSON. Por exemplo, `MongoDbSettings:ConnectionString` vira `MongoDbSettings__ConnectionString`.

#### **Como Adicionar:**

1. Clique em **"New Variable"**
2. Cole o **nome** da variável (ex: `MongoDbSettings__ConnectionString`)
3. Cole o **valor** correspondente
4. Clique em **"Add"**
5. Repita para todas as variáveis

---

### **5. Deploy Automático**

Após configurar as variáveis:

1. O Railway iniciará o **build automaticamente**
2. Você verá os logs do build em tempo real
3. Aguarde até ver **"Build successful"** ✅
4. Em seguida, o deploy será feito automaticamente

⏱️ **Tempo estimado**: 3-5 minutos

---

### **6. Obter URL da API**

1. Após o deploy bem-sucedido, vá em **"Settings"**
2. Role até a seção **"Networking"**
3. Clique em **"Generate Domain"**
4. O Railway criará uma URL pública, exemplo:
   ```
   https://financesimplify-production.up.railway.app
   ```

> [!TIP]
> Você pode customizar o domínio depois se quiser!

---

### **7. Testar a API**

Acesse sua API no navegador:

```
https://SEU-DOMINIO.railway.app/swagger
```

Você deve ver a interface do **Swagger UI** com todos os endpoints da API! 🎉

#### **Teste Rápido:**

1. Abra o Swagger
2. Teste o endpoint de **registro** ou **login**
3. Verifique se a conexão com MongoDB está funcionando

---

## 🔍 Monitoramento e Logs

### **Ver Logs em Tempo Real:**

1. No Railway Dashboard, clique no seu projeto
2. Vá na aba **"Deployments"**
3. Clique no deployment ativo
4. Você verá os logs da aplicação em tempo real

### **Métricas:**

- **CPU Usage**: Uso de processador
- **Memory**: Uso de memória RAM
- **Network**: Tráfego de rede

---

## 🔧 Troubleshooting

### **Problema: Build falhou**

**Solução:**
- Verifique os logs do build
- Certifique-se que o `Dockerfile` está na raiz do projeto
- Verifique se todas as dependências estão no `.csproj`

### **Problema: API não conecta ao MongoDB**

**Solução:**
- Verifique se as variáveis de ambiente estão corretas
- Confirme que usou `__` (dois underscores) nos nomes
- Teste a connection string localmente primeiro

### **Problema: Erro 502 Bad Gateway**

**Solução:**
- Verifique se a aplicação está escutando na porta correta (8080)
- Veja os logs para identificar erros de inicialização
- Confirme que `ASPNETCORE_URLS=http://+:8080` está configurado

### **Problema: Aplicação reinicia constantemente**

**Solução:**
- Verifique os logs para ver o erro
- Pode ser problema de memória (limite do plano gratuito)
- Verifique se todas as variáveis de ambiente estão configuradas

---

## 🔄 Deploy Automático (CI/CD)

O Railway está configurado para **deploy automático**! 🚀

Sempre que você fizer `git push` para a branch `main`:
1. Railway detecta a mudança
2. Faz build automaticamente
3. Deploy da nova versão
4. Zero downtime!

---

## 💰 Gerenciamento de Créditos

### **Ver Uso:**
1. Clique no seu avatar (canto superior direito)
2. Vá em **"Account Settings"**
3. Veja **"Usage"** para acompanhar seus créditos

### **Dicas para Economizar:**
- ✅ Use sleep mode para ambientes de desenvolvimento
- ✅ Monitore o uso de memória
- ✅ Otimize queries do MongoDB para reduzir processamento

---

## 📚 Próximos Passos

Após o deploy bem-sucedido:

1. ✅ **Atualize o frontend** com a nova URL da API
2. ✅ **Configure CORS** se necessário (já está configurado no `Program.cs`)
3. ✅ **Teste todos os endpoints** via Swagger
4. ✅ **Configure domínio customizado** (opcional)
5. ✅ **Configure monitoramento** de erros (opcional: Sentry, Application Insights)

---

## 🆘 Suporte

- **Documentação Railway**: [docs.railway.app](https://docs.railway.app)
- **Discord Railway**: [discord.gg/railway](https://discord.gg/railway)
- **Status Railway**: [status.railway.app](https://status.railway.app)

---

## ✅ Checklist Final

- [ ] Conta Railway criada
- [ ] Arquivos `Dockerfile` e `.dockerignore` no repositório
- [ ] Push feito para o GitHub
- [ ] Projeto criado no Railway
- [ ] Variáveis de ambiente configuradas
- [ ] Build concluído com sucesso
- [ ] Deploy realizado
- [ ] Domínio gerado
- [ ] Swagger acessível
- [ ] Endpoints testados
- [ ] Frontend atualizado com nova URL

---

🎉 **Parabéns! Sua API está no ar!** 🎉
