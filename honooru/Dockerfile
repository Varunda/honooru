FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
RUN apt update && apt install npm -y
WORKDIR /App

# pull the js dependencies out into a different layer
COPY package*.json ./
RUN npm install

# pull csharp deps into a different layer
COPY honooru.csproj ./
RUN dotnet restore

# build ts frontend in a different layer
COPY src/ tsconfig.json webpack.config.js ./
RUN npm run build

COPY . ./
RUN dotnet build -o out

FROM mcr.microsoft.com/dotnet/aspnet:8.0
RUN mkdir -p /plap
RUN apt update && apt install ffmpeg yt-dlp -y
WORKDIR /App
COPY --from=build-env /App/out .
COPY --from=build-env /App/wwwroot ./wwwroot
EXPOSE 8080
ENTRYPOINT [ "dotnet", "honooru.dll" ]
