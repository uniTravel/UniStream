FROM mcr.microsoft.com/dotnet/aspnet:latest AS runtime
WORKDIR /app
COPY ./publish .

ENTRYPOINT ["dotnet", "Account.Projector.dll"]