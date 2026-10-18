FROM node:22-alpine AS dependencias
WORKDIR /app
COPY package.json package-lock.json ./
RUN npm ci

FROM node:22-alpine AS build
WORKDIR /app
ENV NEXT_TELEMETRY_DISABLED=1
COPY --from=dependencias /app/node_modules ./node_modules
COPY . .
RUN npm run build

FROM node:22-alpine AS execucao
WORKDIR /app
ENV NODE_ENV=production \
    NEXT_TELEMETRY_DISABLED=1 \
    PORT=3000 \
    HOSTNAME=0.0.0.0
RUN addgroup -S oficina && adduser -S oficina -G oficina
COPY --from=build --chown=oficina:oficina /app/.next/standalone ./
COPY --from=build --chown=oficina:oficina /app/.next/static ./.next/static
USER oficina
EXPOSE 3000
CMD ["node", "server.js"]
