FROM microsoft/aspnetcore-build:2.1.300-preview1-bionic AS build

WORKDIR /code

COPY . .

WORKDIR /code/src/Slidable.Slides

RUN dotnet publish --output /output --configuration Release

FROM microsoft/aspnetcore:2.1.0-preview1-bionic

COPY --from=build /output /app/

WORKDIR /app

ENTRYPOINT ["dotnet", "Slidable.Slides.dll"]