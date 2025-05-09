FROM mcr.microsoft.com/dotnet/aspnet:latest AS runtime
LABEL org.opencontainers.image.licenses="Apache-2.0" \
      org.opencontainers.image.version="1.0.0"

ARG APP_UID=1000
ARG APP_GID=1000
ENV APP_UID=${APP_UID}

RUN groupadd -g ${APP_GID} appgroup && \
    useradd -u ${APP_UID} -g appgroup -d /app -s /bin/false appuser

WORKDIR /app
RUN chown -R ${APP_UID}:${APP_GID} /app

COPY --chown=${APP_UID}:${APP_GID} ./publish/ .

USER ${APP_UID}:${APP_GID}

ENTRYPOINT ["dotnet", "Account.Subscriber.dll"]