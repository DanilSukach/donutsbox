# Multi-stage build для Angular приложения с SSR

# Stage 1: Build
FROM node:20-alpine AS builder

WORKDIR /app

# Копируем package files
COPY package*.json ./

# Устанавливаем зависимости
RUN npm ci

# Копируем исходный код
COPY . .

# Собираем приложение для production
RUN npm run build

# Stage 2: Production
FROM node:20-alpine AS runner

WORKDIR /app

# Устанавливаем только production зависимости для SSR
COPY package*.json ./
RUN npm ci --only=production && npm cache clean --force

# Копируем собранное приложение из builder stage
COPY --from=builder /app/dist/DonutsboxClient ./dist

# Создаем пользователя для безопасности
RUN addgroup -g 1001 -S nodejs && \
    adduser -S angular -u 1001

# Меняем владельца файлов
RUN chown -R angular:nodejs /app

USER angular

# Открываем порт
EXPOSE 4000

# Переменная окружения для порта
ENV PORT=4000
ENV NODE_ENV=production

# Запускаем SSR сервер
CMD ["node", "dist/server/server.mjs"]

