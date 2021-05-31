FROM mcr.microsoft.com/dotnet/sdk:5.0.202-alpine3.13-amd64
WORKDIR /app
COPY publish/ .
ENV ASPNETCORE_URLS=http://+:80  
EXPOSE 80
ENTRYPOINT ["dotnet","Api.dll"]