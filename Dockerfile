# This dockerfile is used in the integration tests

# Use the official .NET SDK image as a base
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
ARG RESOURCE_REAPER_SESSION_ID="00000000-0000-0000-0000-000000000000"
LABEL "org.testcontainers.resource-reaper-session"=$RESOURCE_REAPER_SESSION_ID

# Set the working directory
WORKDIR /app

# Copy the .csproj file to the container
COPY src/Husky/Husky.csproj ./

ENV HUSKY=0

# Copy the remaining files to the container
COPY . ./

# Restore dependencies
RUN dotnet restore /app/src/Husky

# Build the application
RUN dotnet build --no-restore -c Release -f net8.0 /app/src/Husky

# Create a NuGet package
RUN dotnet pack --no-build --no-restore -c Release -o out /app/src/Husky/Husky.csproj -p:TargetFrameworks=net8.0

# Use the same .NET SDK image for the final stage
FROM mcr.microsoft.com/dotnet/sdk:8.0
ARG RESOURCE_REAPER_SESSION_ID="00000000-0000-0000-0000-000000000000"
LABEL "org.testcontainers.resource-reaper-session"=$RESOURCE_REAPER_SESSION_ID

# Set the working directory
WORKDIR /app

# Install Git
RUN apt-get update && \
    apt-get install -y git

# Copy the NuGet package from the build-env to the runtime image
COPY --from=build-env /app/out/*.nupkg /app/nupkg/

# Install Husky tool and add the global tools path to the PATH
RUN dotnet tool install -g --no-cache --add-source /app/nupkg/ husky \
    && echo "export PATH=\$PATH:/root/.dotnet/tools" >> ~/.bashrc

# Set the entry point to a simple shell
ENTRYPOINT ["/bin/bash"]
