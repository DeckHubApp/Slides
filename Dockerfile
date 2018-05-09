FROM microsoft/dotnet:2.1.300-rc1-sdk-bionic AS build

WORKDIR /code

COPY . .

WORKDIR /code/src/DeckHub.Slides

RUN dotnet publish --output /output --configuration Release

FROM microsoft/dotnet:2.1.0-rc1-aspnetcore-runtime-bionic

COPY --from=build /output /app/

WORKDIR /app

ENTRYPOINT ["dotnet", "DeckHub.Slides.dll"]