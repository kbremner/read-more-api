FROM microsoft/aspnetcore:2.0
ARG source
WORKDIR /app
EXPOSE 80
COPY ${source:-obj/Docker/publish} .
COPY ${source:-Migrations} ./Migrations
CMD ["dotnet", "ReadMoreAPI.dll"]
