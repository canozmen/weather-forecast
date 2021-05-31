FROM mcr.microsoft.com/dotnet/sdk:5.0.202-alpine3.13-amd64
WORKDIR /app
COPY publish/ .
ENV ASPNETCORE_URLS=http://+:80  
EXPOSE 80
USER dotnet
ENTRYPOINT ["dotnet","Api.dll"]
HEALTHCHECK --interval=30s --timeout=30s --start-period=5s --retries=3 CMD [ "curl -I http://127.0.0.1" ]