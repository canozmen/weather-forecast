#FROM mcr.microsoft.com/dotnet/sdk:5.0.202-alpine3.13-amd64
FROM debian:stretch-slim
RUN apt update && apt install wget apt-transport-https gnupg -y
RUN wget -O - https://packages.microsoft.com/keys/microsoft.asc | gpg --dearmor > microsoft.asc.gpg
RUN mv microsoft.asc.gpg /etc/apt/trusted.gpg.d/
RUN wget https://packages.microsoft.com/config/debian/9/prod.list
RUN mv prod.list /etc/apt/sources.list.d/microsoft-prod.list
RUN chown root:root /etc/apt/trusted.gpg.d/microsoft.asc.gpg
RUN chown root:root /etc/apt/sources.list.d/microsoft-prod.list
RUN apt-get update; \
    apt-get install -y apt-transport-https && \
    apt-get update && \
    apt-get install -y dotnet-sdk-5.0
WORKDIR /app
COPY publish/ .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
#RUN addgroup --system dotnet
RUN adduser --system  dotnet --group --home /app
RUN chown -R dotnet:dotnet /app
USER dotnet
ENTRYPOINT ["dotnet","Api.dll"]
HEALTHCHECK --interval=30s --timeout=30s --start-period=5s --retries=3 CMD [ "curl -I http://127.0.0.1:8080/healthz --fail || exit 1" ]