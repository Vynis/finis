# -------------------------------------------------------------
# 1. Build Stage (Node.js 18)
# -------------------------------------------------------------
FROM node:18-alpine AS build
WORKDIR /app

# Instalação das dependências com cache eficiente
COPY package*.json ./
RUN npm ci || npm install

# Cópia do código-fonte e build de produção do Angular
COPY . .
RUN npm run build -- --configuration production

# -------------------------------------------------------------
# 2. Production Stage (Nginx Alpine)
# -------------------------------------------------------------
FROM nginx:alpine

# Copia os arquivos compilados e a configuração do proxy Nginx
COPY --from=build /app/dist/frontend /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]
