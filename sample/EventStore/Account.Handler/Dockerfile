FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY Published/ ./
ENTRYPOINT ["dotnet", "Account.Handler.dll"]