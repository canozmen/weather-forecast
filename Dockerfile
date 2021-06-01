FROM mcr.microsoft.com/dotnet/sdk:5.0.202-alpine3.13-amd64
WORKDIR /app
COPY publish/ .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
# Create a user group 'xyzgroup'
RUN addgroup -S dotnet
# Create a user 'appuser' under 'xyzgroup'
RUN adduser -S -D -h /app dotnet dotnet
# Chown all the files to the app user.
RUN chown -R dotnet:dotnet /app

USER dotnet
ENTRYPOINT ["dotnet","Api.dll"]
HEALTHCHECK --interval=30s --timeout=30s --start-period=5s --retries=3 CMD [ "curl -I http://127.0.0.1:8080/healthz --fail || exit 1" ]