FROM mcr.microsoft.com/dotnet/aspnet:latest AS runtime
WORKDIR /app

LABEL org.opencontainers.image.licenses="Apache-2.0" \
      org.opencontainers.image.version="1.0.0"

RUN groupadd -g 1000 appuser && \
    useradd -u 1000 -g appuser -d /app -s /bin/false appuser && \
    chown -R appuser:appuser /app
USER appuser:appuser

COPY --chown=appuser:appuse ./publish .

ENTRYPOINT ["dotnet", "Account.Sender.dll"]