# 🚀 Finis - Gestão Financeira & Termômetro de Fluxo de Caixa

Sistema de gestão financeira pessoal e empresarial composto por:
- **Frontend**: Single Page Application em Angular 16 servida via Nginx (Container Docker).
- **Backend**: Web API em .NET 9 (C#) com Entity Framework Core e autenticação JWT (Container Docker).
- **Banco de Dados**: MySQL 8.0 rodando diretamente na sua máquina local (Host Windows).

---

## 🐳 Executando com Docker + MySQL Local

O backend e o frontend rodam dentro do Docker, comunicando-se com o MySQL instalado diretamente no seu Windows através de `host.docker.internal`.

### 1. Iniciar a aplicação
Abra o terminal (PowerShell ou CMD) na pasta raiz do projeto e execute:
```powershell
docker compose up -d --build
```

### 2. Acessar os serviços
- **Frontend (Aplicação Web)**: [http://localhost:4201](http://localhost:4201)
- **Backend API**: [http://localhost:5000](http://localhost:5000)
- **Banco MySQL**: Seu MySQL local do Windows (`localhost:3306`)

---

## ⚙️ Configuração Importante do MySQL Local

Como o Backend está dentro de um container Docker, ele acessa o seu Windows através da rede virtual do Docker. Para garantir a conexão:

1. **Permissão de Usuário no MySQL:**
   Certifique-se de que o usuário (`root` ou o que você definir no `.env`) possa receber conexões de qualquer host (`%`). Se necessário, execute no seu MySQL (Workbench, DBeaver ou CLI):
   ```sql
   CREATE DATABASE IF NOT EXISTS finis_db;
   CREATE USER IF NOT EXISTS 'root'@'%' IDENTIFIED BY '@vs130986';
   GRANT ALL PRIVILEGES ON finis_db.* TO 'root'@'%';
   FLUSH PRIVILEGES;
   ```

2. **Arquivo `.env`:**
   As credenciais do seu MySQL local e as portas podem ser ajustadas no arquivo `.env`:
   ```env
   DB_HOST=host.docker.internal
   DB_PORT=3306
   DB_NAME=finis_db
   DB_USER=root
   DB_PASSWORD=@vs130986

   FRONTEND_PORT=4201
   BACKEND_PORT=5000
   ```

---

## 🔄 Como garantir que rode sempre automaticamente:
1. Abra o **Docker Desktop**.
2. Clique no ícone de engrenagem (**Settings** no canto superior direito).
3. Na aba **General**, marque a opção:
   - ✅ **Start Docker Desktop when you log in**
4. Os containers estão com a diretiva `restart: unless-stopped`, iniciando automaticamente em segundo plano ao ligar o computador.
