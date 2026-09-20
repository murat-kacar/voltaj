# Stage 1: Build the Vite React application
FROM node:24.21.0-alpine3.24 AS build
WORKDIR /app

# Copy dependency definitions
COPY package*.json .npmrc ./
RUN npm ci

# Copy source code and build
COPY . .
ARG VITE_API_BASE_URL=""
ENV VITE_API_BASE_URL=${VITE_API_BASE_URL}
RUN npm run build

# Stage 2: Serve static files with lightweight Nginx Alpine
FROM nginx:1.30.5-alpine3.24 AS final
COPY --from=build /app/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
