# build node in diff container
FROM node:22 AS npm-build
WORKDIR /App

# pull the js dependencies out into a different layer
COPY package*.json ./
RUN npm install

# build ts frontend in a different layer
COPY src/ tsconfig.json webpack.config.js ./
RUN npm run build

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /App

# pull csharp deps into a different layer
COPY honooru.csproj ./
RUN dotnet restore

COPY . ./
RUN dotnet build -o out

# put everything together into the runtime container!
FROM mcr.microsoft.com/dotnet/aspnet:8.0
RUN mkdir -p /plap
RUN apt update && apt install ffmpeg yt-dlp -y
WORKDIR /App
COPY --from=build-env /App/out .
COPY --from=npm-build /App/wwwroot ./wwwroot
EXPOSE 8080
ENTRYPOINT [ "dotnet", "honooru.dll" ]
