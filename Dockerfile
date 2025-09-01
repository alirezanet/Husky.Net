# Use the official .NET SDK image as a base
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build-env
ARG RESOURCE_REAPER_SESSION_ID="00000000-0000-0000-0000-000000000000"
LABEL "org.testcontainers.resource-reaper-session"=$RESOURCE_REAPER_SESSION_ID

# Set the working directory
WORKDIR /app

ENV HUSKY=0

# Copy the .csproj file to the container
COPY src/Husky/Husky.csproj ./src/Husky/

# Restore dependencies
RUN dotnet restore /app/src/Husky

# Copy the remaining files to the container
COPY . ./

# Build the application using the custom 'IntegrationTest' configuration
RUN dotnet build --no-restore -c IntegrationTest -f net9.0 /app/src/Husky/Husky.csproj -p:Version=99.1.1-test -p:TargetFrameworks=net9.0

# Create a NuGet package using the 'IntegrationTest' configuration
RUN dotnet pack --no-build --no-restore -c IntegrationTest -o out /app/src/Husky/Husky.csproj -p:Version=99.1.1-test -p:TargetFrameworks=net9.0

# Use the same .NET SDK image for the final stage
FROM mcr.microsoft.com/dotnet/sdk:9.0
ARG RESOURCE_REAPER_SESSION_ID="00000000-0000-0000-0000-000000000000"
LABEL "org.testcontainers.resource-reaper-session"=$RESOURCE_REAPER_SESSION_ID

# Set the working directory
WORKDIR /app

# Install Git
RUN apt-get update && \
    apt-get install -y git && \
    rm -rf /var/lib/apt/lists/*

# Copy the NuGet package from the build-env to the runtime image
COPY --from=build-env /app/out/*.nupkg /app/nupkg/

# Install the specific version from the local source
RUN dotnet tool install -g husky --version 99.1.1-test --add-source /app/nupkg/ --no-cache

RUN echo "export PATH=\$PATH:/root/.dotnet/tools" >> ~/.bashrc

CMD ["/root/.dotnet/tools/husky"]
