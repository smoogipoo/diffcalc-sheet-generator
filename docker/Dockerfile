###
### Base image
###

FROM ubuntu:20.04 AS base

RUN DEBIAN_FRONTEND=noninteractive apt-get update && apt-get install -y \
    curl \
    wget

# Certificates for github-cli.
RUN curl -fsSL https://cli.github.com/packages/githubcli-archive-keyring.gpg | dd of=/usr/share/keyrings/githubcli-archive-keyring.gpg
RUN echo "deb [arch=$(dpkg --print-architecture) signed-by=/usr/share/keyrings/githubcli-archive-keyring.gpg] https://cli.github.com/packages stable main" | tee /etc/apt/sources.list.d/github-cli.list > /dev/null

# SQL import requisites.
RUN DEBIAN_FRONTEND=noninteractive apt-get update && apt-get install -y \
    git \
    gh \
    mysql-client \
    pv

# Score import requisites.
RUN wget https://packages.microsoft.com/config/ubuntu/20.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
RUN dpkg -i packages-microsoft-prod.deb
RUN rm packages-microsoft-prod.deb

RUN DEBIAN_FRONTEND=noninteractive apt-get update && apt-get install -y \
    apt-transport-https \
    dotnet-sdk-8.0

###
### Generator image
###

FROM base AS generator

WORKDIR /workdir
COPY . ./

WORKDIR /workdir/generator
RUN dotnet publish -c Release -o /out
