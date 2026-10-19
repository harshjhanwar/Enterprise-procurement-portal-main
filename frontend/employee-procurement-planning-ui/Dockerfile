FROM node:22-alpine AS build
WORKDIR /app

COPY package.json ./
RUN npm install --no-audit --no-fund

COPY . .
RUN npm run build -- --configuration production

FROM nginx:1.27-alpine
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist/employee-procurement-planning-ui/browser /usr/share/nginx/html
COPY docker-entrypoint.d/40-epp-config.sh /docker-entrypoint.d/40-epp-config.sh
RUN chmod +x /docker-entrypoint.d/40-epp-config.sh

EXPOSE 80
